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
using System.Diagnostics;

namespace Gettext.ResourcesReplacer
{
    class Program
    {
        static void Main(string[] args)
        {
            var getopt = new Getopt(Assembly.GetExecutingAssembly().GetName().Name, args, "f:r:s:tv") { Opterr = false };

            string funcname = null;
            string resourcesPath = null;
            string filesPath = null;
            bool topOnly = false;
            bool verbose = false;

            int option;
            while (-1 != (option = getopt.getopt()))
            {
                switch (option)
                {
                    case 'f': funcname = getopt.Optarg; break;
                    case 'r': resourcesPath = getopt.Optarg; break;
                    case 's': filesPath = getopt.Optarg; break;
                    case 't': topOnly = true; break;
                    case 'v': verbose = true; break;

                    default: PrintUsage(); return;
                }
            }

            if (resourcesPath == null || filesPath == null)
            {
                PrintUsage();
                return;
            }

            var replacer = new Replacer(filesPath, resourcesPath, funcname, topOnly, verbose);
            replacer.Run();
        }

        private static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("Gettext Cs Tools");
            Console.WriteLine("----------------");
            Console.WriteLine();
            Console.WriteLine("Takes input from NET .resource files and uses it to replace the text in C# files.");
            Console.WriteLine();
            Console.WriteLine("Usage: {0} -rRESOURCES -sSOURCES -fFUNCTION [-t] [-v]", Assembly.GetExecutingAssembly().GetName().Name);
            Console.WriteLine(" Resources: path to resource files, their names should be unique and match with the class used to access them.");
            Console.WriteLine(" Sources: path to C# source files to be formatted.");
            Console.WriteLine(" Function: function to use for translation (like Strings.T).");
            Console.WriteLine(" Top directory only: search files only in the top directory, do not search subdirs (default false).");
            Console.WriteLine(" Verbose: print info on processing status on stdout (default false).");
        }
    }
}
