using Fclp;
using Netspark.CleanArchitecture.Scaffold.Extensions;
using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace Netspark.CleanArchitecture.Scaffold
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new FluentCommandLineParser<ScaffolderOptions>();

            YamlConfig config = null; 
            parser.Setup(arg => arg.ConfigFile)
                .As('c', "config-file")
                .SetDefault("./cleanasc.yaml")
                .WithDescription("Configuration file in yaml format for this tool")
                .Callback(p => config = ReadYamlConfig(p));

            parser.Setup(arg => arg.OutputFolder)
                .As('o', "output-folder")
                .SetDefault("./")
                .WithDescription("The root folder for commands/queries tree generation");

            parser.Setup(arg => arg.MergeStrategy)
                .As('m', "merge-strategy")
                .SetDefault(MergeStrategy.Skip)
                .WithDescription("Merge strategy for the scaffolding: Append|Overwrite|Skip|Sync");

            parser.SetupHelp("?", "help")
             .Callback(text => Console.WriteLine(text));

            var result = parser.Parse(args);
            if (result.HasErrors)
            {
                Console.WriteLine(result.ErrorText);
                parser.HelpOption.ShowHelp(parser.Options);
                return;
            }

            new Scaffolder(parser.Object).Run(config);
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

    public class ScaffolderOptions
    {
        public string ConfigFile { get; set; }
        public string OutputFolder { get; set; }
        public MergeStrategy MergeStrategy { get; set; }
    }

    public class YamlConfig
    {
        public string Namespace { get; set; } = "FaceFK";
        public string SrcPath { get; set; } = "./Src";
        public string TestsPath { get; set; } = "./Tests";

        public IList<AppNode> Domains { get; set; } = new List<AppNode>();
    }
   
    public enum MergeStrategy
    {
        Append,
        Overwrite,
        Skip,
        Sync
    }

    public class AppNode
    {
        public string Name { get; set; }
        public YamlNodeType Type { get; set; }
        public IList<AppNode> Children { get; set; } = new List<AppNode>();
        public AppNode Parent { get; set; }

        public string GetFullPath()
        {
            var sb = new Stack<string>();
            sb.Push(Name);

            var node = this;
            while (node.Parent != null)
            {
                node = node.Parent;
                sb.Push(node.Name);
            }

            return Path.Combine(sb.ToArray());
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public enum YamlNodeType
    {
        Folder,
        Command,
        Query
    }
}
