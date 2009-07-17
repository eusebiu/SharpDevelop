﻿// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Martin Koníček" email="martin.konicek@gmail.com"/>
//     <version>$Revision$</version>
// </file>
using Debugger.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using Debugger.AddIn.Visualizers.Graph.Layout;

namespace Debugger.AddIn.Visualizers.Graph
{
	/// <summary>
	/// Remembers which properties the user has expanded in the <see cref="PositionedGraph">.
	/// </summary>
	public class ExpandedExpressions
	{
		private ExpandedPaths expanded = new ExpandedPaths();
		
		public ExpandedExpressions()
		{
		}
		
		public bool IsExpanded(Expression expression)
		{
			return expanded.IsExpanded(expression.Code);
		}
		
		public void SetExpanded(Expression expression)
		{
			expanded.SetExpanded(expression.Code);
		}
		
		public void SetCollapsed(Expression expression)
		{
			expanded.SetCollapsed(expression.Code);
		}
	}
}