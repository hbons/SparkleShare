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
        private static int log_size = 0;

        public static void LogInfo (string type, string message)
        {
            LogInfo (type, message, null);
        }


        public static void LogInfo (string type, string message, Exception exception)
        {
            string timestamp = DateTime.Now.ToString ("HH:mm:ss");
            string line;

            if (string.IsNullOrEmpty (type))
                line = timestamp + " | " + message;
            else
                line = timestamp + " | " + type + " | " + message;

            if (exception != null)
                line += ": " + exception.Message + " " + exception.StackTrace;

            if (SparkleConfig.DebugMode)
                Console.WriteLine (line);

            lock (debug_lock) {
                // Don't let the log get bigger than 1000 lines
                if (log_size >= 1000) {
                    File.WriteAllText (SparkleConfig.DefaultConfig.LogFilePath, line + Environment.NewLine);
                    log_size = 0;

                } else {
                    File.AppendAllText (SparkleConfig.DefaultConfig.LogFilePath, line + Environment.NewLine);
                    log_size++;
                }
            }
        }


        public static void WriteCrashReport (Exception e)
        {
            string home_path = Environment.GetFolderPath (Environment.SpecialFolder.Personal);

            if (SparkleBackend.Platform == PlatformID.Win32NT)
                home_path = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);

            string crash_report_file_path = new string [] { home_path, "SparkleShare", "crash_report.txt" }.Combine ();

            string n = Environment.NewLine;
            string crash_report = "Oops! SparkleShare has crashed... :(" + n + n +
                "If you want to help fix this crash, please report it at " + n +
                "https://github.com/hbons/SparkleShare/issues and include the lines below." + n + n +
                "Remove any sensitive information like file names, IP addresses, domain names, etc. if needed." + n + n +
                "------" +  n + n +
                "SparkleShare version: " + SparkleLib.SparkleBackend.Version + n +
                "Operating system:     " + SparkleLib.SparkleBackend.Platform + " (" + Environment.OSVersion + ")" + n;

            crash_report += e.GetType () + ": " + e.Message + n + e.StackTrace + n;

            if (e.InnerException != null)
                crash_report += n + e.InnerException.Message + n + e.InnerException.StackTrace + n;

            if (SparkleConfig.DefaultConfig != null && File.Exists (SparkleConfig.DefaultConfig.LogFilePath)) {
                string debug_log      = File.ReadAllText (SparkleConfig.DefaultConfig.LogFilePath);
                string [] debug_lines = debug_log.Split (Environment.NewLine.ToCharArray ()); 
                int line_count        = 50;
                    
                if (debug_lines.Length > line_count) {
                    crash_report += string.Join (Environment.NewLine, debug_lines,
                        (debug_lines.Length - line_count), line_count) + n;
                
                } else {
                    crash_report += debug_log + n;
                }
            }

            File.WriteAllText (crash_report_file_path, crash_report);
        }
    }
}
