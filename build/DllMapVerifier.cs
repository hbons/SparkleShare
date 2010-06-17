using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections.Generic;

public static class DllMapVerifier
{
    private struct DllImportRef
    {
        public DllImportRef (string name, int line, int column)
        {
            Name = name;
            Line = line;
            Column = column;
        }

        public string Name;
        public int Line;
        public int Column;
    }

    private static Dictionary<string, List<DllImportRef>> dll_imports 
        = new Dictionary<string, List<DllImportRef>> ();
    private static List<string> ignore_dlls = new List<string> ();
    private static List<string> config_dlls = null;
    
    public static int Main (string [] args)
    {
        LoadConfigDlls (args[0]);
        foreach (string file in args) {
            LoadDllImports (file);
        }

        return VerifyDllImports (args[0]) ? 0 : 1;
    }

    private static bool VerifyDllImports (string configFile)
    {
        int total_unmapped_count = 0;

        foreach (KeyValuePair<string, List<DllImportRef>> dll_import in dll_imports) {
            int file_unmapped_count = 0;
            foreach (DllImportRef dll_import_ref in dll_import.Value) {
                if (config_dlls != null && config_dlls.Contains (dll_import_ref.Name)) {
                    continue;
                }

                if (file_unmapped_count++ == 0) {
                    Console.Error.WriteLine ("Unmapped DLLs in file: {0}", dll_import.Key);
                }

                Console.Error.WriteLine ("  + {0} : {1},{2}", dll_import_ref.Name, 
                    dll_import_ref.Line, dll_import_ref.Column);
            }

            total_unmapped_count += file_unmapped_count;
        }

        if (total_unmapped_count > 0) {
            Console.Error.WriteLine ();
            Console.Error.WriteLine ("  If any DllImport above is explicitly allowed to be unmapped,");
            Console.Error.WriteLine ("  add an 'willfully unmapped' comment to the inside of the attribute:");
            Console.Error.WriteLine ();
            Console.Error.WriteLine ("      [DllImport (\"libX11.so.6\") /* willfully unmapped */]");
            Console.Error.WriteLine ();
        }

        if (total_unmapped_count > 0 && config_dlls == null) {
            Console.Error.WriteLine ("No config file for DLL mapping was found ({0})", configFile);
        }

        return total_unmapped_count == 0;
    }
    
    private static void LoadDllImports (string csFile)
    {
        if (csFile.StartsWith ("-i")) {
            ignore_dlls.Add (csFile.Substring (2));
            return;
        }

        if (Path.GetExtension (csFile) == ".cs" && File.Exists (csFile)) {
            List<DllImportRef> dll_import_refs = null;

            foreach (DllImportRef dll_import in ParseFileForDllImports (csFile)) {
                if (ignore_dlls.Contains (dll_import.Name)) {
                    continue;
                }
            
                if (dll_import_refs == null) {
                    dll_import_refs = new List<DllImportRef> ();
                }

                dll_import_refs.Add (dll_import);
            }

            if (dll_import_refs != null) {
                dll_imports.Add (csFile, dll_import_refs);
            }
        }
    }

    private static void LoadConfigDlls (string configFile)
    {
        try {
            XmlTextReader config = new XmlTextReader (configFile);
            config_dlls = new List<string> ();
            while (config.Read ()) {
                if (config.NodeType == XmlNodeType.Element && 
                    config.Name == "dllmap") {
                    string dll = config.GetAttribute ("dll");
                    if (!config_dlls.Contains (dll)) {
                        config_dlls.Add (dll);
                    }
                }
            }
        } catch {
        }
    }
    
#region DllImport parser

    private static StreamReader reader;
    private static int reader_line;
    private static int reader_col;
    
    private static IEnumerable<DllImportRef> ParseFileForDllImports (string file)
    {
        reader_line = 1;
        reader_col = 1;

        using (reader = new StreamReader (file)) {
            char c;
            bool in_paren = false;
            bool in_attr = false;
            bool in_dll_attr = false;
            bool in_string = false;
            bool in_comment = false;
            int dll_line = 1, dll_col = 1;
            string dll_string = null;
            string dll_comment = null;
            
            while ((c = (char)reader.Peek ()) != Char.MaxValue) {
                switch (c) {
                    case ' ':
                    case '\t': Read (); break;
                    case '[': 
                        in_attr = true;
                        dll_string = null;
                        dll_comment = null;
                        dll_line = reader_line;
                        dll_col = reader_col;
                        Read ();
                        break;
                    case '(': Read (); in_paren = true; break;
                    case ')': Read (); in_paren = false; break;
                    case '"':
                        Read ();
                        if (dll_string == null && in_dll_attr && in_paren && !in_string) {
                            in_string = true;
                        }
                        break;
                    case '/':
                        Read ();
                        if ((char)reader.Peek () == '*') {
                            Read ();
                            if (in_dll_attr && !in_comment) {
                                in_comment = true;
                            }
                        }
                        break;
                    case ']':
                        if (in_dll_attr && dll_string != null && dll_comment != "willfully unmapped") {
                            yield return new DllImportRef (dll_string, dll_line, dll_col);
                        }
                        in_attr = false;
                        in_dll_attr = false;
                        Read ();
                        break;
                    default:
                        if (!in_dll_attr && in_attr && ReadDllAttribute ()) {
                            in_dll_attr = true;
                        } else if (in_dll_attr && in_string) {
                            dll_string = ReadDllString ();
                            in_string = false;
                        } else if (in_dll_attr && in_comment) {
                            dll_comment = ReadDllComment ();
                            in_comment = false;
                        } else {
                            Read ();
                        }
                        break;
                }
            }
        }
    }

    private static bool ReadDllAttribute () 
    {
        return
            Read () == 'D' && 
            Read () == 'l' &&
            Read () == 'l' &&
            Read () == 'I' &&
            Read () == 'm' &&
            Read () == 'p' &&
            Read () == 'o' &&
            Read () == 'r' &&
            Read () == 't';
    }

    private static string ReadDllString ()
    {
        StringBuilder builder = new StringBuilder (32);
        while (true) {
            char c = Read ();
            if (Char.IsLetterOrDigit (c) || c == '.' || c == '-' || c == '_') {
                builder.Append (c);
            } else {
                break;
            }
        }
        return builder.ToString ();
    }

    private static string ReadDllComment ()
    {
        StringBuilder builder = new StringBuilder ();
        char lc = Char.MaxValue;
        while (true) {
            char c = Read ();
            if (c == Char.MaxValue || (c == '/' && lc == '*')) {
                break;
            } else if (lc != Char.MaxValue) {
                builder.Append (lc);
            }
            lc = c;
        }
        return builder.ToString ().Trim ();
    }
    
    private static char Read ()
    {
        char c = (char)reader.Read ();
        if (c == '\n') {
            reader_line++;
            reader_col = 1;
        } else {
            reader_col++;
        }
        return c;
    }
    
#endregion

}

