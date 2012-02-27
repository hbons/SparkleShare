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
using Gettext.Cs;

namespace Gettext.DatabaseResourceGenerator
{
    class DatabaseParserRequestor : IGettextParserRequestor
    {
        bool insertAll;
        string culture;
        DatabaseInterface db;

        public DatabaseParserRequestor(string culture, DatabaseInterface db, bool insertAll)
        {
            this.culture = culture;
            this.db = db;
            this.insertAll = insertAll;
        }

        #region IGettextParserRequestor Members

        public void Handle(string key, string value)
        {
            if (insertAll || !String.IsNullOrEmpty(value))
            {
                this.db.InsertResource(culture, key, value);
            }
        }

        #endregion
    }
}
