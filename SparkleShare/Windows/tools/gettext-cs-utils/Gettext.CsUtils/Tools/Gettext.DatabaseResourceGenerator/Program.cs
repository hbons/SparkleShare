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
using Gnu.Getopt;
using System.Reflection;
using Gettext.Cs;
using System.IO;
using System.Data.SqlClient;
using System.Configuration;

namespace Gettext.DatabaseResourceGenerator
{
    class Program
    {
        
        static void Main(string[] args)
        {
            var getopt = new Getopt(Assembly.GetExecutingAssembly().GetName().Name, args, "s:i:c:nadp") { Opterr = false };

            string input = null;
            string culture = null;

            bool dontDeleteSet = false;
            bool insertAll = false;
            bool skipValidation = false;
            bool onlyPrepare = false;

            string connString = ConfigurationManager.ConnectionStrings["Gettext"].ConnectionString;
            string insertSP = ConfigurationManager.AppSettings["SP.Insert"];
            string deleteSP = ConfigurationManager.AppSettings["SP.Delete"];
            string getSP = ConfigurationManager.AppSettings["SP.Get"];
            string tableName = ConfigurationManager.AppSettings["Table.Name"];
            string tableCulture = ConfigurationManager.AppSettings["Table.Fields.Culture"];
            string tableKey = ConfigurationManager.AppSettings["Table.Fields.Key"];
            string tableValue = ConfigurationManager.AppSettings["Table.Fields.Value"];

            int option;
            while (-1 != (option = getopt.getopt()))
            {
                switch (option)
                {
                    case 'i': input = getopt.Optarg; break;
                    case 'c': culture = getopt.Optarg; break;
                    case 'n': dontDeleteSet = true; break;
                    case 's': connString = getopt.Optarg; break;
                    case 'a': insertAll = true; break;
                    case 'd': skipValidation = true; break;
                    case 'p': onlyPrepare = true; break;
                    default: PrintUsage(); return;
                }
            }

            if (input == null && !onlyPrepare)
            {
                PrintUsage();
                return;
            }

            if (connString == null || getSP == null || insertSP == null || deleteSP == null || tableName == null || tableKey == null || tableCulture == null || tableValue == null)
            {
                Console.Out.WriteLine("Ensure that connection string, table name, table fields, insert and delete stored procedures are set in app config.");
                Console.Out.WriteLine();
                Console.Out.WriteLine("Expected connection strings are:");
                Console.Out.WriteLine(" Gettext");
                Console.Out.WriteLine();
                Console.Out.WriteLine("Expected app settings are:");
                Console.Out.WriteLine(" SP.Get");
                Console.Out.WriteLine(" SP.Insert");
                Console.Out.WriteLine(" SP.Delete");
                Console.Out.WriteLine(" Table.Name");
                Console.Out.WriteLine(" Table.Fields.Key");
                Console.Out.WriteLine(" Table.Fields.Value");
                Console.Out.WriteLine(" Table.Fields.Culture");
                return;
            }

            try
            {
                using (var db = new DatabaseInterface(connString)
                    {
                        GetSP = getSP,
                        DeleteSP = deleteSP,
                        InsertSP = insertSP,
                        CultureField = tableCulture,
                        KeyField = tableKey,
                        ValueField = tableValue,
                        TableName = tableName
                    })
                {
                    db.Init();

                    // Check if table and sps exist
                    if (!skipValidation || onlyPrepare)
                    {
                        db.Prepare();
                    }

                    // If only prepare is set, do not use po file, just make sure everything is ready to use it later
                    if (onlyPrepare)
                    {
                        Console.Out.WriteLine("Table and stored procedures ready.");
                        db.Commit();
                        return;
                    }

                    // Delete previous resource set for the specified culture
                    if (!dontDeleteSet)
                    {
                        db.DeleteResourceSet(culture);
                    }

                    // Dump the file into the database
                    var requestor = new DatabaseParserRequestor(culture, db, insertAll);
                    new PoParser().Parse(new StreamReader(input), requestor);

                    db.Commit();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Exception in program: {0}\n{1}", ex.Message, ex.StackTrace);
            }

        }

        
        private static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("Gettext Cs Tools");
            Console.WriteLine("----------------");
            Console.WriteLine();
            Console.WriteLine("Parses a .po file and adds its entries to a database's table.");
            Console.WriteLine("Then you can use a DatabaseResourceManager to use those resources at runtime.");
            Console.WriteLine("Usage: {0} -iINPUTFILE -cCULTURE -sCONNSTRING", Assembly.GetExecutingAssembly().GetName().Name);
            Console.WriteLine(" INPUTFILE Input file must be in po format (optional if -p).");
            Console.WriteLine(" CULTURE Culture for the resource set to handle (optional if -p).");
            Console.WriteLine(" CONNSTRING Connection string to override app config (optional).");
            Console.WriteLine("Options:");
            Console.WriteLine(" -p Only setup table and stored procedures, do not parse po file.");
            Console.WriteLine(" -n Dont delete previous resource set.");
            Console.WriteLine(" -a Insert all values, regardless of being empty.");
            Console.WriteLine(" -d Skip table and stored procedures validation and creation.");

        }
    }
}
