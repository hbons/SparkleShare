//   MarkLogic SparkleShare backend extension
//   Adam Fowler <adam.fowler@marklogic.com>
//   Copyright 2013 MarkLogic Corporation

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using SparkleLib;
using System.Xml;
using System.Collections;
using MarkLogicLib;
using ServiceStack.Text;

namespace SparkleLib.Marklogic {

    public class SparkleRepo : SparkleRepoBase {

        // Create connection object to manage HTTP Comms
        private Connection connection = null;

        public SparkleRepo (string path, SparkleConfig config) : base (path, config)
        {
            //LocalPath = path; // TODO verify if we need this (probably do - LocalPath

            // TODO MUST DO OPTION 2 FOR POST FETCH ACCESS TO WORK

            // TODO complete function for ML
            // Create connection to ML Repo using this base file name
            // DEPRECATED/REMOVED Option 1: Base filename is a rest URL of the form http://admin:pwd@myserver.com:myport/
            // DONE Option 2: Read the base configuration from the deployed configuration file (E.g. if installed within an organisation and hardcoded)
            /*
             * String address = "";
            XmlNodeList list = config.GetElementsByTagName ("address");
            IEnumerator enumerator = list.GetEnumerator ();
            while (enumerator.MoveNext()) {
                // get this element's children (value element)
                XmlNode node = (XmlNode)enumerator.Current;
                IEnumerator children = node.ChildNodes.GetEnumerator();
                while (children.MoveNext()) {
                    XmlNode child = (XmlNode)children.Current;
                    if (child.LocalName == "value") {
                        address = child.InnerText;
                    }
                }
            }
            */
            connection = new Connection ();
            Options opts = new Options ();
            opts.setConnectionString (RemoteUrl.AbsoluteUri);
            connection.configure (opts);
        }

        // keeper
        public override List<string> ExcludePaths {
            get {
                List<string> rules = new List<string> ();
                rules.Add (".marklogic");

                return rules;
            }
        }


        /**
         * The current REMOTE (ML database) revision (the last one synced locally) - remote last-modified date of latest file synced
         */
        public override string CurrentRevision {
            get {
                // TODO replace with remote function call

                string file_path = new string [] { LocalPath, ".marklogic", "revision" }.Combine ();
                
                try {
                    string fl = File.ReadAllText (file_path);
                    return fl;
                    
                } catch {
                    return "0";
                }
            }
        }

        public override double Size {
            get {
                return 1;
            }
        }

        public override double HistorySize {
            get {
                return 1;
            }
        }

        public override bool HasRemoteChanges {
            get {
                // Get locally saved remote last modified date property (CurrentRevision)
                // Compare against remote version
                // fetch all files under this URI added after this last modified date
                // return true/false accordingly
                // NB if no version spec, fetch whole folder
                // NB warn for override (just incase of weird SNAFU in sync)

                // Use search to find files with revision (last modified date) greater than the one passed in
                // return 1 result only to save bandwidth (1 or more means return true)

                DocRefs refs = connection.listURIsModifiedSince(connection.options.baseuri,CurrentRevision);
                return (0 != refs.docuris.Length);

            }
        }

        public static double GetCurrentMillis()
        {
            return toMillis (DateTime.UtcNow);
        }

        public static double toMillis(DateTime nowUtc) {
            DateTime Jan1970 = new DateTime(1970, 1, 1, 0, 0,0,DateTimeKind.Utc);
            TimeSpan javaSpan = nowUtc - Jan1970;
            return javaSpan.TotalMilliseconds;
        }

        // Holds the souble currenttimemillis() toString last synced timestamp according to the client machine
        private double GetLastSyncTimestamp() {
            string file_path = new string [] { LocalPath, ".marklogic", "lastsynctimestamp" }.Combine ();
                
            try {
                double fl = Double.Parse(File.ReadAllText (file_path));
                return fl;
            } catch {
                return 0;
            }
        }
        private void SetLastSyncTimestamp(double newts) {
            string file_path = new string [] { LocalPath, ".marklogic", "lastsynctimestamp" }.Combine ();
                
            try {
                File.AppendAllText (file_path, newts.ToString());    
            } catch {
                return;
            }
        }

        /**
         * Push local changes only
         */
        public override bool SyncUp ()
        {
            // rewrite for ML
            // get last synced timestamp
            // fetch all files saved locally (last modified) since this timestamp
            // begin trans
            // do a document.save for all of these
            // progress update (supported yet by SparkleShare?)
            // commit trans

            double timestamp = GetLastSyncTimestamp();
            double nowtime = GetCurrentMillis (); // this means the sync doesn't stop you writing new files
            
            // get base folder
            // get all files to sync under this folder
            ICollection<string> files = null;
            // NB exclude .marklogic from this
            if (0 == timestamp) {
                // sync all files (push only)
                files = this.listFiles(LocalPath,0);
            } else {
                // TODO sync only newer files
                files = this.listFiles(LocalPath,timestamp);
            }

            // now do the sync

            // begin transaction
            Response result = connection.beginTransaction ();
            if (result.inError) {
                // TODO error reporting
            } else {

                // add each doc in turn
                try {
                  foreach (string path in files) {
                    Doc doc = new Doc();
                    doc.fromFile((new string[] {LocalPath,path}).Combine());
                    result = connection.save(doc,connection.options.baseuri + "/" + path);
                    if (result.inError) {
                        throw new Exception("Error saving document: " + path);
                    }
                  }

                  // commit transaction
                    result = connection.commitTransaction();
                    if (result.inError) {
                        throw new Exception("Exception committing transaction");
                    }

                  // set timestamp
                  SetLastSyncTimestamp(nowtime);
                    
                    
                    
                    // return true on success
                    return true;

                } catch (Exception e) {
                    // perform rollback
                    result = connection.rollbackTransaction();
                    // check rollback for error too (E.g. connection error)

                    // TODO log error
                }
            }



            return false;
        }

        public override bool SyncDown ()
        {
            bool result = doSyncDown (connection,CurrentRevision,LocalPath);
            if (result) {
                
                SetLastSyncTimestamp (toMillis (DateTime.Now));
            }
            return result;
        }

        public static bool doSyncDown(Connection cn,string currentRevision,string local) {
            
            // fetch current last-modified date locally
            // fetch all URIs remotely modified since this version
            // fetch each document set in turn (20 at a time? Just one? Several in parallel? In same trans?)
            
            
            // create structured search for all files starting with a URI with a last modified property greater than the revision
            // order by ascending modified date (if possible)
            // remember to save the highest revision that is downloaded
            // fetch each and store locally
            
            DocRefs remoteModifications = cn.listURIsModifiedSince (cn.options.baseuri, currentRevision);
            ArrayList uriList = remoteModifications.toArrayList();
            bool totalSuccess = true;
            
            long latestRevisionDateTime = 0;
            long latestRevision = 0;
            
            foreach (string uri in uriList) {
                SyncFileResult result = doFetchFile(cn,uri,local);
                totalSuccess = totalSuccess & result.success;
                if (result.success) {
                    if (0 == latestRevisionDateTime || Double.Parse(result.latestRevisionDateTime) > latestRevisionDateTime) {
                        latestRevision = Int64.Parse(result.latestRevision);
                        latestRevisionDateTime = Int64.Parse(result.latestRevisionDateTime);
                    }
                }
            }
            // TODO set Error = ErrorStatus.SOMETHING
            if (totalSuccess) {
                // update last modified time
                //CurrentRevision = latestRevision;
                return true;
            }
            return false;
            


        }

        public static SyncFileResult doFetchFile(Connection cn,string uri,string local) {
            
            SyncFileResult result = new SyncFileResult ();
            result.success = true;
            
            
            
            string relName = uri.Substring (cn.options.baseuri.Length);
            string fullpath = (new string[] {local,relName}).Combine();
            // ensure folders exist
            Directory.CreateDirectory (fullpath);
            // fetch file to local file system
            Response resp = cn.get (uri);
            if (resp.inError) {
                // TODO handle error
                result.success = false;
            } else {
                // write out file to local
                resp.doc.toFile (fullpath); // TODO code this method
                
                
                resp = cn.metadata ();
                if (resp.inError) {
                    result.success = false;
                } else {
                    string myLastModified = resp.doc.getJsonContent ().Get ("last-modified");
                    // get our date time
                    // see if it's greater than currently saved one
                    DateTime myDT = XmlConvert.ToDateTime (myLastModified);
                    result.latestRevisionDateTime = toMillis(myDT).ToString();
                    result.latestRevision = myLastModified;
                }
            }
            return result;
        }

        public class SyncFileResult
        {
            public bool success {get;set;}
            public string latestRevisionDateTime { get; set; }
            public string latestRevision { get; set; }
        }

        private SyncFileResult fetchFile(string uri) {
            return doFetchFile (connection, uri,LocalPath);
        }


        public override bool HasLocalChanges {
            get {
                // go through file system and find files with last modified date greater than last sync time stamp
                ICollection<string> changes = null;
                if (0 != GetLastSyncTimestamp()) {
                    //changes = listFilesModifiedSince(LocalPath,this.CurrentRevision);
                    changes = listFiles(LocalPath,GetLastSyncTimestamp());
                } else {
                    changes = listFiles(LocalPath,0);
                }

                return (changes.Count > 0);
            }
        }

        public override bool HasUnsyncedChanges {
            get {
                // TODO convert for ML

                return HasLocalChanges;
            }

            set {
                // do nothing - we'll manually check anyway
            }
        }




        public override void RestoreFile (string path, string revision, string target_file_path)
        {
            
            if (path == null)
                throw new ArgumentNullException ("path");
            
            if (revision == null)
                throw new ArgumentNullException ("revision");
            
            path = path.Replace ("\\", "/");

            // TODO support DLS so we can do this properly - for now, just fetch the latest revision on the server
            string uri = connection.options.baseuri + path;
            SyncFileResult result = fetchFile (uri);

            // TODO figure out which path is local on this user's machine (assuming its 'path')
        }


        public override List<SparkleChangeSet> GetChangeSets (string path)
        {
            return null;
        }   


        public override List<SparkleChangeSet> GetChangeSets ()
        {
            return null;
        }




        
        private ICollection<String> listFiles (string path,double timestampMillis)
        {
            ICollection<String> list = new List<string>();

            try {
                // files first
                foreach (string child_path in Directory.GetFiles(path)) {
                    if (IsSymlink (child_path))
                        continue;

                    // add file sub path to file list
                    bool add = true;
                    if (0 != timestampMillis && toMillis(File.GetLastWriteTimeUtc(child_path)) <= timestampMillis) {
                        add = false;
                    }
                    list.Add(child_path.Substring(path.Length + 1));
                }

                // now process subdirectories
                foreach (string child_path in Directory.GetDirectories (path)) {
                    if (IsSymlink (child_path))
                        continue;
                    
                    if (child_path.EndsWith (".marklogic")) 
                        continue;

                    ICollection<string> childList = listFiles (child_path,timestampMillis);
                    foreach (string el in childList) {
                        list.Add (el);
                    }
                }
                
            } catch (IOException e) {
                SparkleLogger.LogInfo ("MarkLogic", "Failed listing files in directory", e);
            }

            return list;
        }
        
        private bool IsSymlink (string file)
        {
            FileAttributes attributes = File.GetAttributes (file);
            return ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint);
        }
    }
}
