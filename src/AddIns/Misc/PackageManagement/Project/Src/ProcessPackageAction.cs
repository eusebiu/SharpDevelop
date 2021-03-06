﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.PackageManagement.Scripting;
using ICSharpCode.SharpDevelop.Project;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public abstract class ProcessPackageAction
	{
		IPackageManagementEvents packageManagementEvents;
		
		public ProcessPackageAction(
			IPackageManagementProject project,
			IPackageManagementEvents packageManagementEvents)
		{
			this.Project = project;
			this.packageManagementEvents = packageManagementEvents;
		}
		
		public IPackageManagementProject Project { get; set; }
		public ILogger Logger { get; set; }
		public IPackage Package { get; set; }
		public Version PackageVersion { get; set; }
		public string PackageId { get; set; }
		public IPackageScriptRunner PackageScriptRunner { get; set; }
		
		public virtual bool HasPackageScriptsToRun()
		{
			return false;
		}
		
		protected void OnParentPackageInstalled()
		{
			packageManagementEvents.OnParentPackageInstalled(Package);
		}
		
		protected void OnParentPackageUninstalled()
		{
			packageManagementEvents.OnParentPackageUninstalled(Package);
		}
		
		public void Execute()
		{
			BeforeExecute();
			if (PackageScriptRunner != null) {
				ExecuteWithScriptRunner();
			} else {
				ExecuteCore();
			}
		}
		
		protected virtual void BeforeExecute()
		{
			GetLoggerIfMissing();
			ConfigureProjectLogger();
			GetPackageIfMissing();
		}
		
		void ExecuteWithScriptRunner()
		{
			using (RunPackageScriptsAction runScriptsAction = CreateRunPackageScriptsAction()) {
				ExecuteCore();
			}
		}
		
		RunPackageScriptsAction CreateRunPackageScriptsAction()
		{
			return CreateRunPackageScriptsAction(PackageScriptRunner, Project);
		}
		
		protected virtual RunPackageScriptsAction CreateRunPackageScriptsAction(
			IPackageScriptRunner scriptRunner,
			IPackageManagementProject project)
		{
			return new RunPackageScriptsAction(scriptRunner, project);
		}
		
		protected virtual void ExecuteCore()
		{
		}
		
		void GetLoggerIfMissing()
		{
			if (Logger == null) {
				Logger = new PackageManagementLogger(packageManagementEvents);
			}
		}
		
		void ConfigureProjectLogger()
		{
			Project.Logger = Logger;
		}
		
		void GetPackageIfMissing()
		{
			if (Package == null) {
				Package = Project.SourceRepository.FindPackage(PackageId, PackageVersion);
			}
		}
		
		protected bool PackageIdExistsInProject()
		{
			string id = GetPackageId();
			return Project.IsPackageInstalled(id);
		}
		
		string GetPackageId()
		{
			if (Package != null) {
				return Package.Id;
			}
			return PackageId;
		}
	}
}
