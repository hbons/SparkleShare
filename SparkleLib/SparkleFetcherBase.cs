//   SparkleShare, a collaboration and sharing tool.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace SparkleLib {

    public class SparkleFetcherInfo {
        public string Address;
        public string RemotePath;
        public string Backend;
        public string Fingerprint;
        public string TargetDirectory;
        public string AnnouncementsUrl;
        public bool FetchPriorHistory;
    }


    public abstract class SparkleFetcherBase {

        public event Action Started = delegate { };
        public event Action Failed = delegate { };

        public event FinishedEventHandler Finished = delegate { };
        public delegate void FinishedEventHandler (bool repo_is_encrypted, bool repo_is_empty, string [] warnings);

        public event ProgressChangedEventHandler ProgressChanged = delegate { };
        public delegate void ProgressChangedEventHandler (double percentage, double speed);


        public abstract bool Fetch ();
        public abstract void Stop ();
        public abstract bool IsFetchedRepoEmpty { get; }
        public abstract bool IsFetchedRepoPasswordCorrect (string password);
        public abstract void EnableFetchedRepoCrypto (string password);

        public double ProgressPercentage { get; private set; }
        public double ProgressSpeed { get; private set; }

        public Uri RemoteUrl { get; protected set; }
        public string RequiredFingerprint { get; protected set; }
        public readonly bool FetchPriorHistory = false;
        public string TargetFolder { get; protected set; }
        public bool IsActive { get; protected set; }
        public string Identifier;
        public SparkleFetcherInfo OriginalFetcherInfo;

        public string [] Warnings {
            get {
                return this.warnings.ToArray ();
            }
        }

        public string [] Errors {
            get {
                return this.errors.ToArray ();
            }
        }

        
        protected List<string> warnings = new List<string> ();
        protected List<string> errors   = new List<string> ();

        protected string [] ExcludeRules = new string [] {
            "*.autosave", // Various autosaving apps
            "*~", // gedit and emacs
            ".~lock.*", // LibreOffice
            "*.part", "*.crdownload", // Firefox and Chromium temporary download files
            ".*.sw[a-z]", "*.un~", "*.swp", "*.swo", // vi(m)
            ".directory", // KDE
            "*.kate-swp", // Kate
            ".DS_Store", "Icon\r", "._*", ".Spotlight-V100", ".Trashes", // Mac OS X
            "*(Autosaved).graffle", // Omnigraffle
            "Thumbs.db", "Desktop.ini", // Windows
            "~*.tmp", "~*.TMP", "*~*.tmp", "*~*.TMP", // MS Office
            "~*.ppt", "~*.PPT", "~*.pptx", "~*.PPTX",
            "~*.xls", "~*.XLS", "~*.xlsx", "~*.XLSX",
            "~*.doc", "~*.DOC", "~*.docx", "~*.DOCX",
            "*.a$v", // QuarkXPress
            "*/CVS/*", ".cvsignore", "*/.cvsignore", // CVS
            "/.svn/*", "*/.svn/*", // Subversion
            "/.hg/*", "*/.hg/*", "*/.hgignore", // Mercurial
            "/.bzr/*", "*/.bzr/*", "*/.bzrignore", // Bazaar
            "*<*", "*>*", "*:*", "*\"*", "*|*", "*\\?*", "*\\**", "*\\\\*" // Not allowed on Windows systems,
            // see (http://msdn.microsoft.com/en-us/library/aa365247(v=vs.85).aspx)
        };


        private Thread thread;


        public SparkleFetcherBase (SparkleFetcherInfo info)
        {
            OriginalFetcherInfo = info;
            RequiredFingerprint = info.Fingerprint;
            FetchPriorHistory   = info.FetchPriorHistory;
            string remote_path  = info.RemotePath.Trim ("/".ToCharArray ());
            string address      = info.Address;

            if (address.EndsWith ("/"))
                address = address.Substring (0, address.Length - 1);

            if (!remote_path.StartsWith ("/"))
                remote_path = "/" + remote_path;

            if (!address.Contains ("://"))
                address = "ssh://" + address;

            TargetFolder = info.TargetDirectory;

            RemoteUrl = new Uri (address + remote_path);
            IsActive  = false;
        }


        public void Start ()
        {
            IsActive = true;
            Started ();

            SparkleLogger.LogInfo ("Fetcher", TargetFolder + " | Fetching folder: " + RemoteUrl);

            if (Directory.Exists (TargetFolder))    
                Directory.Delete (TargetFolder, true);

            this.thread = new Thread (() => {
                if (Fetch ()) {
                    Thread.Sleep (500);
                    SparkleLogger.LogInfo ("Fetcher", "Finished");

                    IsActive = false;

                    bool repo_is_encrypted = (RemoteUrl.AbsolutePath.Contains ("-crypto") ||
                                              RemoteUrl.Host.Equals ("sparkleshare.net"));

                    Finished (repo_is_encrypted, IsFetchedRepoEmpty, Warnings);

                } else {
                    Thread.Sleep (500);

                    if (IsActive) {
                        SparkleLogger.LogInfo ("Fetcher", "Failed");
                        Failed ();
                    
                    } else {
                        SparkleLogger.LogInfo ("Fetcher", "Failed: cancelled by user");
                    }

                    IsActive = false;
                }
            });

            this.thread.Start ();
        }


        public virtual void Complete ()
        {
            string identifier_path = Path.Combine (TargetFolder, ".sparkleshare");

            if (File.Exists (identifier_path)) {
                Identifier = File.ReadAllText (identifier_path).Trim ();
            
            } else {
                Identifier = CreateIdentifier ();
                File.WriteAllText (identifier_path, Identifier);

                CreateInitialChangeSet ();
            }

            File.SetAttributes (identifier_path, FileAttributes.Hidden);
        }


        // Create an initial change set when the
        // user has fetched an empty remote folder
        private void CreateInitialChangeSet ()
        {
            string file_path = Path.Combine (TargetFolder, "SparkleShare.txt");
            string n = Environment.NewLine;

            UriBuilder uri_builder = new UriBuilder (RemoteUrl);

            if (RemoteUrl.Scheme.Contains ("http")) {
                uri_builder.UserName = "";
                uri_builder.Password = "";
            }

            string text = "Congratulations, you've successfully created a SparkleShare repository!" + n +
                n +
                "Any files you add or change in this folder will be automatically synced to " + n +
                uri_builder.ToString () + " and everyone connected to it." + n +
                n +
                "SparkleShare is an Open Source software program that helps people collaborate and " + n +
                "share files. If you like what we do, consider buying us a beer: http://www.sparkleshare.org/" + n +
                n +
                "Have fun! :)" + n;

            if (RemoteUrl.AbsolutePath.Contains ("-crypto") || RemoteUrl.Host.Equals ("sparkleshare.net"))
                text = text.Replace ("a SparkleShare repository", "an encrypted SparkleShare repository");

            File.WriteAllText (file_path, text);
        }


        public static string CreateIdentifier ()
        {
            return Path.GetRandomFileName ().SHA1 ();
        }


        public void Dispose ()
        {
            if (this.thread != null)
                this.thread.Abort ();
        }


        protected void OnProgressChanged (double percentage, double speed) {
            ProgressChanged (percentage, speed);
        }


        protected string GenerateCryptoSalt ()
        {
            string salt = Path.GetRandomFileName ().SHA1 ();
            return salt.Substring (0, 16);
        }


        public static string GetBackend (string address)
        {
            if (address.StartsWith ("ssh+")) {
				string backend = address.Substring (0, address.IndexOf ("://"));
				backend        = backend.Substring (4);

                return char.ToUpper (backend [0]) + backend.Substring (1);

            } else {
                return "Git";
            }
        }
    }
}
