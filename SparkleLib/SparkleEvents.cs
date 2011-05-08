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
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;

namespace SparkleLib {

	// Arguments for most events
	public class SparkleEventArgs : EventArgs {

	    public string Type;
	    public string Message;


	    public SparkleEventArgs (string type)
    	{
	        Type = type;
	    }
	}


	// Arguments for the NewCommit event
	public class NewCommitArgs : EventArgs {

	    public string UserName;
	    public string UserEmail;
	    public string Message;
	    public string LocalPath;


        public NewCommitArgs (string user_name, string user_email,
                              string message, string local_path)
    	{
    		UserName  = user_name;
    		UserEmail = user_email;
	        Message   = message;
	        LocalPath = local_path;
	    }
	}
}
