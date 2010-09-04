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

namespace SparkleLib {

	// Arguments for most events
	public class SparkleEventArgs : System.EventArgs {
        
	    public string Type;
	    public string Message;


	    public SparkleEventArgs (string type)
    	{

	        Type = type;

	    }

	}


	// Arguments for the NewCommit event
	public class NewCommitArgs : System.EventArgs {

        
	    public string Author;
	    public string Email;
	    public string Message;
	    public string RepositoryName;

	    public NewCommitArgs (string author, string email, string message, string repository_name)
    	{

    		Author  = author;
    		Email   = email;
	        Message = message;
	        RepositoryName = repository_name;

	    }

	}

}
