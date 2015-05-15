using Ploeh.AutoFixture.Xunit2;
using SparkleLib.Tests.Fixtures;
using System.IO;
using Xunit;

namespace SparkleLib.Tests {
    public class SparkleFolderTests {

        [Theory, AutoData (typeof (SparkleConfigFixture))]
        public void FolderReturnsDirectoryInfoForFolder (string folder_name, ISparkleConfig config)
        {
            var folder = new SparkleFolder (config);

            var result = folder.GetInfo (folder_name).FullName;

            var expected_full_path = Path.GetFullPath (Path.Combine (config.FoldersPath, folder_name));
            Assert.Equal (expected_full_path, result);

            // Cleanup
            Directory.Delete (Path.Combine ("config_path"), true);
        }

        [Theory, AutoData (typeof (SparkleConfigFixture))]
        public void FolderReturnsDirectoryInfoForCustomFolder (string folder_name, ISparkleConfig config)
        {
            var folder = new SparkleFolder(config);

            var custom_folder_path = Path.GetFullPath (Path.Combine ("custom", "SparkleShare"));

            config.AddFolder (folder_name, "", "", "");
            config.SetFolderOptionalAttribute (folder_name, "path", custom_folder_path);

            var result = folder.GetInfo (folder_name).FullName;

            var expected_full_path = Path.GetFullPath (Path.Combine (custom_folder_path, folder_name));
            Assert.Equal (expected_full_path, result);

            // Cleanup
            Directory.Delete (Path.Combine ("config_path"), true);
        }

    }
}