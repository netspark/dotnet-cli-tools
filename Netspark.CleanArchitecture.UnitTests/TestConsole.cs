using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Netspark.CleanArchitecture.UnitTests
{
    public class TestConsole
    {
        [Fact]
        public void StackTest()
        {
            var verbs = File.ReadAllText(@"c:\Projects\face-fk\__Materials\EnglishIrregularVerbs.html");
            var rows = verbs.Split("<tbody>\r\n", StringSplitOptions.RemoveEmptyEntries)[1]
                            .Split("<tr>\r\n", StringSplitOptions.RemoveEmptyEntries)
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .Select(s => s.Replace("<td><span>", "")
                                            .Replace("</span></td>", "")
                                            .Trim()
                                            .Split("\r\n"))
                            .Select(s => string.Join("\",\"", s[0].Trim(), s[1].Trim(), s[2].Trim()))
                            .Select(s => $"new[] {{\"{s}\"}},");

            File.WriteAllLines(@"c:\Projects\face-fk\__Materials\EnglishIrregularVerbs.txt", rows);

        }

        [Fact]
        public void PathTest()
        {
            var path = Path.GetDirectoryName("./Some/Path/File.cs");
        }
    }
}
