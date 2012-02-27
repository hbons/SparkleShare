using System;
using System.Xml.Xsl;

namespace xslt {
    class Program {
        static void Main (string [] args)
        {
            if (args.Length < 3) {
                Console.WriteLine ("usage: xslt.exe <file.xsl> <input.xml> <output.xml>");
                return;
            }

            var xsl = new XslCompiledTransform ();
            xsl.Load (args [0]);
            xsl.Transform (args [1], args [2]);
        }
    }
}
