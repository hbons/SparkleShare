// 
// CompiledTemplate.cs
//  
// Author:
//       Nathan Baulch <nathan.baulch@gmail.com>
// 
// Copyright (c) 2009 Nathan Baulch
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
using System.Reflection;
using Microsoft.VisualStudio.TextTemplating;
using System.CodeDom.Compiler;
using System.Globalization;

namespace Mono.TextTemplating
{
	public sealed class CompiledTemplate : MarshalByRefObject, IDisposable
	{
		ITextTemplatingEngineHost host;
		TextTransformation tt;
		CultureInfo culture;
		string[] assemblyFiles;
		
		public CompiledTemplate (ITextTemplatingEngineHost host, CompilerResults results, string fullName, CultureInfo culture,
			string[] assemblyFiles)
		{
			AppDomain.CurrentDomain.AssemblyResolve += ResolveReferencedAssemblies;
			this.host = host;
			this.culture = culture;
			this.assemblyFiles = assemblyFiles;
			Load (results, fullName);
		}
		
		void Load (CompilerResults results, string fullName)
		{
			var assembly = results.CompiledAssembly;
			Type transformType = assembly.GetType (fullName);
			tt = (TextTransformation) Activator.CreateInstance (transformType);
			
			//set the host property if it exists
			var hostProp = transformType.GetProperty ("Host", typeof (ITextTemplatingEngineHost));
			if (hostProp != null && hostProp.CanWrite)
				hostProp.SetValue (tt, host, null);
			
			var sessionHost = host as ITextTemplatingSessionHost;
			if (sessionHost != null) {
				//FIXME: should we create a session if it's null?
				tt.Session = sessionHost.Session;
			}
		}

		public string Process ()
		{
			tt.Errors.Clear ();
			
			//set the culture
			if (culture != null)
				ToStringHelper.FormatProvider = culture;
			else
				ToStringHelper.FormatProvider = CultureInfo.InvariantCulture;
			
			tt.Initialize ();
			
			string output = null;
			try {
				output = tt.TransformText ();
			} catch (Exception ex) {
				tt.Error ("Error running transform: " + ex.ToString ());
			}
			host.LogErrors (tt.Errors);
			
			ToStringHelper.FormatProvider = CultureInfo.InvariantCulture;
			return output;
		}
		
		System.Reflection.Assembly ResolveReferencedAssemblies (object sender, ResolveEventArgs args)
		{
			System.Reflection.Assembly asm = null;
			foreach (var asmFile in assemblyFiles) {
				var name = System.IO.Path.GetFileNameWithoutExtension (asmFile);
				if (args.Name.StartsWith (name))
					asm = System.Reflection.Assembly.LoadFrom (asmFile);
			}
			return asm;
		}
		
		public void Dispose ()
		{
			if (host != null) {
				host = null;
				AppDomain.CurrentDomain.AssemblyResolve -= ResolveReferencedAssemblies;
			}
		}
	}
}
