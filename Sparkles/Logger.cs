//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU Lesser General Public License as 
//   published by the Free Software Foundation, either version 3 of the 
//   License, or (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.IO;

namespace Sparkles {
    
    public static class Logger {

        static StreamWriter log_writer = File.CreateText (Configuration.DefaultConfiguration.LogFilePath);
        static object log_writer_lock = new object ();


        public static void LogInfo (string type, string message)
        {
            LogInfo (type, message, null);
        }


        public static void LogInfo (string type, string message, Exception exception)
        {
            string timestamp = DateTime.Now.ToString ("HH:mm:ss");
            string line;

            if (string.IsNullOrEmpty (type))
                line = timestamp + " " + message;
            else
                line = timestamp + " " + type + " | " + message;

            if (exception != null)
                line += ": " + exception.Message + " " + exception.StackTrace;

            if (Configuration.DebugMode)
                Console.WriteLine (line);

            lock (log_writer_lock) {
                try {
                    log_writer.WriteLine (line);
                    log_writer.Flush ();

                } catch (Exception e) {
                    Console.WriteLine (string.Format ("Could not write to log {0}: {1} {2}",
                        (log_writer.BaseStream as FileStream).Name, e.Message, e.StackTrace));
                }
            }
        }


        public static void WriteCrashReport (Exception e)
        {
            if (log_writer != null)
                log_writer.Close ();

            string home_path = Environment.GetFolderPath (Environment.SpecialFolder.Personal);

            if (InstallationInfo.OperatingSystem == OS.Windows)
                home_path = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);

            string crash_report_file_path = Path.Combine (home_path, "SparkleShare", "crash_report.txt");

            string n = Environment.NewLine;
            string crash_report =
                "Oops! SparkleShare has crashed... :(" + n +
                n +
                "If you want to help fix this crash, please report it at " + n +
                "https://github.com/hbons/SparkleShare/issues and include the lines below." + n +
                n +
                "Remove any sensitive information like file names, IP addresses, domain names, etc. if needed." + n +
                n +
                "------" + n +
                n;

            crash_report += e.GetType () + ": " + e.Message + n + e.StackTrace + n + n;

            if (e.InnerException != null)
                crash_report += n + e.InnerException.Message + n + e.InnerException.StackTrace + n;

            if (Configuration.DefaultConfiguration != null && File.Exists (Configuration.DefaultConfiguration.LogFilePath)) {
                string debug_log      = File.ReadAllText (Configuration.DefaultConfiguration.LogFilePath);
                string [] debug_lines = debug_log.Split (Environment.NewLine.ToCharArray ()); 
                int line_count        = 50;
                    
                if (debug_lines.Length > line_count)
                    crash_report += string.Join (n, debug_lines, (debug_lines.Length - line_count), line_count) + n;
                else
                    crash_report += debug_log + n;
            }

            File.WriteAllText (crash_report_file_path, crash_report);
            Console.WriteLine (DateTime.Now.ToString ("HH:mm:ss") + " | Wrote crash report to " + crash_report_file_path);
        }
    }
}
