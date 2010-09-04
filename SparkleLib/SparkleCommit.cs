//   SparkleShare, an instant update workflow to Git.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;

namespace SparkleLib {

	public class SparkleCommit
	{

		public string UserName;
		public string UserEmail;
		public DateTime DateTime;
		public string Hash;
		
		public List <string> Added;
		public List <string> Deleted;
		public List <string> Edited;
		public List <string> MovedFrom;
		public List <string> MovedTo;

		public SparkleCommit (string user_name, string user_email, DateTime date_time, string hash)
		{

			UserName  = user_name;
			UserEmail = user_email;
			DateTime  = date_time;
			Hash      = hash;

			Edited    = new List <string> ();
			Added     = new List <string> ();
			Deleted   = new List <string> ();
			MovedFrom = new List <string> ();
			MovedTo   = new List <string> ();

		}
	
	}

}
