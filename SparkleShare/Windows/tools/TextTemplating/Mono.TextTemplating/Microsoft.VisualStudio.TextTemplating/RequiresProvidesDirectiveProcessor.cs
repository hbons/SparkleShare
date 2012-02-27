// 
// RequiresProvidesDirectiveProcessor.cs
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
using System.Text;

namespace Microsoft.VisualStudio.TextTemplating
{
	
	
	public abstract class RequiresProvidesDirectiveProcessor : DirectiveProcessor
	{
		bool isInProcessingRun;
		ITextTemplatingEngineHost host;
		StringBuilder preInitBuffer = new StringBuilder ();
		StringBuilder postInitBuffer = new StringBuilder ();
		StringBuilder codeBuffer = new StringBuilder ();
		CodeDomProvider languageProvider;
		
		protected RequiresProvidesDirectiveProcessor ()
		{
		}
		
		public override void Initialize (ITextTemplatingEngineHost host)
		{
			base.Initialize (host);
			this.host = host;
		}
		
		protected abstract void InitializeProvidesDictionary (string directiveName, IDictionary<string, string> providesDictionary);
		protected abstract void InitializeRequiresDictionary (string directiveName, IDictionary<string, string> requiresDictionary);
		protected abstract string FriendlyName { get; }
		
		protected abstract void GeneratePostInitializationCode (string directiveName, StringBuilder codeBuffer, CodeDomProvider languageProvider, 
			IDictionary<string, string> requiresArguments, IDictionary<string, string> providesArguments);
		protected abstract void GeneratePreInitializationCode (string directiveName, StringBuilder codeBuffer, CodeDomProvider languageProvider,
			IDictionary<string, string> requiresArguments, IDictionary<string, string> providesArguments);
		protected abstract void GenerateTransformCode (string directiveName, StringBuilder codeBuffer, CodeDomProvider languageProvider,
			IDictionary<string, string> requiresArguments, IDictionary<string, string> providesArguments);
		
		protected virtual void PostProcessArguments (string directiveName, IDictionary<string, string> requiresArguments,
			IDictionary<string, string> providesArguments)
		{
		}
		
		public override string GetClassCodeForProcessingRun ()
		{
			AssertNotProcessing ();
			return codeBuffer.ToString ();
		}
		
		public override string[] GetImportsForProcessingRun ()
		{
			AssertNotProcessing ();
			return null;
		}
		
		public override string[] GetReferencesForProcessingRun ()
		{
			AssertNotProcessing ();
			return null;
		}
		
		public override string GetPostInitializationCodeForProcessingRun ()
		{
			AssertNotProcessing ();
			return postInitBuffer.ToString ();
		}
		
		public override string GetPreInitializationCodeForProcessingRun ()
		{
			AssertNotProcessing ();
			return preInitBuffer.ToString ();
		}
		
		public override void StartProcessingRun (CodeDomProvider languageProvider, string templateContents, CompilerErrorCollection errors)
		{
			AssertNotProcessing ();
			isInProcessingRun = true;
			base.StartProcessingRun (languageProvider, templateContents, errors);
			
			this.languageProvider = languageProvider;
			codeBuffer.Length = 0;
			preInitBuffer.Length = 0;
			postInitBuffer.Length = 0;
		}
		
		public override void FinishProcessingRun ()
		{
			isInProcessingRun = false;
		}
		
		void AssertNotProcessing ()
		{
			if (isInProcessingRun)
				throw new InvalidOperationException ();
		}
		
		//FIXME: handle escaping
		IEnumerable<KeyValuePair<string,string>> ParseArgs (string args)
		{
			var pairs = args.Split (';');
			foreach (var p in pairs) {
				int eq = p.IndexOf ('=');
				var k = p.Substring (0, eq);
				var v = p.Substring (eq);
				yield return new KeyValuePair<string, string> (k, v);
			}
		}
		
		public override void ProcessDirective (string directiveName, IDictionary<string, string> arguments)
		{
			if (directiveName == null)
				throw new ArgumentNullException ("directiveName");
			if (arguments == null)
				throw new ArgumentNullException ("arguments");
			
			var providesDictionary = new Dictionary<string,string> ();
			var requiresDictionary = new Dictionary<string,string> ();
			
			string provides;
			if (arguments.TryGetValue ("provides", out provides)) {
				foreach (var arg in ParseArgs (provides)) {
					providesDictionary.Add (arg.Key, arg.Value);
				}
			}
			
			string requires;
			if (arguments.TryGetValue ("requires", out requires)) {
				foreach (var arg in ParseArgs (requires)) {
					requiresDictionary.Add (arg.Key, arg.Value);
				}
			}
			
			InitializeRequiresDictionary (directiveName, requiresDictionary);
			InitializeProvidesDictionary (directiveName, providesDictionary);
			
			var id = ProvideUniqueId (directiveName, arguments, requiresDictionary, providesDictionary);
			
			foreach (var req in requiresDictionary) {
				var val = host.ResolveParameterValue (id, FriendlyName, req.Key);
				if (val != null)
					requiresDictionary[req.Key] = val; 
				else if (req.Value == null)
					throw new DirectiveProcessorException ("Could not resolve required value '" + req.Key + "'");
			}
			
			foreach (var req in providesDictionary) {
				var val = host.ResolveParameterValue (id, FriendlyName, req.Key);
				if (val != null)
					providesDictionary[req.Key] = val;
			}
			
			PostProcessArguments (directiveName, requiresDictionary, providesDictionary);
			
			GeneratePreInitializationCode (directiveName, preInitBuffer, languageProvider, requiresDictionary, providesDictionary);
			GeneratePostInitializationCode (directiveName, postInitBuffer, languageProvider, requiresDictionary, providesDictionary);
			GenerateTransformCode (directiveName, codeBuffer, languageProvider, requiresDictionary, providesDictionary);
		}
		
		protected virtual string ProvideUniqueId (string directiveName, IDictionary<string, string> arguments,
			IDictionary<string, string> requiresArguments, IDictionary<string, string> providesArguments)
		{
			return directiveName;
		}
		
		protected ITextTemplatingEngineHost Host {
			get { return host; }
		}
	}
}
