using Fclp;
using Netspark.CleanArchitecture.Scaffold.Extensions;
using Netspark.CleanArchitecture.Scaffold.Utils;
using System;
using System.IO;

namespace Netspark.CleanArchitecture.Scaffold
{
    class Program
    {
        static void Main(string[] args)
        {
            bool helpRequested = false;
            var parser = new FluentCommandLineParser<ScaffolderOptions>();

            parser.Setup(arg => arg.ConfigFile)
                .As('c', "config-file")
                .SetDefault("./cleanasc.yaml")
                .WithDescription("Configuration file in yaml format for this tool (absolute or relative to the command process working directory)");

            parser.Setup(arg => arg.OutputFolder)
                .As('o', "output-folder")
                .SetDefault("./")
                .WithDescription("The root folder for commands/queries tree generation (absolute or relative to the command process working directory)");

            parser.Setup(arg => arg.TemplatesVersion)
                .As('t', "templates-version")
                .SetDefault("./")
                .WithDescription("The version of template files used in scaffolder (v1 or v2)");

            parser.Setup(arg => arg.MergeStrategy)
                .As('m', "merge-strategy")
                .SetDefault(MergeStrategy.Skip)
                .WithDescription("Merge strategy for the scaffolding: Append|Overwrite|Skip");

            parser.Setup(arg => arg.GenerateIntegrationTests)
                .As('i', "gen-integration-tests")
                .SetDefault(false)
                .WithDescription("Generate integration tests");

            parser.Setup(arg => arg.GenerateUnitTests)
                .As('u', "gen-unit-tests")
                .SetDefault(false)
                .WithDescription("Generate unit tests");

            parser.Setup(arg => arg.GenerateCommands)
                .As('d', "gen-commands")
                .SetDefault(false)
                .WithDescription("Generate applicaiton commands");

            parser.Setup(arg => arg.GenerateQueries)
                .As('q', "gen-queries")
                .SetDefault(false)
                .WithDescription("Generate applicaiton queries");

            parser.Setup(arg => arg.GenerateControllerActions)
                .As('a', "gen-api")
                .SetDefault(false)
                .WithDescription("Generate controllers and actions");

            parser.Setup(arg => arg.GenerateHandlers)
                .As('h', "gen-handlers")
                .SetDefault(false)
                .WithDescription("Generate applicaiton command/query handlers");

            parser.Setup(arg => arg.GenerateValidators)
                .As('v', "gen-validators")
                .SetDefault(false)
                .WithDescription("Generate fluent validators for commands or queries");

            parser.Setup(arg => arg.GenerateEvents)
                .As('e', "gen-events")
                .SetDefault(false)
                .WithDescription("Generate events for executed commands");

            parser.Setup(arg => arg.GenerateExamples)
                .As('x', "gen-examples")
                .SetDefault(false)
                .WithDescription("Generate swagger examples for controller actions");

            parser.Setup(arg => arg.GenerateFull)
                .As('f', "gen-full")
                .SetDefault(false)
                .WithDescription("Generate all possible templates");

            parser.SetupHelp("?", "help")
                .UseForEmptyArgs()
                .Callback(text => 
                {
                    Console.WriteLine(text);
                    helpRequested = true;
                });

            var result = parser.Parse(args);
            if (result.HasErrors)
            {
                Console.WriteLine(result.ErrorText);
                parser.HelpOption.ShowHelp(parser.Options);
                return;
            }

            var options = parser.Object;
            if (!helpRequested)
            {
                var config = ReadYamlConfig(PathUtils.GetAbsolutePath(options.ConfigFile));
                new Scaffolder(options, config, new VerbService())
                    .Run();
            }
        }

        private static YamlConfig ReadYamlConfig(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("YAML config not found!", path);

            var config = SharpYamlExtensions.DeserializeYaml(File.ReadAllText(path));
            if (config == null)
                throw new Exception($"Invalid YAML configuration {path}");

            return config;
        }
    }
}
