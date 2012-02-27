using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CefSharp;
using System.IO;

namespace SparkleShare {

    class FileSchemeHandler : ISchemeHandler {
        #region ISchemeHandler Members

        public bool ProcessRequest (IRequest request, ref string mimeType, ref Stream stream)
        {
            return false;
        }

        #endregion
    }

    public class FileSchemeHandlerFactory : ISchemeHandlerFactory {
        public ISchemeHandler Create ()
        {
            return new FileSchemeHandler ();
        }
    }
}
