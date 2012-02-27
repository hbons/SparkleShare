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

using Gettext.Cs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Gettext.Cs.Tests
{
    /// <summary>
    ///This is a test class for PoParserTest and is intended
    ///to contain all PoParserTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PoParserTest
    {
        ///<summary>
        /// Tests slashes in parsed strings
        /// <see href="http://code.google.com/p/gettext-cs-utils/issues/detail?id=1"/>
        ///</summary>
        [TestMethod()]
        public void ParseIntoDictionaryStringWithSlashesTest()
        {
            string msgid = @"The type of parameter \""${0}\"" is not supported";
            string msgstr = @"Il tipo del parametro \""${0}\"" non è supportato";

            string parsedMsgid = @"The type of parameter ""${0}"" is not supported";
            string parsedMsgstr = @"Il tipo del parametro ""${0}"" non è supportato";

            PoParser target = new PoParser();
            TextReader reader = new StringReader(String.Format(@"
            msgid ""{0}""
            msgstr ""{1}""
            ", msgid, msgstr));
            
            var actual = target.ParseIntoDictionary(reader);

            Assert.AreEqual(1, actual.Count, "Parsed dictionary entries count do not match");
            Assert.AreEqual(parsedMsgid, actual.Keys.ToArray()[0], "Key does not match");
            Assert.AreEqual(parsedMsgstr, actual.Values.ToArray()[0], "Value does not match");
        }
    }
}
