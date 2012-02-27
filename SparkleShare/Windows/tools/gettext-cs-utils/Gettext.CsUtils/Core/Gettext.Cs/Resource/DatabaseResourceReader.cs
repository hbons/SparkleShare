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
using System.Collections;
using System.Data.SqlClient;
using System.Globalization;
using System.Configuration;

namespace Gettext.Cs
{
    public class DatabaseResourceReader : IResourceReader
    {
        private string dsn;
        private string language;
        private string sp;

        public DatabaseResourceReader(string dsn, CultureInfo culture)
        {
            this.dsn = dsn;
            this.language = culture.Name;
        }

        public DatabaseResourceReader(string dsn, CultureInfo culture, string sp)
        {
            this.sp = sp;
            this.dsn = dsn;
            this.language = culture.Name;
        }

        public System.Collections.IDictionaryEnumerator GetEnumerator()
        {
            Hashtable dict = new Hashtable();

            SqlConnection connection = new SqlConnection(dsn);
            SqlCommand command = connection.CreateCommand();

            if (language == "")
                language = CultureInfo.InvariantCulture.Name;

            // Use stored procedure or plain text
            if (sp == null)
            {
                command.CommandText = string.Format("SELECT MessageKey, MessageValue FROM Message WHERE Culture = '{0}'", language);
            }
            else
            {
                command.CommandText = sp;
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@culture", language);
            }

            try
            {
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader.GetValue(1) != System.DBNull.Value)
                        {
                            dict[reader.GetString(0)] = reader.GetString(1);
                        }
                    }
                }

            }
            catch
            {
                bool raise = false;
                if (bool.TryParse(ConfigurationManager.AppSettings["Gettext.Throw"], out raise) && raise)
                {
                    throw;
                }
            }
            finally
            {
                connection.Close();
            }

            return dict.GetEnumerator();
        }

        public void Close()
        {
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        void IDisposable.Dispose()
        {
        }
    }
}
