using System;
using System.Collections.Generic;
using System.Linq;

namespace Netspark.CleanArchitecture.Scaffold
{
    internal class ControllerFile
    {
        public ControllerFile(string name, string content, string[] usings, IDictionary<string, string> actions)
        {
            Name = name;
            Content = content;
            Usings = usings;
            Actions = actions;
        }

        public string Name { get; }
        public string Content { get; }
        public string[] Usings { get; }
        public IDictionary<string, string> Actions { get; }

        public string AppendMissingActions(string existingContent)
        {
            var lines = existingContent.Split(Environment.NewLine);
            var isFileScoped = IsFileScopedNs(lines, out var nsIndex);

            if (nsIndex == -1)
                return existingContent;

            var header = lines
                .Take(nsIndex + (isFileScoped ? 1 : 2))
                .Where(m => !m.TrimStart().StartsWith("using "))
                .ToArray();

            var existingUsings = lines
                .Where(m => m.TrimStart().StartsWith("using "))
                .ToArray();

            var indent = existingUsings.Max(m => m.IndexOf("using "));

            var combinedUsings = existingUsings
                .Concat(Usings)
                .Select(m => m.TrimStart())
                .Distinct()
                .Select(m => $"{string.Empty.PadLeft(indent, ' ')}{m}")
                .OrderBy(m => m)
                .ToArray();

            var lastExistingUsingIndex = Array.IndexOf(lines, existingUsings.LastOrDefault());
            int skipped = 0;
            var content = lines
                .Skip(lastExistingUsingIndex + 1)// header and usings
                .Reverse()// go from end
                .SkipWhile(m =>
                {
                    if (m.Trim().Contains("}")) skipped++;
                    return skipped <= (isFileScoped ? 1 : 2);
                })
                .Reverse()// go from start
                .ToArray();

            var actionsToAppend = Actions.Keys
                .Where(a => !existingContent.Contains(a))
                .Select(m => Actions[m]);

            var closingSquares = isFileScoped
                ? new[] { "}" }
                : new[] { "    }", "}" };

            var newContent = header
                .Concat(combinedUsings)
                .Concat(content)
                .Concat(actionsToAppend)
                .Concat(closingSquares);

            var outContent = string.Join(Environment.NewLine, newContent);

            return outContent;
        }

        private bool IsFileScopedNs(string[] lines, out int namespaceIndex)
        {
            namespaceIndex = -1;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("namespace "))
                {
                    namespaceIndex = i;
                    return line.EndsWith(';');
                }
            }

            return false;
        }
    }

}
