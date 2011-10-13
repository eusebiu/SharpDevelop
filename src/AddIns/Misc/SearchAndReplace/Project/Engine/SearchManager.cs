﻿/*
 * Created by SharpDevelop.
 * User: Siegfried
 * Date: 06.10.2011
 * Time: 21:33
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop;
using ICSharpCode.SharpDevelop.Bookmarks;
using ICSharpCode.SharpDevelop.Editor;
using ICSharpCode.SharpDevelop.Editor.AvalonEdit;
using ICSharpCode.SharpDevelop.Editor.Search;
using ICSharpCode.SharpDevelop.Gui;
using ICSharpCode.SharpDevelop.Project;

namespace SearchAndReplace
{
	/// <summary>
	/// Description of SearchManager.
	/// </summary>
	public class SearchManager
	{
		static IEnumerable<FileName> GenerateFileList(SearchTarget target, string baseDirectory = null, string filter = "*.*", bool searchSubdirs = false)
		{
			List<FileName> files = new List<FileName>();
			
			switch (target) {
				case SearchTarget.CurrentDocument:
				case SearchTarget.CurrentSelection:
					ITextEditorProvider vc = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
					if (vc != null)
						files.Add(vc.TextEditor.FileName);
					break;
				case SearchTarget.AllOpenFiles:
					foreach (ITextEditorProvider editor in WorkbenchSingleton.Workbench.ViewContentCollection.OfType<ITextEditorProvider>())
						files.Add(editor.TextEditor.FileName);
					break;
				case SearchTarget.WholeProject:
					if (ProjectService.CurrentProject == null)
						break;
					foreach (FileProjectItem item in ProjectService.CurrentProject.Items.OfType<FileProjectItem>())
						files.Add(new FileName(item.FileName));
					break;
				case SearchTarget.WholeSolution:
					if (ProjectService.OpenSolution == null)
						break;
					foreach (var item in ProjectService.OpenSolution.SolutionFolderContainers.Select(f => f.SolutionItems).SelectMany(si => si.Items))
						files.Add(new FileName(Path.Combine(ProjectService.OpenSolution.Directory, item.Location)));
					foreach (var item in ProjectService.OpenSolution.Projects.SelectMany(p => p.Items).OfType<FileProjectItem>())
						files.Add(new FileName(item.FileName));
					break;
				case SearchTarget.Directory:
					if (!Directory.Exists(baseDirectory))
						break;
					return FileUtility.LazySearchDirectory(baseDirectory, filter, searchSubdirs);
				default:
					throw new Exception("Invalid value for FileListType");
			}
			
			return files.Distinct();
		}
		
		public static void ShowSearchResults(string pattern, IObservable<SearchResultMatch> results)
		{
			string title = StringParser.Parse("${res:MainWindow.Windows.SearchResultPanel.OccurrencesOf}",
			                                  new StringTagPair("Pattern", pattern));
			SearchResultsPad.Instance.ShowSearchResults(title, results);
			SearchResultsPad.Instance.BringToFront();
		}
		
		public static IObservable<SearchResultMatch> FindAll(string pattern, bool ignoreCase, bool matchWholeWords, SearchMode mode,
		                                                     SearchTarget target, string baseDirectory = null, string filter = "*.*", bool searchSubdirs = false)
		{
			currentSearchRegion = null;
			CancellationTokenSource cts = new CancellationTokenSource();
			var monitor = WorkbenchSingleton.Workbench.StatusBar.CreateProgressMonitor(cts.Token);
			monitor.TaskName = "Find all occurrences of '" + pattern + "' in " + GetTargetDescription(target, baseDirectory);
			monitor.Status = OperationStatus.Normal;
			var strategy = SearchStrategyFactory.Create(pattern, ignoreCase, matchWholeWords, mode);
			ParseableFileContentFinder fileFinder = new ParseableFileContentFinder();
			IEnumerable<FileName> fileList = GenerateFileList(target, baseDirectory, filter, searchSubdirs);
			return new SearchRun(strategy, fileFinder, fileList, monitor, cts);
		}
		
		static string GetTargetDescription(SearchTarget target, string baseDirectory = null)
		{
			switch (target) {
				case SearchTarget.CurrentDocument:
					return "the current document";
				case SearchTarget.CurrentSelection:
					return "the current selection";
				case SearchTarget.AllOpenFiles:
					return "all open files";
				case SearchTarget.WholeProject:
					return "the whole project";
				case SearchTarget.WholeSolution:
					return "the whole solution";
				case SearchTarget.Directory:
					return "the directory '" + baseDirectory + "'";
				default:
					throw new Exception("Invalid value for SearchTarget");
			}
		}
		
		class SearchRun : IObservable<SearchResultMatch>, IDisposable
		{
			IObserver<SearchResultMatch> observer;
			ISearchStrategy strategy;
			ParseableFileContentFinder fileFinder;
			IEnumerable<FileName> fileList;
			IProgressMonitor monitor;
			CancellationTokenSource cts;
			int count;
			
			public SearchRun(ISearchStrategy strategy, ParseableFileContentFinder fileFinder, IEnumerable<FileName> fileList, IProgressMonitor monitor, CancellationTokenSource cts)
			{
				this.strategy = strategy;
				this.fileFinder = fileFinder;
				this.fileList = fileList;
				this.monitor = monitor;
				this.cts = cts;
				this.count = fileList.Count();
			}
			
			public IDisposable Subscribe(IObserver<SearchResultMatch> observer)
			{
				this.observer = observer;
				var task = new System.Threading.Tasks.Task(delegate { Parallel.ForEach(fileList, new ParallelOptions { CancellationToken = monitor.CancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount }, fileName => SearchFile(fileName, strategy, monitor.CancellationToken)); });
				task.ContinueWith(t => { if (t.Exception != null) observer.OnError(t.Exception); else observer.OnCompleted(); this.Dispose(); });
				task.Start();
				return this;
			}
			
			public void Dispose()
			{
				if (!cts.IsCancellationRequested)
					cts.Cancel();
				monitor.Dispose();
			}
			
			void SearchFile(FileName fileName, ISearchStrategy strategy, CancellationToken ct)
			{
				ITextBuffer buffer = fileFinder.Create(fileName);
				if (buffer == null)
					return;
				
				if (!MimeTypeDetection.FindMimeType(buffer).StartsWith("text/"))
					return;
				var source = DocumentUtilitites.GetTextSource(buffer);
				TextDocument document = null;
				DocumentHighlighter highlighter = null;
				foreach(var result in strategy.FindAll(source, 0, source.TextLength)) {
					ct.ThrowIfCancellationRequested();
					if (document == null) {
						document = new TextDocument(source);
						var highlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(fileName));
						if (highlighting != null)
							highlighter = new DocumentHighlighter(document, highlighting.MainRuleSet);
					}
					var start = document.GetLocation(result.Offset).ToLocation();
					var end = document.GetLocation(result.Offset + result.Length).ToLocation();
					lock (observer)
						observer.OnNext(new SearchResultMatch(fileName, start, end, SearchResultsPad.CreateInlineBuilder(start, end, document, Path.GetExtension(fileName))));
				}
				lock (monitor)
					monitor.Progress += 1.0 / count;
			}
		}
		
		public static ITextEditor GetActiveTextEditor()
		{
			ITextEditorProvider provider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
			if (provider != null) {
				return provider.TextEditor;
			} else {
				return null;
			}
		}
		
		static SearchRegion currentSearchRegion;
		
		public static SearchResultMatch FindNext(string pattern, bool ignoreCase, bool matchWholeWords, SearchMode mode,
		                                         SearchTarget target, string baseDirectory = null, string filter = "*.*", bool searchSubdirs = false)
		{
			if (string.IsNullOrEmpty(pattern))
				return null;
			var tmp = GenerateFileList(target, baseDirectory, filter, searchSubdirs).ToList();
			var active = GetActiveTextEditor();
			if (active != null) {
				int index = tmp.FindIndex(fn => fn.Equals(active.FileName));
				if (index > 0) {
					var item = tmp[index];
					tmp.RemoveAt(index);
					tmp.Insert(0, item);
				}
			}
			var files = tmp.ToArray();
			bool isNewSearch = currentSearchRegion == null || !currentSearchRegion.IsSameState(files, pattern, ignoreCase, matchWholeWords, mode, target, baseDirectory, filter, searchSubdirs);
			if (isNewSearch)
				currentSearchRegion = CreateSearchRegion(files, pattern, ignoreCase, matchWholeWords, mode, target, baseDirectory, filter, searchSubdirs);
			if (currentSearchRegion == null)
				return null;
			if (currentSearchRegion.Files.Length == 0)
				return null;
			if (currentSearchRegion.CurrentFile < 0 || currentSearchRegion.CurrentFile >= currentSearchRegion.Files.Length)
				currentSearchRegion.CurrentFile = 0;
			
			ParseableFileContentFinder finder = new ParseableFileContentFinder();
			while (currentSearchRegion.CurrentFile < currentSearchRegion.Files.Length) {
				FileName currentFile = currentSearchRegion.Files[currentSearchRegion.CurrentFile];
				var buffer = finder.Create(currentFile);
				
				if (buffer == null) {
					currentSearchRegion.Repeat = false;
					currentSearchRegion.CurrentFile++;
					continue;
				}
				
				int offset, length;
				if (currentSearchRegion.LastSearchOffset < 0) {
					if (currentSearchRegion.Selection == null)
						offset = currentSearchRegion.SearchStart.Offset;
					else
						offset = Math.Max(currentSearchRegion.Selection.Offset, currentSearchRegion.SearchStart.Offset);
				} else
					offset = currentSearchRegion.LastSearchOffset;
				if (currentSearchRegion.Selection == null)
					length = buffer.TextLength - offset;
				else
					length = currentSearchRegion.Selection.EndOffset - offset;
				
				var document = new TextDocument(DocumentUtilitites.GetTextSource(buffer));
				var result = currentSearchRegion.Strategy.FindNext(DocumentUtilitites.GetTextSource(buffer), offset, length);
				
				if (result != null)
					currentSearchRegion.LastSearchOffset = result.EndOffset;
				
				if (currentSearchRegion.Repeat && currentFile.Equals(currentSearchRegion.SearchStart.FileName) && currentSearchRegion.LastSearchOffset >= currentSearchRegion.SearchStart.Offset) {
					currentSearchRegion = null;
					return null;
				}
				
				if (result != null) {
					var start = document.GetLocation(result.Offset).ToLocation();
					var end = document.GetLocation(result.EndOffset).ToLocation();
					return new SearchResultMatch(currentFile, start, end, SearchResultsPad.CreateInlineBuilder(start, end, document, Path.GetExtension(currentFile)));
				}
				currentSearchRegion.CurrentFile++;
				currentSearchRegion.LastSearchOffset = 0;
				
				if (currentSearchRegion.CurrentFile >= currentSearchRegion.Files.Length) {
					currentSearchRegion.Repeat = true;
					currentSearchRegion.CurrentFile = 0;
				}
			}
			
			return null;
		}
		
		static SearchRegion CreateSearchRegion(FileName[] files, string pattern, bool ignoreCase, bool matchWholeWords, SearchMode mode, SearchTarget target, string baseDirectory, string filter, bool searchSubdirs)
		{
			ITextEditor editor = GetActiveTextEditor();
			if (editor != null) {
				var document = new TextDocument(DocumentUtilitites.GetTextSource(editor.Document));
				AnchorSegment selection = null;
				if (target == SearchTarget.CurrentSelection)
					selection = new AnchorSegment(document, editor.SelectionStart, editor.SelectionLength);
				return new SearchRegion(files, selection, new PermanentAnchor(editor.FileName, editor.Caret.Line, editor.Caret.Column), pattern, ignoreCase, matchWholeWords, mode, target, baseDirectory, filter, searchSubdirs);
			}
			
			return null;
		}
		
		class SearchRegion
		{
			FileName[] files;
			
			public int CurrentFile { get; set; }
			public int LastSearchOffset { get; set; }
			public bool Repeat { get; set; }
			
			public FileName[] Files {
				get { return files; }
			}
			
			AnchorSegment selection;
			
			public AnchorSegment Selection {
				get { return selection; }
			}
			
			PermanentAnchor searchStart;
			
			public PermanentAnchor SearchStart {
				get { return searchStart; }
			}
			
			ISearchStrategy strategy;
			
			public ISearchStrategy Strategy {
				get {
					if (strategy == null)
						strategy = SearchStrategyFactory.Create(pattern, ignoreCase, matchWholeWords, mode);
					
					return strategy;
				}
			}
			
			public SearchRegion(FileName[] files, AnchorSegment selection, PermanentAnchor searchStart, string pattern, bool ignoreCase, bool matchWholeWords, SearchMode mode, SearchTarget target, string baseDirectory, string filter, bool searchSubdirs)
			{
				if (files == null)
					throw new ArgumentNullException("files");
				this.files = files;
				this.selection = selection;
				this.searchStart = searchStart;
				this.LastSearchOffset = -1;
				this.pattern = pattern;
				this.ignoreCase = ignoreCase;
				this.matchWholeWords = matchWholeWords;
				this.mode = mode;
				this.target = target;
				this.baseDirectory = baseDirectory;
				this.filter = filter;
				this.searchSubdirs = searchSubdirs;
			}
			
			string pattern;
			bool ignoreCase;
			bool matchWholeWords;
			SearchMode mode;
			SearchTarget target;
			string baseDirectory;
			string filter;
			bool searchSubdirs;
			
			public bool IsSameState(FileName[] files, string pattern, bool ignoreCase, bool matchWholeWords, SearchMode mode, SearchTarget target, string baseDirectory, string filter, bool searchSubdirs)
			{
				return this.files.OrderBy(f => f.ToString()).SequenceEqual(files.OrderBy(f => f.ToString())) &&
					pattern == this.pattern &&
					ignoreCase == this.ignoreCase &&
					matchWholeWords == this.matchWholeWords &&
					mode == this.mode &&
					target == this.target &&
					baseDirectory == this.baseDirectory &&
					filter == this.filter &&
					searchSubdirs == this.searchSubdirs;
			}
		}
		
		public static void MarkAll(IObservable<SearchResultMatch> results)
		{
			var marker = new SearchResultMarker();
			marker.Registration = results.Subscribe(marker);
		}
		
		static void MarkResult(SearchResultMatch result, bool switchToOpenedView = true)
		{
			ITextEditor textArea = OpenTextArea(result.FileName, switchToOpenedView);
			if (textArea != null) {
				if (switchToOpenedView)
					textArea.Caret.Position = result.StartLocation;

				foreach (var bookmark in BookmarkManager.GetBookmarks(result.FileName)) {
					if (bookmark.CanToggle && bookmark.LineNumber == result.StartLocation.Line) {
						// bookmark or breakpoint already exists at that line
						return;
					}
				}
				BookmarkManager.AddMark(new Bookmark(result.FileName, result.StartLocation));
			}
		}
		
		static ITextEditor OpenTextArea(string fileName, bool switchToOpenedView = true)
		{
			ITextEditorProvider textEditorProvider;
			if (fileName != null) {
				textEditorProvider = FileService.OpenFile(fileName, switchToOpenedView) as ITextEditorProvider;
			} else {
				textEditorProvider = WorkbenchSingleton.Workbench.ActiveViewContent as ITextEditorProvider;
			}

			if (textEditorProvider != null) {
				return textEditorProvider.TextEditor;
			}
			return null;
		}
		
		static void ShowMarkDoneMessage(int count)
		{
			if (count == 0) {
				ShowNotFoundMessage();
			} else {
				MessageService.ShowMessage(
					StringParser.Parse("${res:ICSharpCode.TextEditor.Document.SearchReplaceManager.MarkAllDone}",
					                   new StringTagPair("Count", count.ToString())),
					"${res:Global.FinishedCaptionText}");
			}
		}
		
		static void ShowNotFoundMessage()
		{
			MessageService.ShowMessage("${res:Dialog.NewProject.SearchReplace.SearchStringNotFound}", "${res:Dialog.NewProject.SearchReplace.SearchStringNotFound.Title}");
		}
		
		class SearchResultMarker : IObserver<SearchResultMatch>
		{
			public IDisposable Registration { get; set; }
			
			int count;
			
			void IObserver<SearchResultMatch>.OnNext(SearchResultMatch value)
			{
				Interlocked.Increment(ref count);
				WorkbenchSingleton.SafeThreadCall((Action)delegate { MarkResult(value, false); });
			}
			
			void IObserver<SearchResultMatch>.OnError(Exception error)
			{
				MessageService.ShowException(error);
				OnCompleted(false);
			}
			
			void OnCompleted(bool success)
			{
				WorkbenchSingleton.SafeThreadCall(
					(Action)delegate {
						if (Registration != null)
							Registration.Dispose();
						if (success)
							ShowMarkDoneMessage(count);
					});
			}
			
			void IObserver<SearchResultMatch>.OnCompleted()
			{
				OnCompleted(true);
			}
		}
		
		public static void SelectResult(SearchResultMatch result)
		{
			if (result == null) {
				ShowNotFoundMessage();
				return;
			}
			var editor = OpenTextArea(result.FileName, true);
			var start = editor.Document.PositionToOffset(result.StartLocation.Line, result.StartLocation.Column);
			var end = editor.Document.PositionToOffset(result.EndLocation.Line, result.EndLocation.Column);
			if (editor != null) {
				editor.Caret.Offset = start;
				editor.Select(start, end - start);
			}
		}
	}
}