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
 
 
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Resources;
using System.Collections;
using System.Text.RegularExpressions;

namespace Gettext.ResourcesReplacer
{
    public class Replacer
    {
        string[] csFiles;
        string[] resFiles;

        string funcname;

        ResourcesData resources;
        bool verbose;

        Regex regex;

        public Replacer(string filesPath, string resourcesPath, string funcname, bool topOnly, bool verbose)
        {
            var option = topOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
            
            var csDir = Path.GetDirectoryName(filesPath);
            if (String.IsNullOrEmpty(csDir)) csDir = Directory.GetCurrentDirectory();

            var resDir = Path.GetDirectoryName(resourcesPath);
            if (String.IsNullOrEmpty(resDir)) resDir = Directory.GetCurrentDirectory();

            this.csFiles = Directory.GetFiles(csDir, Path.GetFileName(filesPath), option);
            this.resFiles = Directory.GetFiles(resDir, Path.GetFileName(resourcesPath), option);
            this.funcname = funcname;
            this.verbose = verbose;
        }

        public void Run()
        {
            ProcessResources();
            CreateRegex();
            ProcessSourceFiles();
        }

        private void CreateRegex()
        {
            string pattern = @"(?<!\w) (?<ResName>{0}) \s* \. \s* (?<Entry>\w+)";

            var names = resources.GetResourceNames();
            string resnames = names.Aggregate(new StringBuilder(names.First()),
                (sb, n) => sb.AppendFormat("|{0}", n),
                (sb) => sb.ToString());

            regex = new Regex(String.Format(pattern, resnames), RegexOptions.IgnorePatternWhitespace);
        }

        private void ProcessSourceFiles()
        {
            if (verbose) Console.WriteLine("Processing source files");

            foreach (var csFile in csFiles)
            {
                if (verbose) Console.WriteLine(" Processing file {0}", csFile);

                bool changed = false;
                string text, newtext;

                using (var reader = new StreamReader(csFile))
                {
                    text = reader.ReadToEnd();
                    newtext = regex.Replace(text, match =>
                        {
                            var res = match.Groups["ResName"].Value;
                            var entry = match.Groups["Entry"].Value;

                            var dict = resources.GetResource(res);
                            if (dict == null)
                            {
                                if (verbose) Console.Out.WriteLine("  {0}: found not-understood resource in call {1}", csFile, match.Value);
                                return match.Value;
                            }

                            string value = null;
                            if (!dict.Data.TryGetValue(entry, out value) || value == null)
                            {
                                if (verbose) Console.Out.WriteLine("  {0}: found not-understood entry in call {1}", csFile, match.Value);
                                return match.Value;
                            }

                            changed = true;
                            return String.Format(@"{1}(@""{0}"")", Escape(value), funcname);
                        });
                }

                if (changed)
                {
                    using (var writer = new StreamWriter(csFile, false))
                    {
                        writer.Write(newtext);
                    }
                }
            }
        }

        private string Escape(string value)
        {
            return value.Replace("\"", "\"\"");
        }

        private void ProcessResources()
        {
            resources = new ResourcesData();

            if (verbose) Console.WriteLine("Processing resource files");

            foreach (var resFile in resFiles)
            {
                if (verbose) Console.WriteLine(" Processing resource file {0}", resFile);

                var resource = new ResourceData(Path.GetFileNameWithoutExtension(resFile));
                using (var reader = new ResourceReader(resFile))
                {
                    IDictionaryEnumerator en = reader.GetEnumerator();
                    while (en.MoveNext())
                    {
                        resource.Data.Add(en.Key.ToString(), en.Value.ToString());
                    }
                }

                resources.AddResource(resource);
            }

            if (resources.GetResourceNames().Length == 0)
                throw new ApplicationException("No resources found");
        }
    }

    class ResourcesData
    {
        private Dictionary<string, ResourceData> resources;

        public ResourcesData()
        {
            this.resources = new Dictionary<string,ResourceData>();
        }

        public void AddResource(ResourceData data)
        {
            foreach (var name in data.Names)
            {
                resources.Add(name, data);
            }
        }

        public ResourceData GetResource(string name)
        {
            if (resources.ContainsKey(name)) return resources[name];
            return null;
        }

        public string[] GetResourceNames()
        {
            return resources.Keys.ToArray();
        }
    }

    class ResourceData
    {
        public string[] Names { get; private set; }
        public Dictionary<String, String> Data { get; private set; }

        public ResourceData(string fullyQualifiedName)
        {
            List<String> names = new List<string>();
            String[] pieces = fullyQualifiedName.Split('.');

            for (int i = pieces.Length - 1; i >= 0; i--)
            {
                StringBuilder sb = new StringBuilder();
                for (int j = i; j < pieces.Length; j++)
                {
                    var piece = pieces[j];
                    if (sb.Length == 0) sb.Append(piece);
                    else sb.AppendFormat(".{0}", piece);
                }
                names.Add(sb.ToString());
            }

            this.Names = names.ToArray();
            this.Data = new Dictionary<string, string>();
        }
    }
}
