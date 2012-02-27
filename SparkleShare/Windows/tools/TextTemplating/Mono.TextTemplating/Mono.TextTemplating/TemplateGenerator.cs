// 
// TemplatingHost.cs
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
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating
{
	public class TemplateGenerator : MarshalByRefObject, ITextTemplatingEngineHost
	{
		//re-usable
		TemplatingEngine engine;
		
		//per-run variables
		string inputFile, outputFile;
		Encoding encoding;
		
		//host fields
		CompilerErrorCollection errors = new CompilerErrorCollection ();
		List<string> refs = new List<string> ();
		List<string> imports = new List<string> ();
		List<string> includePaths = new List<string> ();
		List<string> referencePaths = new List<string> ();
		
		//host properties for consumers to access
		public CompilerErrorCollection Errors { get { return errors; } }
		public List<string> Refs { get { return refs; } }
		public List<string> Imports { get { return imports; } }
		public List<string> IncludePaths { get { return includePaths; } }
		public List<string> ReferencePaths { get { return referencePaths; } }
		public string OutputFile { get { return outputFile; } }
		
		public TemplateGenerator ()
		{
			Refs.Add (typeof (TextTransformation).Assembly.Location);
			Refs.Add (typeof(System.Uri).Assembly.Location);
			Imports.Add ("System");
		}
		
		public CompiledTemplate CompileTemplate (string content)
		{
			if (String.IsNullOrEmpty (content))
				throw new ArgumentNullException ("content");

			errors.Clear ();
			encoding = Encoding.UTF8;
			
			return Engine.CompileTemplate (content, this);
		}
		
		protected TemplatingEngine Engine {
			get {
				if (engine == null)
					engine = new TemplatingEngine ();
				return engine;
			}
		}
		
		public bool ProcessTemplate (string inputFile, string outputFile)
		{
			if (String.IsNullOrEmpty (inputFile))
				throw new ArgumentNullException ("inputFile");
			if (String.IsNullOrEmpty (outputFile))
				throw new ArgumentNullException ("outputFile");
			
			string content;
			try {
				content = File.ReadAllText (inputFile);
			} catch (IOException ex) {
				errors.Clear ();
				AddError ("Could not read input file '" + inputFile + "':\n" + ex.ToString ());
				return false;
			}
			
			string output;
			ProcessTemplate (inputFile, content, ref outputFile, out output);
			
			try {
				if (!errors.HasErrors)
					File.WriteAllText (outputFile, output, encoding);
			} catch (IOException ex) {
				AddError ("Could not write output file '" + outputFile + "':\n" + ex.ToString ());
			}
			
			return !errors.HasErrors;
		}
		
		public bool ProcessTemplate (string inputFileName, string inputContent, ref string outputFileName, out string outputContent)
		{
			errors.Clear ();
			encoding = Encoding.UTF8;
			
			this.outputFile = outputFileName;
			this.inputFile = inputFileName;
			outputContent = Engine.ProcessTemplate (inputContent, this);
			outputFileName = this.outputFile;
			
			return !errors.HasErrors;
		}
		
		public bool PreprocessTemplate (string inputFile, string className, string classNamespace, 
			string outputFile, System.Text.Encoding encoding, out string language, out string[] references)
		{
			language = null;
			references = null;

			if (string.IsNullOrEmpty (inputFile))
				throw new ArgumentNullException ("inputFile");
			if (string.IsNullOrEmpty (outputFile))
				throw new ArgumentNullException ("outputFile");
			
			string content;
			try {
				content = File.ReadAllText (inputFile);
			} catch (IOException ex) {
				errors.Clear ();
				AddError ("Could not read input file '" + inputFile + "':\n" + ex.ToString ());
				return false;
			}
			
			string output;
			PreprocessTemplate (inputFile, className, classNamespace, content, out language, out references, out output);
			
			try {
				if (!errors.HasErrors)
					File.WriteAllText (outputFile, output, encoding);
			} catch (IOException ex) {
				AddError ("Could not write output file '" + outputFile + "':\n" + ex.ToString ());
			}
			
			return !errors.HasErrors;
		}
		
		public bool PreprocessTemplate (string inputFileName, string className, string classNamespace, string inputContent, 
			out string language, out string[] references, out string outputContent)
		{
			errors.Clear ();
			encoding = Encoding.UTF8;
			
			this.inputFile = inputFileName;
			outputContent = Engine.PreprocessTemplate (inputContent, this, className, classNamespace, out language, out references);
			
			return !errors.HasErrors;
		}
		
		CompilerError AddError (string error)
		{
			CompilerError err = new CompilerError ();
			err.ErrorText = error;
			Errors.Add (err);
			return err;
		}
		
		#region Virtual members
		
		public virtual object GetHostOption (string optionName)
		{
			return null;
		}
		
		public virtual AppDomain ProvideTemplatingAppDomain (string content)
		{
			return null;
		}
		
		//FIXME: implement
		protected virtual string ResolveAssemblyReference (string assemblyReference)
		{
			//foreach (string referencePath in ReferencePaths) {
			//	
			//}
			return assemblyReference;
		}
		
		protected virtual string ResolveParameterValue (string directiveId, string processorName, string parameterName)
		{
			var key = new ParameterKey (processorName, directiveId, parameterName);
			string value;
			if (parameters.TryGetValue (key, out value))
				return value;
			if (processorName != null || directiveId != null)
				return ResolveParameterValue (null, null, parameterName);
			return null;
		}
		
		protected virtual Type ResolveDirectiveProcessor (string processorName)
		{
			KeyValuePair<string,string> value;
			if (!directiveProcessors.TryGetValue (processorName, out value))
				throw new Exception (string.Format ("No directive processor registered as '{0}'", processorName));
			var asmPath = ResolveAssemblyReference (value.Value);
			if (asmPath == null)
				throw new Exception (string.Format ("Could not resolve assembly '{0}' for directive processor '{1}'", value.Value, processorName));
			var asm = System.Reflection.Assembly.LoadFrom (asmPath);
			return asm.GetType (value.Key, true);
		}
		
		protected virtual string ResolvePath (string path)
		{
			path = System.Environment.ExpandEnvironmentVariables (path);
			if (Path.IsPathRooted (path))
				return path;
			var dir = Path.GetDirectoryName (inputFile);
			var test = Path.Combine (dir, path);
			if (File.Exists (test))
				return test;
			return null;
		}
		
		#endregion
		
		Dictionary<ParameterKey,string> parameters = new Dictionary<ParameterKey, string> ();
		Dictionary<string,KeyValuePair<string,string>> directiveProcessors = new Dictionary<string, KeyValuePair<string,string>> ();
		
		public void AddDirectiveProcessor (string name, string klass, string assembly)
		{
			directiveProcessors.Add (name, new KeyValuePair<string,string> (klass,assembly));
		}
		
		public void AddParameter (string processorName, string directiveName, string parameterName, string value)
		{
			parameters.Add (new ParameterKey (processorName, directiveName, parameterName), value);
		}
		
		protected virtual bool LoadIncludeText (string requestFileName, out string content, out string location)
		{
			content = "";
			location = ResolvePath (requestFileName);
			
			if (location == null) {
				foreach (string path in includePaths) {
					string f = Path.Combine (path, requestFileName);
					if (File.Exists (f)) {
						location = f;
						break;
					}
				}
			}
			
			if (location == null)
				return false;
			
			try {
				content = File.ReadAllText (location);
				return true;
			} catch (IOException ex) {
				AddError ("Could not read included file '" + location +  "':\n" + ex.ToString ());
			}
			return false;
		}
		
		#region Explicit ITextTemplatingEngineHost implementation
		
		bool ITextTemplatingEngineHost.LoadIncludeText (string requestFileName, out string content, out string location)
		{
			return LoadIncludeText (requestFileName, out content, out location);
		}
		
		void ITextTemplatingEngineHost.LogErrors (CompilerErrorCollection errors)
		{
			this.errors.AddRange (errors);
		}
		
		string ITextTemplatingEngineHost.ResolveAssemblyReference (string assemblyReference)
		{
			return ResolveAssemblyReference (assemblyReference);
		}
		
		string ITextTemplatingEngineHost.ResolveParameterValue (string directiveId, string processorName, string parameterName)
		{
			return ResolveParameterValue (directiveId, processorName, parameterName);
		}
		
		Type ITextTemplatingEngineHost.ResolveDirectiveProcessor (string processorName)
		{
			return ResolveDirectiveProcessor (processorName);
		}
		
		string ITextTemplatingEngineHost.ResolvePath (string path)
		{
			return ResolvePath (path);
		}
		
		void ITextTemplatingEngineHost.SetFileExtension (string extension)
		{
			extension = extension.TrimStart ('.');
			if (Path.HasExtension (outputFile)) {
				outputFile = Path.ChangeExtension (outputFile, extension);
			} else {
				outputFile = outputFile + "." + extension;
			}
		}
		
		void ITextTemplatingEngineHost.SetOutputEncoding (System.Text.Encoding encoding, bool fromOutputDirective)
		{
			this.encoding = encoding;
		}
		
		IList<string> ITextTemplatingEngineHost.StandardAssemblyReferences {
			get { return refs; }
		}
		
		IList<string> ITextTemplatingEngineHost.StandardImports {
			get { return imports; }
		}
		
		string ITextTemplatingEngineHost.TemplateFile {
			get { return inputFile; }
		}
		
		#endregion
		
		struct ParameterKey : IEquatable<ParameterKey>
		{
			public ParameterKey (string processorName, string directiveName, string parameterName)
			{
				this.processorName = processorName ?? "";
				this.directiveName = directiveName ?? "";
				this.parameterName = parameterName ?? "";
				unchecked {
					hashCode = this.processorName.GetHashCode ()
						^ this.directiveName.GetHashCode ()
						^ this.parameterName.GetHashCode ();
				}
			}
			
			string processorName, directiveName, parameterName;
			int hashCode;
			
			public override bool Equals (object obj)
			{
				return obj != null && obj is ParameterKey && Equals ((ParameterKey)obj);
			}
			
			public bool Equals (ParameterKey other)
			{
				return processorName == other.processorName && directiveName == other.directiveName && parameterName == other.parameterName;
			}
			
			public override int GetHashCode ()
			{
				return hashCode;
			}
		}
	}
}
