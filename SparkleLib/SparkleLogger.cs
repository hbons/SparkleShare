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
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.


using System;
using System.IO;

namespace SparkleLib {
    
    public static class SparkleLogger {

        private static Object debug_lock = new Object ();


        public static void LogInfo (string type, string message)
        {
            string timestamp = DateTime.Now.ToString ("HH:mm:ss");
            string line      = timestamp + " | " + type + " | " + message;

            if (SparkleConfig.DebugMode)
                Console.WriteLine (line);

            lock (debug_lock)
                File.AppendAllText (SparkleConfig.DefaultConfig.LogFilePath, line + Environment.NewLine);
        }
    }
}
