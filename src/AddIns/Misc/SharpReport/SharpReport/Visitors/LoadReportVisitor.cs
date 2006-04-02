//------------------------------------------------------------------------------
// <autogenerated>
//     This code was generated by a tool.
//     Runtime Version: 1.1.4322.2032
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </autogenerated>
//------------------------------------------------------------------------------
/// <summary>
/// Loads a ReportFile and set it's Values in the ReportDesigner
/// </summary>
/// <remarks>
/// 	created by - Forstmeier Peter
/// 	created on - 02.12.2004 22:44:39
/// </remarks>

using System;
using System.Xml;
using System.ComponentModel;
using System.Globalization;

using SharpReportCore;

using SharpReport.Visitors;
using SharpReport.Designer;
using SharpReport.ReportItems;


namespace SharpReport.Visitors {
	public class LoadReportVisitor : SharpReport.Visitors.AbstractVisitor {
		
		SharpReport.Designer.BaseDesignerControl designer;
		IDesignableFactory designableFactory ;
		
		public LoadReportVisitor(string fileName):base(fileName) {
			designableFactory = new IDesignableFactory();
		}
		
		/// <summary>
		/// Loads ReportDefinition from File and set the values in the SharpReportDesigner
		/// </summary>
		/// <param name='designer'>SharpReportDesigner</param>
		
		public override void Visit(SharpReport.Designer.BaseDesignerControl designer){
			if (designer == null) {
				throw new ArgumentNullException("designer");
			}
			
			XmlDocument xmlDoc;
			try {
				xmlDoc = XmlHelper.OpenSharpReport (base.FileName);
				this.designer = designer;
				SetDesigner (xmlDoc);
				AdjustSectionsWidth();
			} catch (Exception ) {
				throw ;
			}
			
		}
		
		private void AdjustSectionsWidth() {
			foreach (ReportSection section in designer.SectionsCollection) {
				section.VisualControl.Width = designer.ReportModel.ReportSettings.PageSettings.Bounds.Width;
				if (section.SectionMargin == 0) {
					section.SectionMargin = designer.ReportModel.ReportSettings.PageSettings.Bounds.Left;
				}
			}
		}
		
		
		
		private void  SetDesigner (XmlDocument doc){
			this.designer.ReportModel.ReportSettings.SetSettings ((XmlElement)doc.DocumentElement.FirstChild);
			SetSections (doc);
		}
		
	
		
		private void SetSections (XmlDocument doc) {
		
			XmlNodeList sectionNodes = doc.DocumentElement.ChildNodes;
			//Start with node(1)
			XmlNode node;
			BaseSection baseSection = null;
			for (int i = 1;i < sectionNodes.Count ; i++ ) {
				node = sectionNodes[i];
				XmlElement sectionElem = node as XmlElement;
				if (sectionElem != null) {
					baseSection = (BaseSection)designer.ReportModel.SectionCollection.Find(sectionElem.GetAttribute("name"));
					if (baseSection != null) {
						baseSection.SuspendLayout();
						
						XmlHelper.SetSectionValues (base.XmlFormReader,sectionElem,baseSection);
						XmlNodeList ctrlList = sectionElem.SelectNodes (base.NodesQuery);
						SetReportItems(baseSection,null,ctrlList);
						baseSection.ResumeLayout();
					} else {
						throw new MissingSectionException();
					}
				} else {
					throw new MissingSectionException();
				}
				baseSection.ResumeLayout();
			}
			baseSection.ResumeLayout();
		}
		
		
		void SetReportItems(BaseSection baseSection,
		                    IContainerItem parentContainer,XmlNodeList ctrlList) {
			
			BaseReportItem baseReportItem;
			//BaseReportItem parentItem;
			foreach (XmlNode ctrlNode in ctrlList) {
				XmlElement ctrlElem = ctrlNode as XmlElement;
				if (ctrlElem != null) {
					IItemRenderer itemRenderer = null;
					try {
						itemRenderer = designableFactory.Create(ctrlElem.GetAttribute("type"));
						
						baseReportItem = (BaseReportItem)itemRenderer;
						if (parentContainer == null) {
//							System.Console.WriteLine("\tParent of {0} is Section",baseReportItem.Name);
							baseReportItem.Parent = baseSection;
							baseSection.Items.Add (baseReportItem);
						} else {
//							System.Console.WriteLine("\tParent of <{0}> is Container",baseReportItem.Name);
							baseReportItem.Parent = parentContainer;
							parentContainer.Items.Add(baseReportItem);
							
						}
						
						XmlHelper.BuildControl (base.XmlFormReader,ctrlElem,baseReportItem);
						
						IContainerItem iContainer = baseReportItem as IContainerItem;

						XmlNodeList newList = ctrlNode.SelectNodes (base.NodesQuery);
						if (newList.Count > 0) {
//							System.Console.WriteLine("\t recusiv call for <{0}> with {1} childs ",
//							                         baseReportItem,newList.Count);
							SetReportItems (baseSection,iContainer,newList);
						}
					}
					catch (Exception ) {
						throw new UnkownItemException();
					}
				}
			}
		}
	}
}
