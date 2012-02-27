// 
// ParameterDirectiveProcessor.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.CodeDom;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.VisualStudio.TextTemplating
{
	public sealed class ParameterDirectiveProcessor : DirectiveProcessor, IRecognizeHostSpecific
	{
		CodeDomProvider provider;
		bool isCSharp;
		bool useMonoHack;
		
		bool hostSpecific;
		List<CodeStatement> postStatements = new List<CodeStatement> ();
		CodeTypeMemberCollection members = new CodeTypeMemberCollection ();
		
		public ParameterDirectiveProcessor ()
		{
		}
		
		public override void StartProcessingRun (CodeDomProvider languageProvider, string templateContents, CompilerErrorCollection errors)
		{
			base.StartProcessingRun (languageProvider, templateContents, errors);
			this.provider = languageProvider;
			//HACK: Mono as of 2.10.2 doesn't implement GenerateCodeFromMember
			if (Type.GetType ("Mono.Runtime") != null)
				useMonoHack = true;
			if (languageProvider is Microsoft.CSharp.CSharpCodeProvider)
				isCSharp = true;
			postStatements.Clear ();
			members.Clear ();
		}
		
		public override void FinishProcessingRun ()
		{
			var statement = new CodeConditionStatement (
				new CodeBinaryOperatorExpression (
					new CodePropertyReferenceExpression (
						new CodePropertyReferenceExpression (new CodeThisReferenceExpression (), "Errors"), "HasErrors"),
					CodeBinaryOperatorType.ValueEquality,
					new CodePrimitiveExpression (false)),
				postStatements.ToArray ());
			
			postStatements.Clear ();
			postStatements.Add (statement);
		}
		
		public override string GetClassCodeForProcessingRun ()
		{
			var options = new CodeGeneratorOptions ();
			using (var sw = new StringWriter ()) {
				GenerateCodeFromMembers (sw, options);
				return Indent (sw.ToString (), "        ");
			}
		}
		
		string Indent (string s, string indent)
		{
			if (isCSharp)
				return Mono.TextTemplating.TemplatingEngine.IndentSnippetText (s, indent);
			return s;
		}
		
		public override string[] GetImportsForProcessingRun ()
		{
			return null;
		}
		
		public override string GetPostInitializationCodeForProcessingRun ()
		{
			return Indent (StatementsToCode (postStatements), "            ");
		}
		
		public override string GetPreInitializationCodeForProcessingRun ()
		{
			return null;
		}
		
		string StatementsToCode (List<CodeStatement> statements)
		{
			var options = new CodeGeneratorOptions ();
			using (var sw = new StringWriter ()) {
				foreach (var statement in statements)
					provider.GenerateCodeFromStatement (statement, sw, options);
				return sw.ToString ();
			}
		}
		
		public override string[] GetReferencesForProcessingRun ()
		{
			return null;
		}
		
		public override bool IsDirectiveSupported (string directiveName)
		{
			return directiveName == "parameter";
		}
		
		public override void ProcessDirective (string directiveName, IDictionary<string, string> arguments)
		{
			string name = arguments["name"];
			string type = arguments["type"];
			if (string.IsNullOrEmpty (name))
				throw new DirectiveProcessorException ("Parameter directive has no name argument");
			if (string.IsNullOrEmpty (type))
				throw new DirectiveProcessorException ("Parameter directive has no type argument");
			
			string fieldName = "_" + name + "Field";
			var typeRef = new CodeTypeReference (type);
			var thisRef = new CodeThisReferenceExpression ();
			var fieldRef = new CodeFieldReferenceExpression (thisRef, fieldName);
			
			var property = new CodeMemberProperty () {
				Name = name,
				Attributes = MemberAttributes.Public | MemberAttributes.Final,
				HasGet = true,
				HasSet = false,
				Type = typeRef
			};
			property.GetStatements.Add (new CodeMethodReturnStatement (fieldRef));
			members.Add (new CodeMemberField (typeRef, fieldName));
			members.Add (property);
			
			string acquiredName = "_" + name + "Acquired";
			var valRef = new CodeVariableReferenceExpression ("data");
			var namePrimitive = new CodePrimitiveExpression (name);
			var sessionRef = new CodePropertyReferenceExpression (thisRef, "Session");
			var callContextTypeRefExpr = new CodeTypeReferenceExpression ("System.Runtime.Remoting.Messaging.CallContext");
			var nullPrim = new CodePrimitiveExpression (null);
			
			var acquiredVariable = new CodeVariableDeclarationStatement (typeof (bool), acquiredName, new CodePrimitiveExpression (false));
			var acquiredVariableRef = new CodeVariableReferenceExpression (acquiredVariable.Name);
			this.postStatements.Add (acquiredVariable);
			
			//checks the local called "data" can be cast and assigned to the field, and if successful, sets acquiredVariable to true
			var checkCastThenAssignVal = new CodeConditionStatement (
				new CodeMethodInvokeExpression (
					new CodeTypeOfExpression (typeRef), "IsAssignableFrom", new CodeMethodInvokeExpression (valRef, "GetType")),
				new CodeStatement[] {
					new CodeAssignStatement (fieldRef, new CodeCastExpression (typeRef, valRef)),
					new CodeAssignStatement (acquiredVariableRef, new CodePrimitiveExpression (true)),
				},
				new CodeStatement[] {
					new CodeExpressionStatement (new CodeMethodInvokeExpression (thisRef, "Error",
					new CodePrimitiveExpression ("The type '" + type + "' of the parameter '" + name + 
						"' did not match the type passed to the template"))),
				});
			
			//tries to gets the value from the session
			var checkSession = new CodeConditionStatement (
				new CodeBinaryOperatorExpression (NotNull (sessionRef), CodeBinaryOperatorType.BooleanAnd,
					new CodeMethodInvokeExpression (sessionRef, "ContainsKey", namePrimitive)),
				new CodeVariableDeclarationStatement (typeof (object), "data", new CodeIndexerExpression (sessionRef, namePrimitive)),
				checkCastThenAssignVal);
			
			this.postStatements.Add (checkSession);
			
			//if acquiredVariable is false, tries to gets the value from the host
			if (hostSpecific) {
				var hostRef = new CodePropertyReferenceExpression (thisRef, "Host");
				var checkHost = new CodeConditionStatement (
					BooleanAnd (IsFalse (acquiredVariableRef), NotNull (hostRef)),
					new CodeVariableDeclarationStatement (typeof (string), "data",
						new CodeMethodInvokeExpression (hostRef, "ResolveParameterValue", nullPrim, nullPrim,  namePrimitive)),
					new CodeConditionStatement (NotNull (valRef), checkCastThenAssignVal));
				
				this.postStatements.Add (checkHost);
			}
			
			//if acquiredVariable is false, tries to gets the value from the call context
			var checkCallContext = new CodeConditionStatement (
				IsFalse (acquiredVariableRef),
				new CodeVariableDeclarationStatement (typeof (object), "data",
					new CodeMethodInvokeExpression (callContextTypeRefExpr, "LogicalGetData", namePrimitive)),
				new CodeConditionStatement (NotNull (valRef), checkCastThenAssignVal));
			
			this.postStatements.Add (checkCallContext);
		}
		
		static CodeBinaryOperatorExpression NotNull (CodeExpression reference)
		{
			return new CodeBinaryOperatorExpression (reference, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression (null));
		}
		
		static CodeBinaryOperatorExpression IsFalse (CodeExpression expr)
		{
			return new CodeBinaryOperatorExpression (expr, CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression (false));
		}
		
		static CodeBinaryOperatorExpression BooleanAnd (CodeExpression expr1, CodeExpression expr2)
		{
			return new CodeBinaryOperatorExpression (expr1, CodeBinaryOperatorType.BooleanAnd, expr2);
		}
		
		void IRecognizeHostSpecific.SetProcessingRunIsHostSpecific (bool hostSpecific)
		{
			this.hostSpecific = hostSpecific;
		}

		public bool RequiresProcessingRunIsHostSpecific {
			get { return false; }
		}
		
		void GenerateCodeFromMembers (StringWriter sw, CodeGeneratorOptions options)
		{
			if (!useMonoHack) {
				foreach (CodeTypeMember member in members)
					provider.GenerateCodeFromMember (member, sw, options);
			}
			
			var cgType = typeof (CodeGenerator);
			var cgInit = cgType.GetMethod ("InitOutput", BindingFlags.NonPublic | BindingFlags.Instance);
			var cgFieldGen = cgType.GetMethod ("GenerateField", BindingFlags.NonPublic | BindingFlags.Instance);
			var cgPropGen = cgType.GetMethod ("GenerateProperty", BindingFlags.NonPublic | BindingFlags.Instance);
			
#pragma warning disable 0618
			var generator = (CodeGenerator) provider.CreateGenerator ();
#pragma warning restore 0618
			var dummy = new CodeTypeDeclaration ("Foo");
			
			foreach (CodeTypeMember member in members) {
				var f = member as CodeMemberField;
				if (f != null) {
					cgInit.Invoke (generator, new object[] { sw, options });
					cgFieldGen.Invoke (generator, new object[] { f });
					continue;
				}
				var p = member as CodeMemberProperty;
				if (p != null) {
					cgInit.Invoke (generator, new object[] { sw, options });
					cgPropGen.Invoke (generator, new object[] { p, dummy });
					continue;
				}
			}
		}
	}
}

