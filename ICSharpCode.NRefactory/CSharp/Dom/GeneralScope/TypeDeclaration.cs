﻿// 
// TypeDeclaration.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	public class TypeDeclaration : AbstractMemberBase
	{
		public const int TypeKeyword      = 100;
		
		public Identifier NameIdentifier {
			get {
				return (Identifier)GetChildByRole (Roles.Identifier);
			}
		}
		
		public string Name {
			get {
				return NameIdentifier.Name;
			}
		}
		
		// TODO: rename to TypeParameters, more specific return type
		public IEnumerable<INode> TypeArguments {
			get {
				return GetChildrenByRole (Roles.TypeArgument).Cast<INode> ();
			}
		}
		
		public IEnumerable<Constraint> Constraints { 
			get {
				return base.GetChildrenByRole (Roles.Constraint).Cast <Constraint> ();
			}
		}
		
		public CSharpTokenNode LBrace {
			get {
				return (CSharpTokenNode)GetChildByRole (Roles.LBrace);
			}
		}
		
		public CSharpTokenNode RBrace {
			get {
				return (CSharpTokenNode)GetChildByRole (Roles.RBrace);
			}
		}
		
		public virtual ClassType ClassType {
			get;
			set;
		}
		
		public override S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitTypeDeclaration (this, data);
		}
	}
}
