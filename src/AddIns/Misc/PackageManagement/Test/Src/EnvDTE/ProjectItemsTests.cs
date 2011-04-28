﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using ICSharpCode.PackageManagement.EnvDTE;
using DTE = ICSharpCode.PackageManagement.EnvDTE;
using ICSharpCode.SharpDevelop.Project;
using NUnit.Framework;
using PackageManagement.Tests.Helpers;

namespace PackageManagement.Tests.EnvDTE
{
	[TestFixture]
	public class ProjectItemsTests
	{
		TestableDTEProject project;
		ProjectItems projectItems;
		TestableProject msbuildProject;
		FakeFileService fakeFileService;
		
		void CreateProjectItems()
		{
			project = new TestableDTEProject();
			msbuildProject = project.TestableProject;
			projectItems = project.ProjectItems;
			fakeFileService = project.FakeFileService;
		}
		
		void ProjectItemCollectionAssertAreEqual(string[] expectedItems, List<DTE.ProjectItem> itemsList)
		{
			var actualItems = new List<string>();
			itemsList.ForEach(r => actualItems.Add(r.Name));
			
			CollectionAssert.AreEqual(expectedItems, actualItems);
		}
		
		void ProjectItemCollectionAssertAreEqual(string[] expectedItems, IEnumerable itemsList)
		{
			var actualItems = new List<string>();
			foreach (DTE.ProjectItem item in itemsList) {
				actualItems.Add(item.Name);
			}
			
			CollectionAssert.AreEqual(expectedItems, actualItems);
		}
		
		[Test]
		public void AddFromFileCopy_AddFileNameOutsideProjectFolder_FileIsIncludedInProjectInProjectFolder()
		{
			CreateProjectItems();
			msbuildProject.FileName = @"d:\projects\myproject\myproject\myproject.csproj";
			string fileName = @"d:\projects\myproject\packages\tools\test.cs";
			
			projectItems.AddFromFileCopy(fileName);
			
			var fileItem = msbuildProject.Items[0] as FileProjectItem;
			
			Assert.AreEqual("test.cs", fileItem.Include);
		}
		
		[Test]
		public void AddFromFileCopy_AddFileNameOutsideProjectFolder_FileItemTypeTakenFromProject()
		{
			CreateProjectItems();
			msbuildProject.FileName = @"d:\projects\myproject\myproject\myproject.csproj";
			string fileName = @"d:\projects\myproject\packages\tools\test.cs";
			
			msbuildProject.ItemTypeToReturnFromGetDefaultItemType = ItemType.Page;
			projectItems.AddFromFileCopy(fileName);
			
			var fileItem = msbuildProject.Items[0] as FileProjectItem;
			
			Assert.AreEqual(ItemType.Page, fileItem.ItemType);
		}
		
		[Test]
		public void AddFromFileCopy_AddFileNameOutsideProjectFolder_FileNamePassedToDetermineFileItemType()
		{
			CreateProjectItems();
			msbuildProject.FileName = @"d:\projects\myproject\myproject\myproject.csproj";
			string fileName = @"d:\projects\myproject\packages\tools\test.cs";
			
			msbuildProject.ItemTypeToReturnFromGetDefaultItemType = ItemType.Page;
			projectItems.AddFromFileCopy(fileName);
			
			Assert.AreEqual("test.cs", msbuildProject.FileNamePassedToGetDefaultItemType);
		}
		
		[Test]
		public void AddFromFileCopy_AddFileNameOutsideProjectFolder_ProjectIsSaved()
		{
			CreateProjectItems();
			msbuildProject.FileName = @"d:\projects\myproject\myproject\myproject.csproj";
			string fileName = @"d:\projects\myproject\packages\tools\test.cs";
			
			msbuildProject.ItemTypeToReturnFromGetDefaultItemType = ItemType.Page;
			projectItems.AddFromFileCopy(fileName);
			
			bool saved = msbuildProject.IsSaved;
			
			Assert.IsTrue(saved);
		}
		
		[Test]
		public void AddFromFileCopy_AddFileNameOutsideProjectFolder_FileIsCopied()
		{
			CreateProjectItems();
			msbuildProject.FileName = @"d:\projects\myproject\myproject\myproject.csproj";
			string fileName = @"d:\projects\myproject\packages\tools\test.cs";
			
			msbuildProject.ItemTypeToReturnFromGetDefaultItemType = ItemType.Page;
			projectItems.AddFromFileCopy(fileName);
			
			string[] expectedFileNames = new string[] {
				@"d:\projects\myproject\packages\tools\test.cs",
				@"d:\projects\myproject\myproject\test.cs"
			};
			
			string[] actualFileNames = new string[] {
				fakeFileService.OldFileNamePassedToCopyFile,
				fakeFileService.NewFileNamePassedToCopyFile
			};
			
			CollectionAssert.AreEqual(expectedFileNames, actualFileNames);
		}
		
		[Test]
		public void GetEnumerator_ProjectHasTwoFiles_TwoFilesReturned()
		{
			CreateProjectItems();
			msbuildProject.AddFile(@"Test.cs");
			
			var itemsList = new List<DTE.ProjectItem>();
			itemsList.AddRange(projectItems);
			
			var expectedItems = new string[] {
				"Test.cs"
			};
			
			ProjectItemCollectionAssertAreEqual(expectedItems, itemsList);
		}
		
		[Test]
		public void GetEnumerator_UseUntypedEnumeratorProjectHasOneFile_OneFileReturned()
		{
			CreateProjectItems();
			msbuildProject.AddFile(@"Program.cs");
			
			var enumerable = projectItems as IEnumerable;
			
			var expectedFiles = new string[] {
				"Program.cs"
			};
			
			ProjectItemCollectionAssertAreEqual(expectedFiles, enumerable);
		}
		
		[Test]
		public void GetEnumerator_ProjectHasOneFileAndOneReference_OneFileReturned()
		{
			CreateProjectItems();
			msbuildProject.AddReference("NUnit.Framework");
			msbuildProject.AddFile(@"Program.cs");
			
			var enumerable = projectItems as IEnumerable;
			
			var expectedFiles = new string[] {
				"Program.cs"
			};
			
			ProjectItemCollectionAssertAreEqual(expectedFiles, enumerable);
		}
		
		[Test]
		public void GetEnumerator_ProjectHasOneFileInSubDirectory_OneFolderReturned()
		{
			CreateProjectItems();
			msbuildProject.AddFile(@"src\Program.cs");
			
			var enumerable = projectItems as IEnumerable;
			
			var expectedItems = new string[] {
				"src"
			};
			
			ProjectItemCollectionAssertAreEqual(expectedItems, enumerable);
		}
		
		[Test]
		public void GetEnumerator_ProjectHasTwoFilesInDifferentSubDirectories_TwoFoldersReturned()
		{
			CreateProjectItems();
			msbuildProject.AddFile(@"Controllers\Program.cs");
			msbuildProject.AddFile(@"ViewModels\Program.cs");
			
			var enumerable = projectItems as IEnumerable;
			
			var expectedItems = new string[] {
				"Controllers",
				"ViewModels"
			};
			
			ProjectItemCollectionAssertAreEqual(expectedItems, enumerable);
		}
		
		[Test]
		public void GetEnumerator_ProjectHasTwoFilesInSameSubDirectory_OneFolderReturned()
		{
			CreateProjectItems();
			msbuildProject.AddFile(@"Controllers\Tests\Program1.cs");
			msbuildProject.AddFile(@"Controllers\Tests\Program2.cs");
			
			var enumerable = projectItems as IEnumerable;
			
			var expectedItems = new string[] {
				"Controllers",
			};
			
			ProjectItemCollectionAssertAreEqual(expectedItems, enumerable);
		}
		
		[Test]
		public void GetEnumerator_ProjectHasOneFolderAndOneFileInSameSubDirectory_OneFileAndOneFolderReturned()
		{
			CreateProjectItems();
			msbuildProject.AddFile(@"Controllers\Program.cs");
			msbuildProject.AddDirectory(@"Controllers");
			
			var enumerable = projectItems as IEnumerable;
			
			var expectedItems = new string[] {
				"Controllers",
			};
			
			ProjectItemCollectionAssertAreEqual(expectedItems, enumerable);
		}
		
		[Test]
		public void GetEnumerator_ProjectHasOneFolderAndOneFileInSameSubDirectoryWithDirectoryFirstInProject_OneFileAndOneFolderReturned()
		{
			CreateProjectItems();
			msbuildProject.AddDirectory(@"Controllers");
			msbuildProject.AddFile(@"Controllers\Program.cs");
			
			var enumerable = projectItems as IEnumerable;
			
			var expectedItems = new string[] {
				"Controllers",
			};
			
			ProjectItemCollectionAssertAreEqual(expectedItems, enumerable);
		}
		
		[Test]
		public void GetEnumerator_ProjectHasOneLinkedFile_OneFileReturned()
		{
			CreateProjectItems();
			var fileItem = msbuildProject.AddFile(@"..\..\Program.cs");
			fileItem.SetMetadata("Link", "MyProgram.cs");
			
			var enumerable = projectItems as IEnumerable;
			
			var expectedFiles = new string[] {
				"Program.cs"
			};
			
			ProjectItemCollectionAssertAreEqual(expectedFiles, enumerable);
		}
		
		[Test]
		public void GetEnumerator_ProjectHasFileWithRelativePath_FileIsTreatedAsLinkAndReturned()
		{
			CreateProjectItems();
			msbuildProject.AddFile(@"..\..\Program.cs");
			
			var enumerable = projectItems as IEnumerable;
			
			var expectedFiles = new string[] {
				"Program.cs"
			};
			
			ProjectItemCollectionAssertAreEqual(expectedFiles, enumerable);
		}
		
		[Test]
		public void GetEnumerator_ProjectHasOneLinkedFileInProjectSubDirectoryAndOneDirectory_OneDirectoryReturned()
		{
			CreateProjectItems();
			msbuildProject.AddDirectory("Configuration");
			var fileItem = msbuildProject.AddFile(@"..\..\AssemblyInfo.cs");
			fileItem.SetMetadata("Link", @"Configuration\MyAssemblyInfo.cs");
			
			var enumerable = projectItems as IEnumerable;
			
			var expectedItems = new string[] {
				"Configuration"
			};
			
			ProjectItemCollectionAssertAreEqual(expectedItems, enumerable);
		}
	}
}