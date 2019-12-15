using Fclp;
using Netspark.CleanArchitecture.Scaffold.Extensions;
using Netspark.CleanArchitecture.Scaffold.Utils;
using SharpYaml.Serialization;
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

            parser.Setup(arg => arg.MergeStrategy)
                .As('m', "merge-strategy")
                .SetDefault(MergeStrategy.Skip)
                .WithDescription("Merge strategy for the scaffolding: Append|Overwrite|Skip");

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
