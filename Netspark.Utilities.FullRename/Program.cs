using Fclp;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Netspark.Utilities.FullRename
{
    class Program
    {
        static async Task Main(string[] args)
        {
            bool helpRequested = false;
            var parser = new FluentCommandLineParser<ReplaceOptions>();

            parser.Setup(arg => arg.Target)
                .As('t', "target")
                .SetDefault("./")
                .WithDescription("Target filesystem entry (file or folder) to perform deep replacement in");

            parser.Setup(arg => arg.Search)
                .As('s', "search")
                .Required()
                .WithDescription("Search prase or text to find");

            parser.Setup(arg => arg.Replace)
                .As('r', "replace")
                .Required()
                .WithDescription("Replacement for the search prase");

            parser.Setup(arg => arg.Extensions)
                .As('e', "extensions")
                .WithDescription("File extensions to replace content in");

            parser.Setup(arg => arg.IgnoreCase)
                .As('i', "ignore-case")
                .SetDefault(false)
                .WithDescription("Ignore case in file search");

            parser.Setup(arg => arg.Multiterm)
                .As('m', "multiterm")
                .SetDefault(false)
                .WithDescription("Performs multiple passes and replaces each next search with corresponding next replace. ");

            parser.Setup(arg => arg.Verbose)
                .As('v', "verbose")
                .SetDefault(false)
                .WithDescription("If specified, logs all operations to standard output");

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
                var replacer = new Replacer();
                if (!options.Multiterm)
                {
                    await replacer.Replace(options);
                }
                else
                {
                    var (searches, replaces) = GetMultiterms(options);
                    for(var i = 0; i < searches.Length; i++)
                    {
                        options.Search = searches[i];
                        options.Replace = replaces[i];
                        await replacer.Replace(options);
                    }
                }
            }
        }

        private static (string[] Searches, string[] Replaces) GetMultiterms(ReplaceOptions options)
        {
            var searches = options.Search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var replaces = options.Replace.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (searches.Length != replaces.Length)
                throw new Exception("When multiterm option present, search and replace arrays should have the same number of space-separated terms");

            var len = Math.Min(searches.Length, replaces.Length);
            return (searches.Take(len).ToArray(), replaces.Take(len).ToArray());
        }
    }
}
