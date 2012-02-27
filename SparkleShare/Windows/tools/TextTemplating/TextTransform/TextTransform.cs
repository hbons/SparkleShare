// 
// Main.cs
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
using System.IO;
using System.Collections.Generic;
using Mono.Options;

namespace Mono.TextTemplating
{
	class TextTransform
	{
		static OptionSet optionSet;
		const string name ="TextTransform.exe";
		
		public static int Main (string[] args)
		{
			if (args.Length == 0) {
				ShowHelp (true);
			}
			
			var generator = new TemplateGenerator ();
			string outputFile = null, inputFile = null;
			var directives = new List<string> ();
			var parameters = new List<string> ();
			var session = new Microsoft.VisualStudio.TextTemplating.TextTemplatingSession ();
			string preprocess = null;
			
			optionSet = new OptionSet () {
				{ "o=|out=", "The name of the output {file}", s => outputFile = s },
				{ "r=", "Assemblies to reference", s => generator.Refs.Add (s) },
				{ "u=", "Namespaces to import <{0:namespace}>", s => generator.Imports.Add (s) },
				{ "I=", "Paths to search for included files", s => generator.IncludePaths.Add (s) },
				{ "P=", "Paths to search for referenced assemblies", s => generator.ReferencePaths.Add (s) },
				{ "dp=", "Directive processor (name!class!assembly)", s => directives.Add (s) },
				{ "a=", "Parameters ([processorName]![directiveName]!name!value)", s => parameters.Add (s) },
				{ "h|?|help", "Show help", s => ShowHelp (false) },
		//		{ "k=,", "Session {key},{value} pairs", (s, t) => session.Add (s, t) },
				{ "c=", "Preprocess the template into {0:class}", (s) => preprocess = s },
			};
			
			var remainingArgs = optionSet.Parse (args);
			
			if (string.IsNullOrEmpty (outputFile)) {
				Console.Error.WriteLine ("No output file specified.");
				return -1;
			}
			
			if (remainingArgs.Count != 1) {
				Console.Error.WriteLine ("No input file specified.");
				return -1;
			}
			inputFile = remainingArgs [0];
			
			if (!File.Exists (inputFile)) {
				Console.Error.WriteLine ("Input file '{0}' does not exist.");
				return -1;
			}
			
			//FIXME: implement quoting and escaping for values
			foreach (var par in parameters) {
				var split = par.Split ('!');
				if (split.Length < 2) {
					Console.Error.WriteLine ("Parameter does not have enough values: {0}", par);
					return -1;
				}
				if (split.Length > 2) {
					Console.Error.WriteLine ("Parameter has too many values: {0}", par);
					return -1;
				}
				string name = split[split.Length-2];
				string val  = split[split.Length-1];
				if (string.IsNullOrEmpty (name)) {
					Console.Error.WriteLine ("Parameter has no name: {0}", par);
					return -1;
				}
				generator.AddParameter (split.Length > 3? split[0] : null, split.Length > 2? split[split.Length-3] : null, name, val);
			}
			
			foreach (var dir in directives) {
				var split = dir.Split ('!');
				if (split.Length != 3) {
					Console.Error.WriteLine ("Directive does not have correct number of values: {0}", dir);
					return -1;
				}
				foreach (var s in split) {
					if (string.IsNullOrEmpty (s)) {
						Console.Error.WriteLine ("Directive has missing value: {0}", dir);
						return -1;
					}
				}
				generator.AddDirectiveProcessor (split[0], split[1], split[2]);
			}
			
			if (preprocess == null) {
				Console.Write ("Processing '{0}'... ", inputFile);
				generator.ProcessTemplate (inputFile, outputFile);
				if (generator.Errors.HasErrors) {
					Console.WriteLine ("failed.");
				} else {
					Console.WriteLine ("completed successfully.");
				}
			} else {
				string className = preprocess;
				string classNamespace = null;
				int s = preprocess.LastIndexOf ('.');
				if (s > 0) {
					classNamespace = preprocess.Substring (0, s);
					className = preprocess.Substring (s + 1);
				}
				
				Console.Write ("Preprocessing '{0}' into class '{1}.{2}'... ", inputFile, classNamespace, className);
				string language;
				string[] references;
				generator.PreprocessTemplate (inputFile, className, classNamespace, outputFile, System.Text.Encoding.UTF8,
					out language, out references);
				if (generator.Errors.HasErrors) {
					Console.WriteLine ("failed.");
				} else {
					Console.WriteLine ("completed successfully:");
					Console.WriteLine ("    Language: {0}", language);
					if (references != null && references.Length > 0) {
						Console.WriteLine (" References:");
						foreach (string r in references)
							Console.WriteLine ("    {0}", r);
					}
				}
			}
			
			foreach (System.CodeDom.Compiler.CompilerError err in generator.Errors)
				Console.Error.WriteLine ("{0}({1},{2}): {3} {4}", err.FileName, err.Line, err.Column,
				                   err.IsWarning? "WARNING" : "ERROR", err.ErrorText);
			
			return generator.Errors.HasErrors? -1 : 0;
		}
		
		static void ShowHelp (bool concise)
		{
			Console.WriteLine ("TextTransform command line T4 processor");
			Console.WriteLine ("Usage: {0} [options] input-file", name);
			if (concise) {
				Console.WriteLine ("Use --help to display options.");
			} else {
				Console.WriteLine ("Options:");
				optionSet.WriteOptionDescriptions (System.Console.Out);
			}
			Console.WriteLine ();
			Environment.Exit (0);
		}
	}
}