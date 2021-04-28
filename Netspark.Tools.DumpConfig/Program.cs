using Fclp;
using System;
using System.Threading.Tasks;

namespace Netspark.Tools.DumpConfig
{
    class Program
    {
        static async Task Main(string[] args)
        {
            bool helpRequested = false;
            var parser = new FluentCommandLineParser<DumpOptions>();

            parser.Setup(arg => arg.Target)
                .As('t', "target")
                .SetDefault("./")
                .WithDescription("Target appsettings.json file or directory with appsettings json files");

            parser.Setup(arg => arg.OutputFile)
                .As('o', "output-dir")
                .SetDefault("./")
                .WithDescription("Output directory to write result in format appsettings.{env?}.env");

            parser.Setup(arg => arg.Environment)
                .As('e', "env")
                .Required()
                .WithDescription("The name of the environment to set in appsettings.{env}.json during the load");

            parser.Setup(arg => arg.WithValues)
                .As('v', "values")
                .SetDefault(false)
                .WithDescription("Dump values along with keys");

            parser.Setup(arg => arg.WithLocal)
                .As('l', "local")
                .SetDefault(false)
                .WithDescription("For folder mode, load appsettings.Local.json if found");

            parser.Setup(arg => arg.WithDuplicates)
                .As('d', "duplicates")
                .SetDefault(false)
                .WithDescription("Dump everything even if base appsettings has the same value");

            parser.Setup(arg => arg.Separator)
                .As('s', "separator")
                .SetDefault("__")
                .WithDescription("Separator for sections, i.e. __ or :: etc");
            
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
                var dumper = new Dumper(options);
                await dumper.Dump();
            }
        }

    }
}
