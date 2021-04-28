using System.IO;

namespace Netspark.Tools.DumpConfig
{
    public class DumpOptions
    {
        /// <summary>
        /// Target appsettings.json file or directory with appsettings json files
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the output file name.
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// The name of the environment to set in appsettings.{env}.json during the load if the directory specified
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Dump values along with keys
        /// </summary>
        public bool WithValues { get; set; }

        /// <summary>
        /// For folder mode, load appsettings.Local.json if found
        /// </summary>
        public bool WithLocal { get; set; }

        /// <summary>
        /// Dump everything even if base appsettings has the same value
        /// </summary>
        public bool WithDuplicates { get; set; }

        /// <summary>
        /// Separator for sections, i.e. __ or :: etc
        /// </summary>
        public string Separator { get; set; } = "__";

        public TargetType? GetTargetType() => File.Exists(Target)
            ? TargetType.File
            : Directory.Exists(Target)
                ? TargetType.Directory
                : (TargetType?)null;

        public TargetType? GetOutputType() => File.Exists(OutputFile)
            ? TargetType.File
            : Directory.Exists(OutputFile)
                ? TargetType.Directory
                : (TargetType?)null;
    }

    public enum TargetType
    {
        File,
        Directory
    }
}
