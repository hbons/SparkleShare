using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace SparkleShare
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [ComVisible(true)]
    public class SparkleScriptingObject
    {
        public void LinkClicked(string url)
        {
            Program.UI.EventLog.Controller.LinkClicked(url);
        }
    }
}
