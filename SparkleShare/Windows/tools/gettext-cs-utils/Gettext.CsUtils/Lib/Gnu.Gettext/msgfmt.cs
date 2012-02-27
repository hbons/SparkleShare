/**
 * gettext-cs-utils
 *
 * Copyright 2011 Manas Technology Solutions 
 * http://www.manas.com.ar/
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either 
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public 
 * License along with this library.  If not, see <http://www.gnu.org/licenses/>.
 * 
 **/
 
 
/* GNU gettext for C#
 * Copyright (C) 2003 Free Software Foundation, Inc.
 * Written by Bruno Haible <bruno@clisp.org>, 2003.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 */

/*
 * This program creates a .resources file from a set of key/value pairs given
 * on standard input.
 */

using System; /* String, Console, Exception */
using System.IO; /* Stream, BufferedStream, StreamReader */
using System.Text; /* StringBuilder, UTF8Encoding */
using System.Resources; /* ResourceWriter */

namespace GNU.Gettext {
  public class WriteResource {
    private StreamReader reader;
    // Read a NUL-terminated UTF-8 encoded string.
    private String ReadString () {
      StringBuilder b = new StringBuilder();
      for (;;) {
        int c = reader.Read();
        if (c < 0) // EOF?
          return null;
        if (c == 0) // End of String?
          break;
        b.Append((char)c);
      }
      return b.ToString();
    }
    // Read all msgid/msgstr pairs, register them in the ResourceWriter,
    // and write the binary contents to the output stream.
    private void ReadAllInput (ResourceWriter rw) {
      for (;;) {
        String msgid = ReadString();
        if (msgid == null)
          break;
        String msgstr = ReadString();
        if (msgstr == null)
          break;
        rw.AddResource(msgid, msgstr);
      }
      rw.Generate();
    }
    // Read all msgid/msgstr pairs (each string being NUL-terminated and
    // UTF-8 encoded) and write the .resources file to the given filename.
    WriteResource (String filename) {
      Stream input = new BufferedStream(Console.OpenStandardInput());
      reader = new StreamReader(input, new UTF8Encoding());
      if (filename.Equals("-")) {
        BufferedStream output = new BufferedStream(Console.OpenStandardOutput());
        // A temporary output stream is needed because ResourceWriter.Generate
        // expects to be able to seek in the Stream.
        MemoryStream tmpoutput = new MemoryStream();
        ResourceWriter rw = new ResourceWriter(tmpoutput);
        ReadAllInput(rw);
#if __CSCC__
        // Use the ResourceReader to check against pnet-0.6.0 ResourceWriter
        // bug.
        try {
          ResourceReader rr = new ResourceReader(new MemoryStream(tmpoutput.ToArray()));
          foreach (System.Collections.DictionaryEntry entry in rr);
        } catch (IOException e) {
          throw new Exception("class ResourceWriter is buggy", e);
        }
#endif
        tmpoutput.WriteTo(output);
        rw.Close();
        output.Close();
      } else {
#if __CSCC__
        MemoryStream tmpoutput = new MemoryStream();
        ResourceWriter rw = new ResourceWriter(tmpoutput);
        ReadAllInput(rw);
        // Use the ResourceReader to check against pnet-0.6.0 ResourceWriter
        // bug.
        try {
          ResourceReader rr = new ResourceReader(new MemoryStream(tmpoutput.ToArray()));
          foreach (System.Collections.DictionaryEntry entry in rr);
        } catch (IOException e) {
          throw new Exception("class ResourceWriter is buggy", e);
        }
        BufferedStream output = new BufferedStream(new FileStream(filename, FileMode.Create, FileAccess.Write));
        tmpoutput.WriteTo(output);
        rw.Close();
        output.Close();
#else
        ResourceWriter rw = new ResourceWriter(filename);
        ReadAllInput(rw);
        rw.Close();
#endif
      }
    }
    public static int Main (String[] args) {
      try {
        new WriteResource(args[0]);
      } catch (Exception e) {
        Console.Error.WriteLine(e);
        Console.Error.WriteLine(e.StackTrace);
        return 1;
      }
      return 0;
    }
  }
}
