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


using System.IO;

namespace Sparkles {

    public class SSHCommand : Command
    {
        public static string SSHPath = Path.GetDirectoryName(LocateCommand("ssh")).Replace("\\", "/");

        public static string SSHCommandPath {
            get {
                return LocateCommand("ssh").Replace ("\\", "/");
            }
        }
        public static string SSHKeyScanCommandPath
        {
            get
            {
                return LocateCommand("ssh-keyscan").Replace("\\", "/");
            }
        }
        public static string SSHKeyGenCommandPath
        {
            get
            {
                return LocateCommand("ssh-keygen").Replace("\\", "/");
            }
        }


        public SSHCommand (string command, string args) : this (command, args, null)
        {
        }


        public SSHCommand (string command, string args, SSHAuthenticationInfo auth_info) :
            base (command, args)
        {
        }
        public static string SSHVersion
        {
            get
            {
                var ssh_version = new Command(SSHCommandPath, "-V", false);
                string version = ssh_version.StartAndReadStandardError();   //the version is written to StandardError instead of StanderdOutput!
                return version.Replace("SSH ", "").Split(',')[0];
            }
        }
        public static string KeyscanVersion
        {
            get
            {
                var ssh_version = new Command(SSHKeyScanCommandPath, "",false);
                ssh_version.StartAndWaitForExit(); // call to check if exists
                return "found";
            }
        }
        public static string KeygenVersion
        {
            get
            {
                // since keygen has no version output try to create testkey, if keygen is not found Comand will exit
                string arguments =
                "-t rsa " + // Crypto type
                "-b 4096 " + // Key size
                "-P \"\" " + // No password
                "-C \"test\" " + // Key comment
                "-f \"" + System.IO.Path.Combine(Configuration.DefaultConfiguration.DirectoryPath, "tmp", "testkey") + "\"";
                var ssh_version = new Command(SSHKeyGenCommandPath, arguments,false);
                ssh_version.StartAndWaitForExit(); // call to check if exists
                if (File.Exists(System.IO.Path.Combine(Configuration.DefaultConfiguration.DirectoryPath, "tmp", "testkey")))
                {
                    File.Delete(System.IO.Path.Combine(Configuration.DefaultConfiguration.DirectoryPath, "tmp", "testkey"));
                }
                if (File.Exists(System.IO.Path.Combine(Configuration.DefaultConfiguration.DirectoryPath, "tmp", "testkey.pub")))
                {
                    File.Delete(System.IO.Path.Combine(Configuration.DefaultConfiguration.DirectoryPath, "tmp", "testkey.pub"));
                }
                return "found";
            }
        }

    }
}
