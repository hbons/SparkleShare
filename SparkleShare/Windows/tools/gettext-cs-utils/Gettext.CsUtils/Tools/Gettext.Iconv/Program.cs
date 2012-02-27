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
using System.Reflection;
using Gnu.Getopt;
using System.IO;

namespace Gettext.Iconv
{
    class Program
    {
        static void Main(string[] args)
        {
            var getopt = new Getopt(Assembly.GetExecutingAssembly().GetName().Name, args, "p:e:c:s:f:") { Opterr = false };

            string source = null;
            string pattern = null;
            string extension = null;
            string charset = null;
            string fromCharset = null;

            int option;
            while (-1 != (option = getopt.getopt()))
            {
                switch (option)
                {
                    case 'e': extension = getopt.Optarg; break;
                    case 's': source = getopt.Optarg; break;
                    case 'c': charset = getopt.Optarg; break;
                    case 'p': pattern = getopt.Optarg; break;
                    case 'f': fromCharset = getopt.Optarg; break;

                    default: PrintUsage(); return;
                }
            }

            if (extension == null || source == null || charset == null)
            {
                PrintUsage(); 
                return;
            }

            try
            {
                foreach (var file in Directory.GetFiles(source, pattern, SearchOption.AllDirectories))
                {
                    var output = String.Format("{0}.{1}", file, extension);
                    using (var reader = new StreamReader(file, Encoding.GetEncoding(fromCharset)))
                    {
                        using (var writer = new StreamWriter(output, false, Encoding.GetEncoding(charset)))
                        {
                            writer.Write(reader.ReadToEnd());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failure processing files: {0}", ex.Message);
                return;
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("Gettext Cs Tools");
            Console.WriteLine("----------------");
            Console.WriteLine();
            Console.WriteLine("Converts a certain file to another encoding.");
            Console.WriteLine();
            Console.WriteLine("Usage: {0} -cCHARSET -pPATTERN -sSOURCE -eEXTENSION", Assembly.GetExecutingAssembly().GetName().Name);
            Console.WriteLine(" Pattern: pattern of the files to process (optional).");
            Console.WriteLine(" Source: source path of the files to process.");
            Console.WriteLine(" Extension: extension to use for output files.");
            Console.WriteLine(" Charset: output charset for processed files.");
        }
    }
}
