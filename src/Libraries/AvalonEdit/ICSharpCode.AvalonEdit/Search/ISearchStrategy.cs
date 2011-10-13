﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Search
{
	/// <summary>
	/// Basic interface for search algorithms.
	/// </summary>
	public interface ISearchStrategy
	{
		/// <summary>
		/// Finds all matches in the given ITextSource and the given range.
		/// </summary>
		/// <remarks>This method is thread-safe. Implementers must guarantee this!</remarks>
		IEnumerable<ISearchResult> FindAll(ITextSource document, int offset, int length);
		
		/// <summary>
		/// Finds the next match in the given ITextSource and the given range.
		/// </summary>
		/// <remarks>This method is thread-safe. Implementers must guarantee this!</remarks>
		ISearchResult FindNext(ITextSource document, int offset, int length);
	}
	
	/// <summary>
	/// Represents a search result.
	/// </summary>
	public interface ISearchResult : ISegment
	{
		
	}
	
	/// <summary>
	/// Defines supported search modes.
	/// </summary>
	public enum SearchMode
	{
		/// <summary>
		/// Standard search
		/// </summary>
		Normal,
		/// <summary>
		/// RegEx search
		/// </summary>
		RegEx,
		/// <summary>
		/// Wildcard search
		/// </summary>
		Wildcard
	}
	
	/// <inheritdoc/>
	public class SearchPatternException : Exception, ISerializable
	{
		/// <inheritdoc/>
		public SearchPatternException()
		{
		}
		
		/// <inheritdoc/>
		public SearchPatternException(string message) : base(message)
		{
		}
		
		/// <inheritdoc/>
		public SearchPatternException(string message, Exception innerException) : base(message, innerException)
		{
		}

		// This constructor is needed for serialization.
		/// <inheritdoc/>
		protected SearchPatternException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}