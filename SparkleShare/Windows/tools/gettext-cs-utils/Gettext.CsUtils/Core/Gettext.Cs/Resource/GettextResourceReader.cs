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
using System.Resources;
using System.IO;

namespace Gettext.Cs
{
    public class GettextResourceReader : IResourceReader
    {
        Stream stream;

        public GettextResourceReader(Stream stream)
        {
            this.stream = stream;
        }
        
        #region IResourceReader Members

        public void Close()
        {
            if (stream != null)
            {
                this.stream.Close();
            }
        }

        public System.Collections.IDictionaryEnumerator GetEnumerator()
        {
            if (stream == null)
            {
                throw new ArgumentNullException("Input stream cannot be null");
            }
            
            using (var reader = new StreamReader(stream))
            {
                return new PoParser().ParseIntoDictionary(reader).GetEnumerator();
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (stream != null)
            {
                stream.Dispose();
            }
        }

        #endregion
    }
}
