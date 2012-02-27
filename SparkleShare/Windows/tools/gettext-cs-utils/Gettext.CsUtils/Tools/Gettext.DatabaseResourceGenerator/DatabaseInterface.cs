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
using System.Data.SqlClient;

namespace Gettext.DatabaseResourceGenerator
{
    class DatabaseInterface : IDisposable
    {
        string connString;

        SqlConnection conn;
        SqlTransaction trans;

        public string KeyField { get; set; }
        public string ValueField { get; set; }
        public string CultureField { get; set; }

        public string KeyParam { get { return "@" + KeyField; } }
        public string ValueParam { get { return "@" + ValueField; } }
        public string CultureParam { get { return "@" + CultureField; } }
        
        public string TableName { get; set; }
        public string InsertSP { get; set; }
        public string DeleteSP { get; set; }
        public string GetSP { get; set; }

        public DatabaseInterface(string connString)
        {
            this.connString = connString;
            this.conn = new SqlConnection(connString);
        }

        public DatabaseInterface Init()
        {
            this.conn.Open();
            this.trans = conn.BeginTransaction();
            return this;
        }

        public void Commit()
        {
            this.trans.Commit();
        }

        public void InsertResource(string culture, string key, string value)
        {
            try
            {
                var command = new SqlCommand() { CommandText = InsertSP, CommandType = System.Data.CommandType.StoredProcedure, Connection = conn, Transaction = trans };
                command.Parameters.AddWithValue(CultureParam, culture);
                command.Parameters.AddWithValue(KeyParam, key);
                command.Parameters.AddWithValue(ValueParam, value);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error inserting resource for culture {0} key '{1}' value '{2}': {3}", culture, key, value, ex.Message);
                throw;
            }
        }

        public void DeleteResourceSet(string culture)
        {
            try
            {
                var command = new SqlCommand() { CommandText = DeleteSP, CommandType = System.Data.CommandType.StoredProcedure, Connection = conn, Transaction = trans };
                command.Parameters.AddWithValue(CultureParam, culture);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error deleting resource set for culture {0}: {1}", culture, ex.Message);
                throw;
            }
        }

        public void Prepare()
        {
            CheckI18NTable();
            if (!ExistsSP(GetSP)) CreateGetSP();
            if (!ExistsSP(InsertSP)) CreateInsertSP();
            if (!ExistsSP(DeleteSP)) CreateDeleteSP();
        }

        private void CreateDeleteSP()
        {
            Console.WriteLine(string.Format("Delete string resources stored procedure named {0} does not exist. Creating for table {1}...", DeleteSP, TableName));
            string cmd = String.Empty;

            try
            {
                cmd = String.Format(@"
                CREATE PROCEDURE [dbo].[{0}]	
                    @{2} VARCHAR(5)
                    AS
                    BEGIN
	                    DELETE FROM {1}	
	                    WHERE {2} = @{2}
                END", DeleteSP, TableName, CultureField);

                var command = conn.CreateCommand();
                command.CommandText = cmd;
                command.Transaction = trans;
                command.ExecuteScalar();


                Console.WriteLine("Created {0} stored procedure with parameter @{1} on table {2}.", DeleteSP, CultureField, TableName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error creating delete stored procedure: {0}\n\nCommand: {1}", ex.Message, cmd);
                throw;
            }
        }

        private void CreateGetSP()
        {
            Console.WriteLine(string.Format("Get resource set stored procedure named {0} does not exist. Creating for table {1}...", GetSP, TableName));
            string cmd = String.Empty;

            try
            {
                cmd = String.Format(@"
                CREATE PROCEDURE [dbo].[{0}]	
                    @{2} VARCHAR(5)
                    AS
                    BEGIN
	                    SELECT [{3}], [{4}] FROM [{1}]	
	                    WHERE [{2}] = @{2}
                END", GetSP, TableName, CultureField, KeyField, ValueField);

                var command = conn.CreateCommand();
                command.CommandText = cmd;
                command.Transaction = trans;
                command.ExecuteScalar();


                Console.WriteLine("Created {0} stored procedure with parameter @{1} on table {2}.", GetSP, CultureField, TableName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error creating get stored procedure: {0}\n\nCommand: {1}", ex.Message, cmd);
                throw;
            }
        }

        private void CreateInsertSP()
        {
            Console.WriteLine(string.Format("Insert string resource stored procedure named {0} does not exist. Creating for table {1}...", InsertSP, TableName));

            string cmd = String.Empty;
            try
            {
                cmd = String.Format(@"
                CREATE PROCEDURE [dbo].[{0}]	
	                @{2} VARCHAR(5),
	                @{3} VARCHAR(4000),
	                @{4} VARCHAR(4000)
                AS
                BEGIN
	                INSERT INTO [{1}] ([{2}], [{3}], [{4}])
                    VALUES (@{2}, @{3}, @{4})
                END", InsertSP, TableName, CultureField, KeyField, ValueField);

                var command = conn.CreateCommand();
                command.CommandText = cmd;
                command.Transaction = trans;
                command.ExecuteScalar();

                Console.WriteLine("Created {0} stored procedure with parameters @{1}, @{2}, @{3} on table {4}.", InsertSP, CultureField, KeyField, ValueField, TableName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error creating insert stored procedure: {0}\n\nCommand: {1}", ex.Message, cmd);
                throw;
            }
        }

        private void CheckI18NTable()
        {
            Console.WriteLine("Checking if the given table {0} exists...", TableName);

            var command = conn.CreateCommand();
            command.CommandText = "select case when exists((select * from information_schema.tables where table_name = '" + TableName + "')) then 1 else 0 end";
            command.Transaction = trans;

            bool existsTable = (int)command.ExecuteScalar() == 1;

            if (!existsTable)
            {
                try
                {
                    Console.WriteLine(string.Format("Table {0} does not exist. Creating table with fields {1} {2} {3}...", TableName, CultureField, KeyField, ValueField));
                    command.CommandText = string.Format("CREATE TABLE [dbo].[{0}] ([{1}] varchar(5) NOT NULL, [{2}] varchar(4000) NOT NULL, [{3}] varchar(4000))", TableName, CultureField, KeyField, ValueField);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error creating table: {0}\n\nCommand: {1}", ex.Message, command.CommandText);
                    throw;
                }

                try
                {
                    command.CommandText = string.Format("ALTER TABLE [{0}] ADD CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED ([{1}], [{2}])", TableName, CultureField, KeyField);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("Error creating table primary key: {0}\n\nCommand: {1}", ex.Message, command.CommandText);
                    throw;
                }
            }
        }


        private bool ExistsSP(string sp)
        {
            Console.WriteLine("Checking if stored procedure {0} exists...", sp);

            try
            {
                string cmd = String.Format("SELECT COUNT(*) FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[{0}]') AND type in (N'P', N'PC')", sp);

                var command = new SqlCommand(cmd, conn, trans);
                var count = (int)command.ExecuteScalar();

                return count > 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error checking SP {0}: {1}", sp, ex.Message);
                throw;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (this.conn != null)
            {
                this.conn.Dispose();
            }
        }

        #endregion


    }
}
