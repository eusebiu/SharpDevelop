﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents a method, constructor, destructor or operator.
	/// </summary>
	#if WITH_CONTRACTS
	[ContractClass(typeof(IMethodContract))]
	#endif
	public interface IMethod : IParameterizedMember
	{
		/// <summary>
		/// Gets the attributes associated with the return type. (e.g. [return: MarshalAs(...)])
		/// </summary>
		IList<IAttribute> ReturnTypeAttributes { get; }
		
		IList<ITypeParameter> TypeParameters { get; }
		
		// handles is VB-specific and not part of the public API, so
		// we don't really need it
		//IList<string> HandlesClauses { get; }
		
		bool IsExtensionMethod { get; }
		bool IsConstructor { get; }
		bool IsDestructor { get; }
		bool IsOperator { get; }
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(IMethod))]
	abstract class IMethodContract : IParameterizedMemberContract, IMethod
	{
		IList<IAttribute> IMethod.ReturnTypeAttributes {
			get {
				Contract.Ensures(Contract.Result<IList<IAttribute>>() != null);
				return null;
			}
		}
		
		IList<ITypeParameter> IMethod.TypeParameters {
			get {
				Contract.Ensures(Contract.Result<IList<ITypeParameter>>() != null);
				return null;
			}
		}
		
		bool IMethod.IsExtensionMethod {
			get { return false; }
		}
		
		bool IMethod.IsConstructor {
			get { return false; }
		}
		
		bool IMethod.IsDestructor {
			get { return false; }
		}
		
		bool IMethod.IsOperator {
			get { return false; }
		}
	}
	#endif
}
