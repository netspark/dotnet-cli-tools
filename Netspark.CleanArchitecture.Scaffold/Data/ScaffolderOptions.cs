namespace Netspark.CleanArchitecture.Scaffold
{
    public class ScaffolderOptions
    {
        private bool _generateFull;

        public string ConfigFile { get; set; }

        public string OutputFolder { get; set; }

        public MergeStrategy MergeStrategy { get; set; }

        public string TemplatesVersion { get; set; }

        public bool GenerateUnitTests { get; set; }

        public bool GenerateIntegrationTests { get; set; }

        public bool GenerateCommands { get; set; }

        public bool GenerateQueries { get; set; }

        public bool GenerateControllerActions { get; set; }

        public bool GenerateHandlers { get; set; }

        public bool GenerateValidators { get; set; }

        public bool GenerateEvents { get; set; }

        public bool GenerateExamples { get; set; }

        public bool GenerateFull
        {
            get => _generateFull;
            set
            {
                _generateFull = value;
                if (value)
                {
                    GenerateUnitTests = true;
                    GenerateIntegrationTests = true;
                    GenerateCommands = true;
                    GenerateQueries = true;
                    GenerateControllerActions = true;
                    GenerateHandlers = true;
                    GenerateValidators = true;
                    GenerateEvents = true;
                    GenerateExamples = true;
                }
            }
        }
    }
}
