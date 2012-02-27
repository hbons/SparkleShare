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
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Configuration;
using System.Collections.Specialized;
using System.Security.Permissions;
using System.Collections;

namespace Gettext.Cs
{
    /// <summary>
    /// Gettext based resource manager that reads from po files.
    /// </summary>
    public class GettextResourceManager : FileBasedResourceManager
    {
        #region Defaults

        const string defaultFileFormat = "{{culture}}\\{{resource}}.po";
        const string defaultPath = "";

        #endregion

        #region Properties

        /// <summary>
        /// Returns the Gettext resource set type used.
        /// </summary>
        public override Type ResourceSetType
        {
            get { return typeof(GettextResourceSet); }
        }

        #endregion

        #region Constructos

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="name">Name of the resource</param>
        /// <param name="path">Path to retrieve the files from</param>
        /// <param name="fileformat">Format of the file name using {{resource}} and {{culture}} placeholders.</param>
        public GettextResourceManager(string name, string path, string fileformat)
            : base(name, path, fileformat)
        {
        }

        /// <summary>
        /// Creates a new instance using local path and "{{culture}}\{{resource}}.po" file format.
        /// </summary>
        /// <param name="name">Name of the resource</param>
        public GettextResourceManager(string name)
            : base(name, defaultPath, defaultFileFormat)
        {
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Loads the named configuration section and retrieves file format and path from "fileformat" and "path" settings.
        /// </summary>
        /// <param name="section">Name of the section to retrieve.</param>
        /// <returns>True if the configuration section was loaded.</returns>
        public bool LoadConfiguration(string section)
        {
            var config = ConfigurationManager.GetSection(section) as NameValueCollection;

            if (config == null) return false;
            
            this.FileFormat = config["fileformat"] ?? FileFormat;
            this.Path = config["path"] ?? Path;
            
            return true;
        }

        /// <summary>
        /// Creates a new instance retrieving path and fileformat from the specified configuration section.
        /// </summary>
        /// <param name="name">Name of the resource</param>
        /// <param name="section">Name of the configuration section</param>
        /// <returns>New instance of ResourceManager</returns>
        public static FileBasedResourceManager CreateFromConfiguration(string name, string section)
        {
            return CreateFromConfiguration(name, section, defaultFileFormat, defaultPath);
        }

        /// <summary>
        /// Creates a new instance retrieving path and fileformat from the specified configuration section.
        /// </summary>
        /// <param name="name">Name of the resource</param>
        /// <param name="section">Name of the configuration section with fileformat and path settings</param>
        /// <param name="fallbackFileFormat">File format to be used if configuration could not be retrieved</param>
        /// <param name="fallbackPath">Path to be used if configuration could not be retrieved</param>
        /// <returns>New instance of ResourceManager</returns>
        public static FileBasedResourceManager CreateFromConfiguration(string name, string section, string fallbackFileFormat, string fallbackPath)
        {
            var config = ConfigurationManager.GetSection(section) as NameValueCollection;

            string fileformat = null;
            string path = null;

            if (config == null)
            {
                fileformat = fallbackFileFormat;
                path = fallbackPath;
            }
            else
            {
                fileformat = config["fileformat"] ?? fallbackFileFormat;
                path = config["path"] ?? fallbackPath;
            }

            return new FileBasedResourceManager(name, path, fileformat);
        }

        #endregion

    }

    
}
