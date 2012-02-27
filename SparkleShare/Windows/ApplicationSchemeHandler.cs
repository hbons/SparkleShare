using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CefSharp;
using System.IO;

namespace SparkleShare {

    class ApplicationSchemeHandler : ISchemeHandler {
        #region ISchemeHandler Members

        public bool ProcessRequest (IRequest request, ref string mimeType, ref Stream stream)
        {
            if (request.Url.EndsWith (".png")) {
                System.Drawing.Bitmap Image=null;

                if (request.Url.EndsWith ("avatar-default-32.png"))
                    Image = Icons.avatar_default_32;
                else if (request.Url.EndsWith ("document-added-12.png"))
                    Image = Icons.document_added_12;
                else if (request.Url.EndsWith ("document-edited-12.png"))
                    Image = Icons.document_edited_12;
                else if (request.Url.EndsWith ("document-deleted-12.png"))
                    Image = Icons.document_deleted_12;
                else if (request.Url.EndsWith ("document-moved-12.png"))
                    Image = Icons.document_moved_12;

                if (Image != null) {
                    stream = new MemoryStream ();
                    Image.Save (stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Seek (0, SeekOrigin.Begin);
                    mimeType = "image/png";
                    return true;
                }
            } else if (request.Url.EndsWith (".js")) {
                string Text = null;

                if (request.Url.EndsWith ("jquery.js"))
                    Text = Properties.Resources.jquery_js;

                if (Text != null) {
                    stream = new MemoryStream (Encoding.UTF8.GetPreamble ().Concat (Encoding.UTF8.GetBytes (Text)).ToArray ());
                    mimeType = "application/javascript";
                    return true;
                }
            }

            return false;
        }

        #endregion
    }

    public class ApplicationSchemeHandlerFactory : ISchemeHandlerFactory {
        public ISchemeHandler Create ()
        {
            return new ApplicationSchemeHandler ();
        }
    }

}
