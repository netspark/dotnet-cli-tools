using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ude;

namespace Netspark.Utilities.FullRename
{
    public class Replacer
    {
        public async Task Replace(ReplaceOptions options)
        {
            try
            {
                if (!options.Verbose)
                {
                    Console.SetOut(TextWriter.Null);
                }

                Console.WriteLine("Checking target '{0}'...", options.Target);
                var type = options.GetTargetType();
                if (!type.HasValue)
                {
                    Console.WriteLine("File or directory '{0}' not found!", options.Target);
                    return;
                }

                var (newTarget, files) = ReplaceNames(options);
                options.Target = newTarget;

                await ReplaceContent(options, files);
            }
            finally
            {
                if (!options.Verbose)
                {
                    Console.Out.Close();
                }
            }
        }

        private (string Target, string[] Files) ReplaceNames(ReplaceOptions options)
        {
            var (search, replace, ignoreCase, target) =
            (
                options.Search,
                options.Replace,
                options.IgnoreCase,
                options.Target
            );

            var type = options.GetTargetType();
            switch (type)
            {
                case TargetType.File:
                    var file = RenameFile(target, search, replace, ignoreCase);
                    return (file, new[] { file });
                case TargetType.Directory:
                    return RenameDirectoryRecursive(target, search, replace, ignoreCase);
                default:
                    throw new NotSupportedException($"{type}");
            }
        }

        private async Task ReplaceContent(ReplaceOptions options, string[] files)
        {
            var (search, replace, ignoreCase, target) =
            (
                options.Search,
                options.Replace,
                options.IgnoreCase,
                options.Target
            );

            var supportedExtensions = options.ReplaceExtensions;

            foreach (var file in files)
            {
                Console.WriteLine("Processing file '{0}'...", file);

                var ext = Path.GetExtension(file);
                if (supportedExtensions.Contains(ext))
                {
                    var cdet = GetCharsetDetector(file);

                    if (cdet.Charset != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("Charset: {0}, confidence: {1}... ", cdet.Charset, cdet.Confidence);
                        var encoding = Encoding.GetEncoding(cdet.Charset);

                        var original = await File.ReadAllTextAsync(file, encoding);
                        var content = Rename(original, search, replace, ignoreCase);
                        if (content != original)
                        {
                            await File.WriteAllTextAsync(file, content, encoding);
                            Console.Write("Saving content... Done.{0}", Environment.NewLine);
                        }
                        else
                        {
                            Console.Write("Done.{0}", Environment.NewLine);
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Charset detection failed, skipping...");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("File extension '{0}' not listed. Done", ext);
                }

                Console.ResetColor();
                Console.WriteLine();
            }
        }

        private static ICharsetDetector GetCharsetDetector(string file)
        {
            ICharsetDetector cdet;
            using (var fs = File.OpenRead(file))
            {
                cdet = new CharsetDetector();
                cdet.Feed(fs);
                cdet.DataEnd();
            }

            return cdet;
        }

        private (string, string[]) RenameDirectoryRecursive(string target, string search, string replace, bool ignoreCase)
        {
            var comparison = ignoreCase
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.InvariantCulture;

            target = RenameDir(target, search, replace, ignoreCase);

            var directories = GetDirectories(target, search, ignoreCase);
            RenameDirectories(directories, search, replace, ignoreCase, comparison);

            var files = GetFiles(target, search, ignoreCase);
            RenameFiles(search, replace, ignoreCase, files);

            return (target, files);
        }

        private void RenameFiles(string search, string replace, bool ignoreCase, string[] files)
        {
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                files[i] = RenameFile(file, search, replace, ignoreCase);
            }
        }

        private void RenameDirectories(string[] directories, string search, string replace, bool ignoreCase, StringComparison comparison)
        {
            for (var i = 0; i < directories.Length; i++)
            {
                var oldName = directories[i];
                var newName = RenameDir(oldName, search, replace, ignoreCase);
                if (oldName == newName)
                    continue;

                // Replace all children referring to old dir name
                for (var j = 0; j < directories.Length; j++)
                {
                    if (i == j)
                        continue;

                    var other = directories[j];
                    var oldSearch = Path.TrimEndingDirectorySeparator(oldName) + Path.DirectorySeparatorChar;
                    if (other.StartsWith(oldSearch, comparison))
                    {
                        var remain = other.Length - oldName.Length;
                        directories[j] = newName + other.Substring(oldName.Length - 1, remain);
                    }
                }
            }
        }

        private static string[] GetDirectories(string target, string search, bool ignoreCase)
        {
            var comparison = ignoreCase
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.InvariantCulture;

            EnumerationOptions options = GetOptions(ignoreCase);

            // sort by top to bottom
            var sep = Path.DirectorySeparatorChar.ToString();
            var directories = Directory
                .GetDirectories(target, "*", options)
                .Where(f => f.Contains(search, comparison))
                .OrderBy(f => Regex.Matches(f, Regex.Escape(sep)).Count)
                .ThenBy(f => f)
                .ToArray();
            return directories;
        }

        private static string[] GetFiles(string target, string search, bool ignoreCase)
        {
            var comparison = ignoreCase
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.InvariantCulture;

            EnumerationOptions options = GetOptions(ignoreCase);

            // sort by top to bottom
            var sep = Path.DirectorySeparatorChar.ToString();
            var files = Directory
                .GetFiles(target, "*.*", options)
                //.Where(f => f.Contains(search, comparison))
                .OrderBy(f => Regex.Matches(f, Regex.Escape(sep)).Count)
                .ThenBy(f => f)
                .ToArray();

            return files;
        }

        private static EnumerationOptions GetOptions(bool ignoreCase)
        {
            return new EnumerationOptions
            {
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false,
                MatchCasing = ignoreCase
                    ? MatchCasing.CaseInsensitive
                    : MatchCasing.CaseSensitive,
                MatchType = MatchType.Simple
            };
        }

        private string Rename(string target, string search, string replace, bool ignoreCase, Action<string, string> action = null)
        {
            var comparison = ignoreCase
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.InvariantCulture;

            if (target.Contains(search, comparison))
            {
                var newName = target.Replace(search, replace, comparison);
                action?.Invoke(target, newName);
                return newName;
            }

            return target;
        }

        private string RenameFile(string target, string search, string replace, bool ignoreCase)
        {
            return Rename(target, search, replace, ignoreCase, (o, n) =>
            {
                if (!File.Exists(n))
                    File.Move(o, n);
                else
                {
                    Console.WriteLine("Cannot rename {0} to {1} because target already exists!", o, n);
                    //Console.ReadKey();
                }
            });
        }

        private string RenameDir(string target, string search, string replace, bool ignoreCase)
        {
            return Rename(target, search, replace, ignoreCase, (o, n) =>
            {
                if (!Directory.Exists(n))
                    Directory.Move(o, n);
                else
                {
                    Console.WriteLine("Cannot rename {0} to {1} because target already exists!", o, n);
                    //Console.ReadKey();
                }
            });
        }
    }
}
