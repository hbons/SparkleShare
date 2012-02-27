/**
 * gettext-cs-utils
 *
 * Copyright 2011 Manas Technology Solutions 
 * http://www.manas.com.ar/
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either 
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public 
 * License along with this library.  If not, see <http://www.gnu.org/licenses/>.
 * 
 **/
 
 
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Gettext.Cs
{
    /// <summary>
    /// Parses standard po files.
    /// </summary>
    public class PoParser
    {
        /// <summary>
        /// Parses an input po file.
        /// </summary>
        public void Parse(TextReader reader, IGettextParserRequestor requestor)
        {
            const int StateWaitingKey = 1;
            const int StateConsumingKey = 2;
            const int StateConsumingValue = 3;

            int state = StateWaitingKey;

            StringBuilder currentKey = null;
            StringBuilder currentValue = null;

            string line;
            while(true) {
                line = reader.ReadLine();
                line = line == null ? null : line.Trim();
                if (line == null || line.Length == 0)
                {
                    if (state == StateConsumingValue &&
                        currentKey != null &&
                        currentValue != null)
                    {
                        requestor.Handle(currentKey.ToString().Replace("\\n", "\n").Replace("\\\"", "\""),
                            currentValue.ToString().Replace("\\n", "\n").Replace("\\\"", "\""));
                        currentKey = null;
                        currentValue = null;
                    }

                    if (line == null)
                        break;

                    state = StateWaitingKey;
                    continue;
                }
                else if (line[0] == '#')
                {
                    continue;
                }

                bool isMsgId = line.StartsWith("msgid ");
                bool isMsgStr = !isMsgId && line.StartsWith("msgstr ");

                if (isMsgId || isMsgStr)
                {
                    state = isMsgId ? StateConsumingKey : StateConsumingValue;

                    int firstQuote = line.IndexOf('"');
                    if (firstQuote == -1)
                        continue;

                    int secondQuote = line.IndexOf('"', firstQuote + 1);
                    while (secondQuote != -1 && line[secondQuote - 1] == '\\')
                        secondQuote = line.IndexOf('"', secondQuote + 1);
                    if (secondQuote == -1)
                        continue;

                    string piece = line.Substring(firstQuote + 1, secondQuote - firstQuote - 1);

                    if (isMsgId)
                    {
                        currentKey = new StringBuilder();
                        currentKey.Append(piece);
                    }
                    else
                    {
                        currentValue = new StringBuilder();
                        currentValue.Append(piece);
                    }
                }
                else if (line[0] == '"')
                {
                    if (line[line.Length - 1] == '"')
                    {
                        line = line.Substring(1, line.Length - 2);
                    }
                    else
                    {
                        line = line.Substring(1, line.Length - 1);
                    }

                    switch (state)
                    {
                        case StateConsumingKey:
                            currentKey.Append(line);
                            break;
                        case StateConsumingValue:
                            currentValue.Append(line);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Parses an input po file.
        /// </summary>
        public void Parse(string text, IGettextParserRequestor requestor)
        {
            Parse(new StringReader(text), requestor);
        }

        /// <summary>
        /// Parses an input po file into a dictionary.
        /// </summary>
        public Dictionary<String, String> ParseIntoDictionary(TextReader reader)
        {
            var requestor = new DictionaryGettextParserRequestor();
            Parse(reader, requestor);
            return requestor;
        }
    }
}
