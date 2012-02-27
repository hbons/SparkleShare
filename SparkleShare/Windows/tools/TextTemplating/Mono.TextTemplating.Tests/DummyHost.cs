// 
// IncludeFileProviderHost.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.Collections.Generic;
using System.CodeDom.Compiler;
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating.Tests
{
	
	public class DummyHost : ITextTemplatingEngineHost
	{
		public readonly Dictionary<string, string> Locations = new Dictionary<string, string> ();
		public readonly Dictionary<string, string> Contents = new Dictionary<string, string> ();
		public readonly Dictionary<string, object> HostOptions = new Dictionary<string, object> ();
	 	List<string> standardAssemblyReferences = new List<string> ();
		List<string> standardImports = new List<string> ();
		public readonly CompilerErrorCollection Errors = new CompilerErrorCollection ();
		public readonly Dictionary<string, Type> DirectiveProcessors = new Dictionary<string, Type> ();
		
		public virtual object GetHostOption (string optionName)
		{
			object o;
			HostOptions.TryGetValue (optionName, out o);
			return o;
		}
		
		public virtual bool LoadIncludeText (string requestFileName, out string content, out string location)
		{
			content = null;
			return Locations.TryGetValue (requestFileName, out location)
				&& Contents.TryGetValue (requestFileName, out content);
		}
		
		public virtual void LogErrors (CompilerErrorCollection errors)
		{
			Errors.AddRange (errors);
		}
		
		public virtual AppDomain ProvideTemplatingAppDomain (string content)
		{
			return null;
		}
		
		public virtual string ResolveAssemblyReference (string assemblyReference)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual Type ResolveDirectiveProcessor (string processorName)
		{
			Type t;
			DirectiveProcessors.TryGetValue (processorName, out t);
			return t;
		}
		
		public virtual string ResolveParameterValue (string directiveId, string processorName, string parameterName)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual string ResolvePath (string path)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual void SetFileExtension (string extension)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual void SetOutputEncoding (System.Text.Encoding encoding, bool fromOutputDirective)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual IList<string> StandardAssemblyReferences {
			get { return standardAssemblyReferences; }
		}
		
		public virtual IList<string> StandardImports {
			get { return standardImports; }
		}
		
		public virtual string TemplateFile {
			get; set;
		}
	}
}
