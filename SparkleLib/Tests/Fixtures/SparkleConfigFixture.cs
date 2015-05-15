using Ploeh.AutoFixture;

namespace SparkleLib.Tests.Fixtures {
    public class SparkleConfigFixture : Fixture {
        public SparkleConfigFixture ()
        {
            Customize<ISparkleConfig> (
                composer =>
                    composer.FromFactory (
                        () => new SparkleConfig ("config_path", "config_file_name"))
                        .Do (config => SparkleConfig.DefaultConfig = config));
        }
    }
}