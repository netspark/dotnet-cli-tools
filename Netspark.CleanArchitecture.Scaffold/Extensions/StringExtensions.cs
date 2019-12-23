using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Netspark.CleanArchitecture.Scaffold.Extensions
{
    public static class StringExtensions
    {
		public static string[] SplitIntoWords(this string s)
		{
			if (string.IsNullOrEmpty(s) || s.Trim().Length == 0)
				return new string[] { };

			var sb = new StringBuilder(s);
			int i = 1;
			while (i < sb.Length)
			{
				if (char.IsUpper(sb[i]))
				{
					sb.Insert(i, ' ');
					i += 2;
				}
				else
				{ 
					i++; 
				}

			}

			return sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		}


		public static bool EndsWithAny(this string s, params string[] suffixes) =>
			suffixes?.Any(p => s?.EndsWith(p) ?? false) ?? false;

		public static bool StartsWithAny(this string s, params string[] prefixes) =>
			prefixes?.Any(p => s?.StartsWith(p) ?? false) ?? false;

	}
}
