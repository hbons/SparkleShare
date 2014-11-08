using Sparkles;
using Squirrel;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SparkleShare {

    public static class SparkleUpdaterExtensions {

        public static void HandleEvents (this SparkleUpdater updater, params string [] arguments)
        {
            Extensions.RunSync (() => updater.HandleEventsAsync (arguments));
        }

        public static string CheckForNewVersion(this SparkleUpdater updater, ProgressSource progressSource)
        {
            return Extensions.RunSync (() => updater.CheckForNewVersionAsync (progressSource));
        }
    }

    public class ProgressSource
    {
        public event EventHandler<int> Progress;

        public void Raise(int i)
        {
            if (Progress != null)
                Progress.Invoke(this, i);
        }
    }

    public class SparkleUpdater {

        #if DEBUG
        private static bool restartRequired = true;
        #else
        private static bool restartRequired = false;
        #endif

        private static DateTime lastCheck = DateTime.MinValue;

        public static bool Prerelease
        {
            get
            {
                string prerelease_enabled = Configuration.DefaultConfiguration.GetConfigOption ("prerelease");

                if (string.IsNullOrEmpty (prerelease_enabled)) {
                    Configuration.DefaultConfiguration.SetConfigOption ("prerelease", bool.FalseString);
                    return true;

                } else {
                    return prerelease_enabled.Equals (bool.TrueString);
                }
            }
        }

        public static string ReleasesUrl
        {
            get
            {
                string releases_url = Configuration.DefaultConfiguration.GetConfigOption("releasesurl");

                if (string.IsNullOrEmpty (releases_url)) {
                    releases_url = "https://github.com/BarryThePenguin/SparkleShare";
                    Configuration.DefaultConfiguration.SetConfigOption ("releasesurl", releases_url);
                }

                return releases_url;
            }
        }

        public void TogglePrerelease ()
        {
            bool prerelease_enabled = Configuration.DefaultConfiguration.GetConfigOption ("prerelease").Equals (bool.TrueString);
            Configuration.DefaultConfiguration.SetConfigOption ("prerelease", (!prerelease_enabled).ToString ());
        }

        public SparkleUpdater () { }

        private static UpdateManager UpdateManager ()
        {
            return Extensions.RunSync (() => UpdateManagerAsync ());
        }

        private static async Task<UpdateManager> UpdateManagerAsync ()
        {
            return await Squirrel.UpdateManager.GitHubUpdateManager(ReleasesUrl, "sparkleshare", prerelease: Prerelease);
        }

        public async Task HandleEventsAsync (params string [] arguments)
        {
            try
            {
                using (var manager = await UpdateManagerAsync ()) {
                    SquirrelAwareApp.HandleEvents(onInitialInstall: v => OnInitialInstall (manager, v), onAppUpdate: v => OnAppUpdate (manager, v), onAppUninstall: v => OnAppUninstall (manager, v), arguments: arguments);
                }
            }
            catch (Exception ex) {
                Logger.LogInfo("SquirrelEvents", "Unable to handle squirrel events", ex);
            }
        }

        private static void UpdateHandlers ()
        {
            SparkleProtocolHandler.AddProtocolHandler ("sparkleshare", "URL:sparkleshare Protcol", "https:%1");
            SparkleProtocolHandler.AddProtocolHandler ("sparkleshare-unsafe", "URL:sparkleshare-unsafe Protcol", "http:%1");
            SparkleProtocolHandler.AddProtocolHandler ("github-windows", "URL:github-sparkleshare Protocol", "https:%1");
            SparkleProtocolHandler.AddProtocolHandler ("sourcetree", "URL:sourcetree-sparkleshare Protocol", "https:%1");
        }

        private static void OnInitialInstall (UpdateManager manager, Version version)
        {
            UpdateHandlers ();
            manager.CreateShortcutForThisExe ();
            Environment.Exit (0);
        }

        private static void OnAppUpdate (UpdateManager manager, Version version)
        {
            UpdateHandlers ();
            manager.CreateShortcutForThisExe ();
            manager.CreateShortcutsForExecutable ("SparkleShare.exe", ShortcutLocation.Startup, true);
            Environment.Exit (0);
        }

        private static void OnAppUninstall (UpdateManager manager, Version version)
        {
            SparkleProtocolHandler.RemoveProtocolHandler ("sparkleshare", "URL:sparkleshare Protcol");
            SparkleProtocolHandler.RemoveProtocolHandler ("sparkleshare-unsafe", "URL:sparkleshare-unsafe Protcol");
            SparkleProtocolHandler.RemoveProtocolHandler ("github-windows", "URL:github-sparkleshare Protocol");
            SparkleProtocolHandler.RemoveProtocolHandler ("sourcetree", "URL:sourcetree-sparkleshare Protocol");
            manager.RemoveShortcutsForExecutable ("SparkleShare.exe", ShortcutLocation.Startup);
            manager.RemoveShortcutForThisExe ();
            RemoveFromBookmarks ();
            Environment.Exit (0);
        }

        private static void RemoveFromBookmarks ()
        {
            string user_profile_path = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
            string shortcut_path = Path.Combine (user_profile_path, "Links", "SparkleShare.lnk");

            if (File.Exists (shortcut_path))
                File.Delete (shortcut_path);
        }

        public async Task Update ()
        {
            // If restart is required, wait for it, don't update
            if (restartRequired)
                return;

            // If last check was more than a day ago, check again
            var now = DateTime.Now;
            if (lastCheck < now.AddDays (-1)) {
                lastCheck = now;
            } else {
                return;
            }

            var progressSource = new ProgressSource ();

            EventHandler<int> progressSourceOnProgress = ((sender, p) =>
                SparkleShare.UI.Bubbles.Controller.ShowBubble ("Updating SparkleShare!",
                    "Update progress... " + p + "%",
                    null));

            progressSource.Progress += progressSourceOnProgress;

            try {
                using (var manager = await UpdateManagerAsync ()) {

                    var result = await manager.UpdateApp (progressSource.Raise);

                    if (result != null) {
                        restartRequired = true;
                        SparkleShare.UI.Bubbles.Controller.ShowBubble ("New Update Installed!",
                            "Restart SparkleShare to start the new version",
                            null);
                    }
                }
            } catch (Exception exception) {
                Logger.LogInfo ("Update", "Failed", exception);
            } finally {
                progressSource.Progress -= progressSourceOnProgress;
            }
        }

        public async Task<string> CheckForNewVersionAsync (ProgressSource progressSource)
        {
            string response = "Version check failed.";

            try {
                using (var manager = await UpdateManagerAsync ()) {
                    var releases = await manager.CheckForUpdate (progress: progressSource.Raise);

                    if (releases != null && releases.ReleasesToApply.Count > 0) {
                        response = "A newer version is available!";
                    } else {
                        response = "You are running the latest version.";
                    }
                }
            } catch (Exception exception) {
                Logger.LogInfo ("Update", response, exception);
            }

            return response;
        }

        internal void CreateStartupItem ()
        {
            using (var manager = UpdateManager ()) {
                manager.CreateShortcutsForExecutable ("SparkleShare.exe", ShortcutLocation.Startup, false);
            }
        }
    }
}