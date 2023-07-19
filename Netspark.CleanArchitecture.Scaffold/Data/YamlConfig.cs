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
        public string TemplatesVersion { get; set; } = "v1";
        public string DtoSuffix = "Dto";
        public string VmSuffix = "Vm";

        public string ConfigFolder { get; set; }
        public string OutputFolder { get; set; }
        public MergeStrategy MergeStrategy { get; set; }

        public bool GenerateUnitTests { get; set; }

        public bool GenerateIntegrationTests { get; set; }

        public bool GenerateCommands { get; set; }

        public bool GenerateQueries { get; set; }

        public bool GenerateControllerActions { get; set; }

        public bool GenerateExamples { get; set; }

        public bool GenerateHandlers { get; set; }

        public bool GenerateValidators { get; set; }

        public bool GenerateEvents { get; set; }

        public IList<AppNode> Domains { get; set; } = new List<AppNode>();
    }
}
