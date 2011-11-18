//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

using SparkleLib;

namespace SparkleShare {

    public abstract partial class SparkleControllerBase {

        // Short alias for the translations
        public static string _ (string s)
        {
            return s;
        }

        public static string GetPluralString (string singular, string plural, int number)
        {
            if (number>1)
                return plural;
            return singular;
        }

        public void Exit (int exitCode)
        {
            Environment.ExitCode = exitCode;
            System.Windows.Forms.Application.Exit ();
        }

    }
}

