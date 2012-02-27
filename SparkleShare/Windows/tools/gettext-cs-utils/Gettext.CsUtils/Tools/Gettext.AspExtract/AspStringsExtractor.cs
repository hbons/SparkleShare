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

namespace Instedd.Gettext.AspExtract
{
    public class AspStringsExtractor
    {
        IAspStringsParser parser;
        
        string outputExtension;
        string keyword;
        string charset;
        string fromCharset;

        public bool Notify { get; set; }

        public AspStringsExtractor(string keyword, string outputExtension, string charset, string fromCharset, IAspStringsParser parser)
        {
            this.keyword = keyword;
            this.parser = parser;
            this.outputExtension = outputExtension;
            this.charset = charset;
            this.fromCharset = fromCharset;
        }

        private class ParserRequestor : IAspStringsParserRequestor, IDisposable
        {
            string path;
            string keyword;
            string outputCharset;

            TextWriter writer;

            public ParserRequestor(string outputPath, string keyword, string outputCharset)
            {
                this.keyword = keyword;
                this.path = outputPath;
                this.outputCharset = outputCharset;

                this.writer = new StreamWriter(path, false, outputCharset == null ? Encoding.Default : Encoding.GetEncoding(outputCharset));
            }

            #region IAspStringsParserRequestor Members

            public void Report(string text)
            {
                writer.WriteLine(@"{0}(@""{1}"");", keyword, Escape(text));
            }

            private string Escape(string text)
            {
                return text.Replace('\r', ' ').Replace("\"", "\"\""); 
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                if (this.writer != null)
                {
                    this.writer.Dispose();
                }
            }

            #endregion
        }

        public void Extract(string path)
        {
            ExtractFilesWithPattern(path, "*.aspx");
            ExtractFilesWithPattern(path, "*.ascx");
            ExtractFilesWithPattern(path, "*.Master");
        }

        private void ExtractFilesWithPattern(string path, string pattern)
        {
            foreach (var file in Directory.GetFiles(path, pattern, SearchOption.AllDirectories))
            {
                ExtractFromFile(file);
            }
        }

        private void ExtractFromFile(string file)
        {
            var output = String.Format("{0}.{1}", file, outputExtension);
            
            using (var requestor = new ParserRequestor(output, keyword, charset))
            {
                using (var reader = new StreamReader(file, fromCharset == null ? Encoding.Default : Encoding.GetEncoding(fromCharset)))
                {
                    parser.Parse(reader.ReadToEnd(), requestor);
                }
            }

            if (Notify)
            {
                Console.Out.WriteLine("Parsed file {0} and output written to {1}.", file, output);
            }
        }
    }
}
