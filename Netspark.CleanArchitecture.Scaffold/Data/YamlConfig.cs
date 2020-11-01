using System.Collections.Generic;

namespace Netspark.CleanArchitecture.Scaffold
{
    public class YamlConfig
    {
        public string Namespace { get; set; } = "FaceFK";
        public string UiSuffix { get; set; } = "WebUI";
        public string UiPath { get; set; } = "WebUI";
        public string AppPath { get; set; } = "Application";
        public string AppSuffix { get; set; } = "Application";
        public string DbContext { get; set; } = "IFaceDbContext";
        public string SrcPath { get; set; } = "./Src";
        public string TestsPath { get; set; } = "./Tests";
        public string ApiUrlPrefix { get; set; } = "api/v1";

        public string ConfigFolder { get; set; }
        public string OutputFolder { get; set; }
        public MergeStrategy MergeStrategy { get; set; }

        public IList<AppNode> Domains { get; set; } = new List<AppNode>();
    }
}
