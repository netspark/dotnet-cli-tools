using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Netspark.Tools.DumpConfig
{
    public class Dumper
    {
        private readonly DumpOptions _options;

        public Dumper(DumpOptions options)
        {
            _options = options;
        }

        public async Task Dump()
        {
            string[] files = GetAppsettingsFiles();
            var config = LoadConfiguration(files);

            var take = _options.WithLocal ? 2 : 1;
            var baseConfig = files.Length > 1 && !_options.WithDuplicates
                ? LoadConfiguration(files.Take(take).ToArray())
                : null;

            var entries = PrepareEntries(config, baseConfig);

            // write entries
            await File.WriteAllLinesAsync(_options.OutputFile, entries);
        }

        public string[] GetAppsettingsFiles()
        {
            var target = _options.Target;
            var env = _options.Environment;
            var files = _options.GetTargetType() switch
            {
                TargetType.File => new[] { target },
                TargetType.Directory => new[]
                {
                    Path.Combine(target, "appsettings.json"),
                    Path.Combine(target, $"appsettings.{env}.json"),
                    Path.Combine(target, "appsettings.Local.json"),
                },
                _ => throw new Exception($"ERROR: File or directory '{target}' does not exist!")
            };

            return files;
        }

        public IConfiguration LoadConfiguration(IList<string> files)
        {
            var configBuilder = new ConfigurationBuilder();
            for (int i = 0; i <  files.Count; i++)
            {
                var file = files[i];
                var required = i == 0;
                configBuilder.AddJsonFile(file, required);
            }

            var config = configBuilder.Build();
            return config;
        }

        public string[] PrepareEntries(IConfiguration config, IConfiguration baseConfig = null)
        {
            // filter entries
            var withValues = _options.WithValues;
            IEnumerable<KeyValuePair<string, string>> values = config
                .AsEnumerable()
                .Where(c => !string.IsNullOrEmpty(c.Value))
                .OrderBy(c => c.Key);

            if (baseConfig != null)
            {
                var filter = baseConfig
                    .AsEnumerable()
                    .ToDictionary(c => c.Key, c => c.Value);

                values = values.Where(c => filter.ContainsKey(c.Key) && filter[c.Key] != c.Value);
            }

            // select values
            var separator = _options.Separator;
            var entries = withValues
                ? values.Select(c =>
                {
                    var key = c.Key.Replace(":", separator);
                    return $"{key}={c.Value}";
                })
                : values.Select(c => c.Key);

            return entries.ToArray();
        }
    }
}
