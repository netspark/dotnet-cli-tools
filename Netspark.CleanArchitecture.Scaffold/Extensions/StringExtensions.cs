using System;
using System.Collections.Generic;
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
	}
}
