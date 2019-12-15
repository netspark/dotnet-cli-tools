using System.Collections.Generic;

namespace Netspark.CleanArchitecture.Scaffold
{
    public class YamlConfig
    {
        public string Namespace { get; set; } = "FaceFK";
        public string DbContext { get; set; } = "IFaceDbContext";
        public string SrcPath { get; set; } = "./Src";
        public string TestsPath { get; set; } = "./Tests";

        public string ConfigFolder { get; set; }
        public string OutputFolder { get; set; }
        public MergeStrategy MergeStrategy { get; set; }

        public IList<AppNode> Domains { get; set; } = new List<AppNode>();
    }
}
