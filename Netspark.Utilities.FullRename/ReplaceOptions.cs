using System.IO;

namespace Netspark.Utilities.FullRename
{
    public class ReplaceOptions
    {
        public string Target { get; set; }
        public string Search { get; set; }
        public string Replace { get; set; }
        public bool IgnoreCase { get; set; }

        public string[] ReplaceExtensions = { }; 

        public bool Verbose { get; set; }

        public TargetType? GetTargetType() => File.Exists(Target)
            ? FullRename.TargetType.File
            : Directory.Exists(Target) 
                ? FullRename.TargetType.Directory 
                : (TargetType?)null;
    }

    public enum TargetType
    {
        File, 
        Directory
    }
}
