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
using System.Collections;
using System.Globalization;
using System.Resources;
using System.Configuration;

namespace Gettext.Cs
{
    public class DatabaseResourceManager : System.Resources.ResourceManager
    {
        private string dsn;
        private string sp;

        public DatabaseResourceManager()
            : base()
        {
            this.dsn = ConfigurationManager.AppSettings["Gettext.ConnectionString"] ?? ConfigurationManager.ConnectionStrings["Gettext"].ConnectionString;
            ResourceSets = new System.Collections.Hashtable();
        }

        public DatabaseResourceManager(string storedProcedure)
            : this()
        {
            this.sp = storedProcedure;
        }

        // Hack: kept for compatibility
        public DatabaseResourceManager(string name, string path, string fileformat)
            : this()
        {
        }

        protected override ResourceSet InternalGetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
        {
            DatabaseResourceSet rs = null;
 
            if (ResourceSets.Contains(culture.Name))
            {
                rs = ResourceSets[culture.Name] as DatabaseResourceSet;
            }
            else
            {
                lock (ResourceSets)
                {
                    // Check hash table once again after lock is set
                    if (ResourceSets.Contains(culture.Name))
                    {
                        rs = ResourceSets[culture.Name] as DatabaseResourceSet;
                    }
                    else
                    {
                        rs = new DatabaseResourceSet(dsn, culture, sp);
                        ResourceSets.Add(culture.Name, rs);
                    }
                }
            }
            
            return rs; 
        }
    }
}
