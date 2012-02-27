// 
// Tokeniser.cs
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
using System.Diagnostics;

namespace Mono.TextTemplating
{
	
	public class Tokeniser
	{
		string content;
		int position = 0;
		string value;
		State nextState = State.Content;
		Location nextStateLocation;
		Location nextStateTagStartLocation;
		
		public Tokeniser (string fileName, string content)
		{
			State = State.Content;
			this.content = content;
			this.Location = this.nextStateLocation = this.nextStateTagStartLocation = new Location (fileName, 1, 1);
		}
		
		public bool Advance ()
		{
			value = null;
			State = nextState;
			Location = nextStateLocation;
			TagStartLocation = nextStateTagStartLocation;
			if (nextState == State.EOF)
				return false;
			nextState = GetNextStateAndCurrentValue ();
			return true;
		}
		
		State GetNextStateAndCurrentValue ()
		{
			switch (State) {
			case State.Block:
			case State.Expression:
			case State.Helper:
				return GetBlockEnd ();
			
			case State.Directive:
				return NextStateInDirective ();
				
			case State.Content:
				return NextStateInContent ();
				
			case State.DirectiveName:
				return GetDirectiveName ();
				
			case State.DirectiveValue:
				return GetDirectiveValue ();
				
			default:
				throw new InvalidOperationException ("Unexpected state '" + State.ToString () + "'");
			}
		}
		
		State GetBlockEnd ()
		{
			int start = position;
			for (; position < content.Length; position++) {
				char c = content[position];
				nextStateTagStartLocation = nextStateLocation;
				nextStateLocation = nextStateLocation.AddCol ();
				if (c == '\r') {
					if (position + 1 < content.Length && content[position + 1] == '\n')
						position++;
					nextStateLocation = nextStateLocation.AddLine();
				} else if (c == '\n') {
					nextStateLocation = nextStateLocation.AddLine();
				} else if (c =='>' && content[position-1] == '#' && content[position-2] != '\\') {
					value = content.Substring (start, position - start - 1);
					position++;
					TagEndLocation = nextStateLocation;
					
					//skip newlines directly after blocks, unless they're expressions
					if (State != State.Expression && (position += IsNewLine()) > 0) {
						nextStateLocation = nextStateLocation.AddLine ();
					}
					return State.Content;
				}
			}
			throw new ParserException ("Unexpected end of file.", nextStateLocation);
		}
		
		State GetDirectiveName ()
		{
			int start = position;
			for (; position < content.Length; position++) {
				char c = content[position];
				if (!Char.IsLetterOrDigit (c)) {
					value = content.Substring (start, position - start);
					return State.Directive;
				} else {
					nextStateLocation = nextStateLocation.AddCol ();
				}
			}
			throw new ParserException ("Unexpected end of file.", nextStateLocation);
		}
		
		State GetDirectiveValue ()
		{
			int start = position;
			int delimiter = '\0';
			for (; position < content.Length; position++) {
				char c = content[position];
				nextStateLocation = nextStateLocation.AddCol ();
				if (c == '\r') {
					if (position + 1 < content.Length && content[position + 1] == '\n')
						position++;
					nextStateLocation = nextStateLocation.AddLine();
				} else if (c == '\n')
					nextStateLocation = nextStateLocation.AddLine();
				if (delimiter == '\0') {
					if (c == '\'' || c == '"') {
						start = position;
						delimiter = c;
					} else if (!Char.IsWhiteSpace (c)) {
						throw new ParserException ("Unexpected character '" + c + "'. Expecting attribute value.", nextStateLocation);
					}
					continue;
				}
				if (c == delimiter) {
					value = content.Substring (start + 1, position - start - 1);
					position++;
					return State.Directive;
				}
			}
			throw new ParserException ("Unexpected end of file.", nextStateLocation);;
		}
		
		State NextStateInContent ()
		{
			int start = position;
			for (; position < content.Length; position++) {
				char c = content[position];
				nextStateTagStartLocation = nextStateLocation;
				nextStateLocation = nextStateLocation.AddCol ();
				if (c == '\r') {
					if (position + 1 < content.Length && content[position + 1] == '\n')
						position++;
					nextStateLocation = nextStateLocation.AddLine();
				} else if (c == '\n') {
					nextStateLocation = nextStateLocation.AddLine();
				} else if (c =='<' && position + 2 < content.Length && content[position+1] == '#') {
					TagEndLocation = nextStateLocation;
					char type = content[position+2];
					if (type == '@') {
						nextStateLocation = nextStateLocation.AddCols (2);
						value = content.Substring (start, position - start);
						position += 3;
						return State.Directive;
					} else if (type == '=') {
						nextStateLocation = nextStateLocation.AddCols (2);
						value = content.Substring (start, position - start);
						position += 3;
						return State.Expression;
					} else if (type == '+') {
						nextStateLocation = nextStateLocation.AddCols (2);
						value = content.Substring (start, position - start);
						position += 3;
						return State.Helper;
					}  else {
						value = content.Substring (start, position - start);
						nextStateLocation = nextStateLocation.AddCol ();
						position += 2;
						return State.Block;
					}
				}
			}
			//EOF is only valid when we're in content
			value = content.Substring (start);
			return State.EOF;
		}

		int IsNewLine() {
			int found = 0;

			if (position < content.Length && content[position] == '\r') {
				found++;
			}
			if (position+found < content.Length && content[position+found] == '\n') {
				found++;
			}
			return found;
		}
		
		State NextStateInDirective () {
			for (; position < content.Length; position++) {
				char c = content[position];
				if (c == '\r') {
					if (position + 1 < content.Length && content[position + 1] == '\n')
						position++;
					nextStateLocation = nextStateLocation.AddLine();
				} else if (c == '\n') {
					nextStateLocation = nextStateLocation.AddLine();
				} else if (Char.IsLetter (c)) {
					return State.DirectiveName;
				} else if (c == '=') {
					nextStateLocation = nextStateLocation.AddCol ();
					position++;
					return State.DirectiveValue;	
				} else if (c == '#' && position + 1 < content.Length && content[position+1] == '>') {
					position+=2;
					TagEndLocation = nextStateLocation.AddCols (2);
					nextStateLocation = nextStateLocation.AddCols (3);
					
					//skip newlines directly after directives
					if ((position += IsNewLine()) > 0) {
						nextStateLocation = nextStateLocation.AddLine();
					}

					return State.Content;
				} else if (!Char.IsWhiteSpace (c)) {
					throw new ParserException ("Directive ended unexpectedly with character '" + c + "'", nextStateLocation);
				} else {
					nextStateLocation = nextStateLocation.AddCol ();
				}
			}
			throw new ParserException ("Unexpected end of file.", nextStateLocation);
		}
		
		public State State {
			get; private set;
		}
		
		public int Position {
			get { return position; }
		}
		
		public string Content {
			get { return content; }
		}
		
		public string Value {
			get { return value; }
		}
		
		public Location Location { get; private set; }
		public Location TagStartLocation { get; private set; }
		public Location TagEndLocation { get; private set; }
	}
	
	public enum State
	{
		Content = 0,
		Directive,
		Expression,
		Block,
		Helper,
		DirectiveName,
		DirectiveValue,
		Name,
		EOF
	}
	
	public class ParserException : Exception
	{
		public ParserException (string message, Location location) : base (message)
		{
			Location = location;
		}
		
		public Location Location { get; private set; }
	}
}
