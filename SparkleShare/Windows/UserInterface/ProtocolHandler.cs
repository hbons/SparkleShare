//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
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


using Microsoft.Win32;
using System.IO;

namespace SparkleShare
{

    /// <summary>
    /// Maintain Protocol Handlers created by SparkleShare
    /// </summary>
    static class SparkleProtocolHandler
    {

        /// <summary>
        /// Add or Update protocol handler
        /// </summary>
        /// <param name="handleName">The name of the handler to add</param>
        /// <param name="handleValue">Default value of the protocol handler</param>
        /// <param name="handleCommand">The arguments passed to the Invite Opener</param>
        internal static void AddProtocolHandler(string handleName, string handleValue, string handleCommand)
        {
            var inviteOpener = Path.Combine(Directory.GetCurrentDirectory(), "SparkleShareInviteOpener");

            // test the handleName for third party protocols like GitHub
            // if one exist and their default value doesn't match our custom Protocol, do not update
            using (RegistryKey testKey = Registry.ClassesRoot.OpenSubKey(handleName))
            {
                if (testKey == null || handleValue.Equals(testKey.GetValue("")))
                {
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software").CreateSubKey("Classes").CreateSubKey(handleName))
                    {
                        key.SetValue("", handleValue);
                        key.SetValue("URL Protocol", "");
                        key.CreateSubKey("DefaultIcon").SetValue("", inviteOpener);
                        key.CreateSubKey("shell")
                            .CreateSubKey("open")
                            .CreateSubKey("command")
                            .SetValue("", inviteOpener + " " + handleCommand);
                    }
                }
            }
        }

        /// <summary>
        /// Remove protocol handler
        /// </summary>
        /// <param name="handleName">The name of the handler to remove</param>
        /// <param name="handleValue">Default value of the protocol handler</param>
        internal static void RemoveProtocolHandler(string handleName, string handleValue)
        {
            var key = Registry.CurrentUser.OpenSubKey(handleName);

            // if the the default value doesn't match our custom Protocol, do not remove
            if (key != null && handleValue.Equals(key.GetValue("")))
                Registry.CurrentUser.DeleteSubKeyTree(handleName);
        }
    }
}
