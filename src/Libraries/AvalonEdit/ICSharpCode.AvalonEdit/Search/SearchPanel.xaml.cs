﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Search
{
	/// <summary>
	/// Interaction logic for SearchPanel.xaml
	/// </summary>
	public partial class SearchPanel : UserControl
	{
		TextArea textArea;
		SearchResultBackgroundRenderer renderer;
		SearchResult currentResult;
		FoldingManager foldingManager;
		
		bool UseRegex { get { return useRegex.IsChecked == true; } }
		bool MatchCase { get { return matchCase.IsChecked == true; } }
		bool WholeWords { get { return wholeWords.IsChecked == true; } }
		
		string searchPattern;
		ISearchStrategy strategy;
		
		/// <summary>
		/// Gets/sets the content of the search box and the search string.
		/// </summary>
		public string SearchPattern {
			get { return searchPattern; }
			set {
				searchPattern = value;
				UpdateSearch();
			}
		}

		void UpdateSearch()
		{
			messageView.IsOpen = false;
			strategy = SearchStrategyFactory.Create(searchPattern ?? "", !MatchCase, UseRegex, WholeWords);
			DoSearch(true);
		}
		
		/// <summary>
		/// Creates a new SearchPanel and attaches it to a text area.
		/// </summary>
		public SearchPanel(TextArea textArea)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			this.textArea = textArea;
			DataContext = this;
			InitializeComponent();
			
			textArea.TextView.Layers.Add(this);
			foldingManager = textArea.GetService(typeof(FoldingManager)) as FoldingManager;
			
			renderer = new SearchResultBackgroundRenderer();
			textArea.TextView.BackgroundRenderers.Add(renderer);
			textArea.Document.TextChanged += delegate { DoSearch(false); };
			this.Loaded += delegate { searchTextBox.Focus(); };
			
			useRegex.Checked     += ValidateSearchText;
			matchCase.Checked    += ValidateSearchText;
			wholeWords.Checked   += ValidateSearchText;
			
			useRegex.Unchecked   += ValidateSearchText;
			matchCase.Unchecked  += ValidateSearchText;
			wholeWords.Unchecked += ValidateSearchText;
		}
		
		void ValidateSearchText(object sender, RoutedEventArgs e)
		{
			var be = searchTextBox.GetBindingExpression(TextBox.TextProperty);
			try {
				Validation.ClearInvalid(be);
				UpdateSearch();
			} catch (SearchPatternException ex) {
				var ve = new ValidationError(be.ParentBinding.ValidationRules[0], be, ex.Message, ex);
				Validation.MarkInvalid(be, ve);
			}
		}
		
		/// <summary>
		/// Reactivates the SearchPanel by setting the focus on the search box and selecting all text.
		/// </summary>
		public void Reactivate()
		{
			searchTextBox.Focus();
			searchTextBox.SelectAll();
		}
		
		/// <summary>
		/// Moves to the next occurrence in the file.
		/// </summary>
		public void FindNext()
		{
			SearchResult result = null;
			if (currentResult != null)
				result = renderer.CurrentResults.GetNextSegment(currentResult);
			if (result == null)
				result = renderer.CurrentResults.FirstSegment;
			if (result != null) {
				currentResult = result;
				SetResult(result);
			}
		}
		
		/// <summary>
		/// Moves to the previous occurrence in the file.
		/// </summary>
		public void FindPrevious()
		{
			SearchResult result = null;
			if (currentResult != null)
				result = renderer.CurrentResults.GetPreviousSegment(currentResult);
			if (result == null)
				result = renderer.CurrentResults.LastSegment;
			if (result != null) {
				currentResult = result;
				SetResult(result);
			}
		}
		
		ToolTip messageView = new ToolTip { Placement = PlacementMode.Bottom };

		void DoSearch(bool changeSelection)
		{
			renderer.CurrentResults.Clear();
			currentResult = null;
			if (!string.IsNullOrEmpty(SearchPattern)) {
				int offset = textArea.Caret.Offset;
				if (changeSelection) {
					textArea.Selection = SimpleSelection.Empty;
				}
				foreach (SearchResult result in strategy.FindAll(textArea.Document)) {
					if (currentResult == null && result.StartOffset >= offset) {
						currentResult = result;
						if (changeSelection) {
							SetResult(result);
						}
					}
					renderer.CurrentResults.Add(result);
				}
			}
			textArea.TextView.InvalidateLayer(KnownLayer.Selection);
		}

		void SetResult(SearchResult result)
		{
			textArea.Caret.Offset = currentResult.StartOffset;
			textArea.Selection = new SimpleSelection(currentResult.StartOffset, currentResult.EndOffset);
			if (foldingManager != null) {
				foreach (var folding in foldingManager.GetFoldingsContaining(result.StartOffset))
					folding.IsFolded = false;
			}
			textArea.Caret.BringCaretToView();
		}
		
		void SearchLayerKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key) {
				case Key.Enter:
					e.Handled = true;
					messageView.IsOpen = false;
					if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
						FindPrevious();
					else
						FindNext();
					var error = Validation.GetErrors(searchTextBox).FirstOrDefault();
					if (error != null) {
						messageView.Content = "Error: " + error.ErrorContent;
						messageView.PlacementTarget = searchTextBox;
						messageView.IsOpen = true;
					}
					break;
				case Key.Escape:
					e.Handled = true;
					CloseClick(sender, e);
					break;
			}
		}
		
		void CloseClick(object sender, RoutedEventArgs e)
		{
			textArea.TextView.Layers.Remove(this);
			textArea.TextView.BackgroundRenderers.Remove(renderer);
			messageView.IsOpen = false;
		}
	}
}