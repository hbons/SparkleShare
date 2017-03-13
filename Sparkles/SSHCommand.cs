using System.IO;

namespace Sparkles {
    public class SSHCommand : Command
    {
        public static string SSHPath = "";
        public static string SSHCommandPath {
            get {
                return Path.Combine(SSHPath, "ssh").Replace("\\", "/");
            }
        }

        public SSHCommand(string command, string args) : this (command, args, null) {
        }

        public SSHCommand(string command, string args, SSHAuthenticationInfo auth_info) : base (Path.Combine(SSHPath, command), args) {
            string GIT_SSH_COMMAND = SSHPath;

            if (auth_info != null)
                GIT_SSH_COMMAND = FormatGitSSHCommand(auth_info);

            SetEnvironmentVariable("GIT_SSH_COMMAND", GIT_SSH_COMMAND);
        }

        public static string FormatGitSSHCommand(SSHAuthenticationInfo auth_info)
        {
            return SSHCommandPath + " " +
                "-i " + auth_info.PrivateKeyFilePath.Replace("\\" , "/").Replace(" ", "\\ ") + " " +
                "-o UserKnownHostsFile=" + auth_info.KnownHostsFilePath.Replace("\\", "/").Replace(" ", "\\ ") + " " +
                "-o IdentitiesOnly=yes" + " " + // Don't fall back to other keys on the system
                "-o PasswordAuthentication=no" + " " + // Don't hang on possible password prompts
                "-F /dev/null"; // Ignore the system's SSH config file
        }
    }
}
