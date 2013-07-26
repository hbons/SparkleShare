using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparkleShare
{
    using System.Runtime.InteropServices;
    using System.Security.Permissions;

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
