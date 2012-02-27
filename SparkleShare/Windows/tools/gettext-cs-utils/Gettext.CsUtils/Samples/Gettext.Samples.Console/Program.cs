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
using System.Globalization;

namespace Gettext.Samples.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            if (SetCultureFromArgs(args))
            {
                string translated = Strings.T("Hello world");
                System.Console.Out.WriteLine(translated);
            }
        }

        private static bool SetCultureFromArgs(string[] args)
        {
            if (args.Length == 0 || args[0].Contains("?"))
            {
                PrintUsage();
                return false;
            }
            else
            {
                try
                {
                    var culture = CultureInfo.GetCultureInfo(args[0]);
                    System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
                    return true;
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine("Error setting culture from argument {0}:\n{1}", args[0], ex.Message);
                    return false;
                }
            }
        }

        private static void PrintUsage()
        {
            System.Console.Out.WriteLine("Use culture short code as only parameter to view message in that language. Choose from one of the available cultures; localized cultures will fall back to neutral ones, and not available ones will fall back to english.");
            System.Console.Out.WriteLine();
            System.Console.Out.WriteLine("Available cultures:");
            System.Console.Out.WriteLine(" en");
            System.Console.Out.WriteLine(" es");
            System.Console.Out.WriteLine(" pt");
            System.Console.Out.WriteLine(" fr");
        }
    }
}
