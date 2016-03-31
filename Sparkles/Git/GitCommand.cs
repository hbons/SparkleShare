//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
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


namespace Sparkles.Git {

    public class GitCommand : Command {

        public static string SSHPath;
        public static string GitPath;
        public static string ExecPath;


        public static string GitVersion {
            get {
                if (GitPath == null)
                    GitPath = LocateCommand ("git");

                string git_version = new Command (GitPath, "--version").StartAndReadStandardOutput ();
                return git_version.Replace ("git version ", "");
            }
        }


        public GitCommand (string path, string args) : base (path, args)
        {
            if (GitPath == null)
                GitPath = LocateCommand ("git");

            StartInfo.FileName = GitPath;
            StartInfo.WorkingDirectory = path;

            if (string.IsNullOrEmpty (SSHPath))
                SSHPath = "ssh";

            string GIT_SSH_COMMAND = SSHPath + " " +
                "-i \"" + SSHAuthenticationInfo.DefaultAuthenticationInfo.PrivateKeyFilePath + "\" " +
                "-o UserKnownHostsFile=\"" + SSHAuthenticationInfo.DefaultAuthenticationInfo.KnownHostsFilePath + "\" " +
                "-o PasswordAuthentication=no " +
                "-F /dev/null"; // Ignore the environment's SSH config file

            if (ExecPath == null)
                SetEnvironmentVariable ("GIT_EXEC_PATH", ExecPath);

            SetEnvironmentVariable ("GIT_SSH_COMMAND", GIT_SSH_COMMAND);
            SetEnvironmentVariable ("GIT_TERMINAL_PROMPT", "0");
			SetEnvironmentVariable ("LANG", "en_US");
        }


        void SetEnvironmentVariable (string variable, string content)
        {
            if (StartInfo.EnvironmentVariables.ContainsKey (variable))
                StartInfo.EnvironmentVariables [variable] = content;
            else
                StartInfo.EnvironmentVariables.Add (variable, content);
        }
    }
}
