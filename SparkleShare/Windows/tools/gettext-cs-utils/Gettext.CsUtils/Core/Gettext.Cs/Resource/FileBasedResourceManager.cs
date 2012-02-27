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
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Collections;

namespace Gettext.Cs
{
    /// <summary>
    /// Extendable file based resource manager.
    /// </summary>
    public class FileBasedResourceManager : System.Resources.ResourceManager
    {
        #region Properties

        string path;
        string fileformat;

        /// <summary>
        /// Path to retrieve the files from.
        /// </summary>
        public string Path
        {
            get { return path; }
            set { path = value; }
        }

        /// <summary>
        /// Format of the resource set po file based on {{culture}} and {{resource}} placeholders.
        /// </summary>
        public string FileFormat
        {
            get { return fileformat; }
            set { fileformat = value; }
        }

        #endregion

        #region Notification Events

        /// <summary>
        /// Arguments for events related to the creation, successful or not, of a resource set.
        /// </summary>
        public class ResourceSetCreationEventArgs : EventArgs
        {
            /// <summary>
            /// Exception in case of error, null on success.
            /// </summary>
            public Exception Exception { get; set; }

            /// <summary>
            /// FileName from where the resource set was loaded.
            /// </summary>
            public String FileName { get; set; }

            /// <summary>
            /// Type of the resource set being initialized.
            /// </summary>
            public Type ResourceSetType { get; set; }

            /// <summary>
            /// Instance of the resource set created, may be null on error.
            /// </summary>
            public System.Resources.ResourceSet ResourceSet { get; set; }

            /// <summary>
            /// Whether the creation was successful.
            /// </summary>
            public bool Success { get; set; }
        }

        /// <summary>
        /// Event that notifies the successful creation of a resource set.
        /// </summary>
        public event EventHandler<ResourceSetCreationEventArgs> CreatedResourceSet;

        /// <summary>
        /// Event that notifies an error creating a resource set.
        /// </summary>
        public event EventHandler<ResourceSetCreationEventArgs> FailedResourceSet;

        protected void RaiseCreatedResourceSet(string filename, System.Resources.ResourceSet set)
        {
            var handler = CreatedResourceSet;
            if (handler != null)
            {
                handler(this, new ResourceSetCreationEventArgs 
                { 
                    FileName = filename, 
                    ResourceSet = set, 
                    ResourceSetType = this.ResourceSetType, 
                    Success = true 
                });
            }
        }

        protected void RaiseFailedResourceSet(string filename, Exception ex)
        {
            var handler = FailedResourceSet;
            if (handler != null)
            {
                handler(this, new ResourceSetCreationEventArgs 
                { 
                    FileName = filename, 
                    ResourceSet = null, 
                    ResourceSetType = this.ResourceSetType, 
                    Success = false,
                    Exception = ex
                });
            }
        }

        #endregion

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="name">Name of the resource</param>
        /// <param name="path">Path to retrieve the files from</param>
        /// <param name="fileformat">Format of the file name using {{resource}} and {{culture}} placeholders.</param>
        public FileBasedResourceManager(string name, string path, string fileformat)
            : base()
        {
            this.path = path;
            this.fileformat = fileformat;
            this.BaseNameField = name;

            base.IgnoreCase = false;
            base.ResourceSets = new System.Collections.Hashtable();
        }

        protected override string GetResourceFileName(System.Globalization.CultureInfo culture)
        {
            return fileformat.Replace("{{culture}}", culture.Name).Replace("{{resource}}", BaseNameField);
        }

        protected override System.Resources.ResourceSet InternalGetResourceSet(System.Globalization.CultureInfo culture, bool createIfNotExists, bool tryParents)
        {
            if (path == null && fileformat == null) return null;
            if (culture == null || culture.Equals(CultureInfo.InvariantCulture)) return null;

            System.Resources.ResourceSet rs = null;
            Hashtable resourceSets = this.ResourceSets;

            if (!TryFetchResourceSet(resourceSets, culture, out rs))
            {
                string resourceFileName = this.FindResourceFile(culture);
                if (resourceFileName == null)
                {
                    if (tryParents)
                    {
                        CultureInfo parent = culture.Parent;
                        rs = this.InternalGetResourceSet(parent, createIfNotExists, tryParents);
                        AddResourceSet(resourceSets, culture, ref rs);
                        return rs;
                    }
                }
                else
                {
                    rs = this.CreateResourceSet(resourceFileName);
                    AddResourceSet(resourceSets, culture, ref rs);
                    return rs;
                }
            }

            return rs;
        }

        protected virtual System.Resources.ResourceSet InternalCreateResourceSet(string resourceFileName)
        {
            object[] args = new object[] { resourceFileName };
            return (System.Resources.ResourceSet)Activator.CreateInstance(this.ResourceSetType, args);
        }

        private System.Resources.ResourceSet CreateResourceSet(string resourceFileName)
        {
            System.Resources.ResourceSet set = null;

            try
            {
                set = InternalCreateResourceSet(resourceFileName);
                RaiseCreatedResourceSet(resourceFileName, set);
            }
            catch (Exception ex)
            {
                RaiseFailedResourceSet(resourceFileName, ex);
            }

            return set;
        }

        private string FindResourceFile(CultureInfo culture)
        {
            string resourceFileName = this.GetResourceFileName(culture);
            string path = this.path ?? String.Empty;

            // Try with simple path + filename combination
            string fullpath = System.IO.Path.Combine(path, resourceFileName);
            if (File.Exists(fullpath)) return fullpath;

            // If path is relative, attempt different directories
            if (path == String.Empty || !System.IO.Path.IsPathRooted(path))
            {
                // Try the entry assembly dir
                if (Assembly.GetEntryAssembly() != null)
                {
                    string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), path);
                    fullpath = System.IO.Path.Combine(dir, resourceFileName);
                    if (File.Exists(fullpath)) return fullpath;
                }

                // Else try the executing assembly dir
                if (Assembly.GetExecutingAssembly() != null)
                {
                    if (Assembly.GetEntryAssembly() == null || System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) != System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                    {
                        string dir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);
                        fullpath = System.IO.Path.Combine(dir, resourceFileName);
                        if (File.Exists(fullpath)) return fullpath;
                    }
                }
            }

            return null;
        }

        private void AddResourceSet(Hashtable localResourceSets, CultureInfo culture, ref System.Resources.ResourceSet rs)
        {
            lock (localResourceSets)
            {
                if (localResourceSets.Contains(culture))
                {
                    var existing = (System.Resources.ResourceSet)localResourceSets[culture];

                    if (existing != null && !object.Equals(existing, rs))
                    {
                        rs.Dispose();
                        rs = existing;
                        var a = (System.Collections.Specialized.NameValueCollection)System.Configuration.ConfigurationManager.GetSection("appSettings");
                    }
                }
                else
                {
                    localResourceSets.Add(culture, rs);
                }
            }
        }

        private bool TryFetchResourceSet(Hashtable localResourceSets, CultureInfo culture, out System.Resources.ResourceSet set)
        {
            lock (localResourceSets)
            {
                if (ResourceSets.Contains(culture))
                {
                    set = (System.Resources.ResourceSet)ResourceSets[culture];
                    return true;
                }

                set = null;
                return false;
            }
        }

        private bool ValidateGetResourceSet(CultureInfo culture)
        {
            return !(culture == null || culture.Equals(CultureInfo.InvariantCulture) || String.IsNullOrEmpty(culture.Name));
        }

    }
}
