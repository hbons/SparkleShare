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
using System;


namespace Sparkles.Git {

    public class GitCommand : Command {

        public static string SSHPath;
        public static string ExecPath;


        static string git_path;

        public static string GitPath {
            get {
                if (git_path == null)
                    git_path = LocateCommand ("git");

                return git_path;
            }

            set {
                git_path = value;
            }
        }


        public static string GitVersion {
            get {
                if (GitPath == null)
                    GitPath = LocateCommand ("git");

                string git_version = new Command (GitPath, "--version").StartAndReadStandardOutput ();
                return git_version.Replace ("git version ", "");
            }
        }


        public static string GitLFSVersion {
            get {
                if (GitPath == null)
                    GitPath = LocateCommand ("git");

                string git_lfs_version = new Command (GitPath, "lfs version").StartAndReadStandardOutput ();
                return git_lfs_version.Replace ("git-lfs/", "").Split (' ') [0];
            }
        }


        public GitCommand (string working_dir, string args) : this (working_dir, args, null)
        {
        }


        public GitCommand (string working_dir, string args, SSHAuthenticationInfo auth_info) : base (GitPath, args)
        {
            StartInfo.WorkingDirectory = working_dir;

            if (string.IsNullOrEmpty (SSHPath))
                SSHPath = "ssh";

            string GIT_SSH_COMMAND = SSHPath;

            if (auth_info != null)
                GIT_SSH_COMMAND = FormatGitSSHCommand (auth_info);

            if (ExecPath == null)
                SetEnvironmentVariable ("GIT_EXEC_PATH", ExecPath);

			SetEnvironmentVariable ("GIT_SSH_COMMAND", GIT_SSH_COMMAND);
            SetEnvironmentVariable ("GIT_TERMINAL_PROMPT", "0");
			SetEnvironmentVariable ("LANG", "en_US");
        }


        public static string FormatGitSSHCommand (SSHAuthenticationInfo auth_info)
        {
            return SSHPath + " " +
                "-i " + auth_info.PrivateKeyFilePath.Replace (" ", "\\ ") + " " +
                "-o UserKnownHostsFile=" + auth_info.KnownHostsFilePath.Replace (" ", "\\ ") + " " +
                "-o IdentitiesOnly=yes" + " " + // Don't fall back to other keys on the system
                "-o PasswordAuthentication=no" + " " + // Don't hang on possible password prompts
                "-F /dev/null"; // Ignore the system's SSH config file
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
