using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Netspark.CleanArchitecture.Scaffold
{
    public interface IVerbService
    {
        string GetPastSimpleForm(string verbBase);
    }

    public class VerbService : IVerbService
    {
		private static IDictionary<string, IrregularVerb> _irregulars = new[]
		{
			new[] {"be","was","been"},
			new[] {"beat","beat","beaten"},
			new[] {"become","became","become"},
			new[] {"begin","began","begun"},
			new[] {"bend","bent","bent"},
			new[] {"bet","bet","bet"},
			new[] {"bid","bid","bid"},
			new[] {"bite","bit","bitten"},
			new[] {"blow","blew","blown"},
			new[] {"break","broke","broken"},
			new[] {"bring","brought","brought"},
			new[] {"build","built","built"},
			new[] {"burn","burned","burned"},
			new[] {"buy","bought","bought"},
			new[] {"catch","caught","caught"},
			new[] {"choose","chose","chosen"},
			new[] {"come","came","come"},
			new[] {"cost","cost","cost"},
			new[] {"cut","cut","cut"},
			new[] {"dig","dug","dug"},
			new[] {"dive","dove","dived"},
			new[] {"do","did","done"},
			new[] {"draw","drew","drawn"},
			new[] {"dream","dreamed","dreamed"},
			new[] {"drive","drove","driven"},
			new[] {"drink","drank","drunk"},
			new[] {"eat","ate","eaten"},
			new[] {"fall","fell","fallen"},
			new[] {"feel","felt","felt"},
			new[] {"fight","fought","fought"},
			new[] {"find","found","found"},
			new[] {"fly","flew","flown"},
			new[] {"forget","forgot","forgotten"},
			new[] {"forgive","forgave","forgiven"},
			new[] {"freeze","froze","frozen"},
			new[] {"get","got","gotten"},
			new[] {"give","gave","given"},
			new[] {"go","went","gone"},
			new[] {"grow","grew","grown"},
			new[] {"hang","hung","hung"},
			new[] {"have","had","had"},
			new[] {"hear","heard","heard"},
			new[] {"hide","hid","hidden"},
			new[] {"hit","hit","hit"},
			new[] {"hold","held","held"},
			new[] {"hurt","hurt","hurt"},
			new[] {"keep","kept","kept"},
			new[] {"know","knew","known"},
			new[] {"lay","laid","laid"},
			new[] {"lead","led","led"},
			new[] {"leave","left","left"},
			new[] {"lend","lent","lent"},
			new[] {"let","let","let"},
			new[] {"lie","lay","lain"},
			new[] {"lose","lost","lost"},
			new[] {"make","made","made"},
			new[] {"mean","meant","meant"},
			new[] {"meet","met","met"},
			new[] {"pay","paid","paid"},
			new[] {"put","put","put"},
			new[] {"read","read","read"},
			new[] {"ride","rode","ridden"},
			new[] {"ring","rang","rung"},
			new[] {"rise","rose","risen"},
			new[] {"run","ran","run"},
			new[] {"say","said","said"},
			new[] {"see","saw","seen"},
			new[] {"sell","sold","sold"},
			new[] {"send","sent","sent"},
			new[] {"show","showed","shown"},
			new[] {"shut","shut","shut"},
			new[] {"sing","sang","sung"},
			new[] {"sit","sat","sat"},
			new[] {"sleep","slept","slept"},
			new[] {"speak","spoke","spoken"},
			new[] {"spend","spent","spent"},
			new[] {"stand","stood","stood"},
			new[] {"swim","swam","swum"},
			new[] {"take","took","taken"},
			new[] {"teach","taught","taught"},
			new[] {"tear","tore","torn"},
			new[] {"tell","told","told"},
			new[] {"think","thought","thought"},
			new[] {"throw","threw","thrown"},
			new[] {"understand","understood","understood"},
			new[] {"wake","woke","woken"},
			new[] {"wear","wore","worn"},
			new[] {"win","won","won"},
			new[] {"write","wrote","written"}
		}
		.ToDictionary(s => s[0], s => new IrregularVerb(s));

		public string GetPastSimpleForm(string verbBase)
        {
			if (string.IsNullOrWhiteSpace(verbBase))
				return verbBase;

			var verbKey = verbBase.ToLower();

			string simple = _irregulars.ContainsKey(verbKey) 
				? _irregulars[verbKey].PastSimple
				: GetRegularPastSimple(verbBase);
			
			return char.IsUpper(verbBase[0])
				? char.ToUpperInvariant(simple[0]) + simple.Substring(1)
				: simple;
		}

		private string GetRegularPastSimple(string verbBase)
		{
			if (verbBase.EndsWith("y", StringComparison.OrdinalIgnoreCase))
				return verbBase.TrimEnd('y', 'Y') + "ied";
			
			if (verbBase.EndsWith("e", StringComparison.OrdinalIgnoreCase))
				return verbBase + "d";

			return verbBase + "ed";
		}


		public class IrregularVerb
		{
			public IrregularVerb() { }
			public IrregularVerb(string[] vector)
			{
				if (vector.Length < 3)
					throw new ArgumentOutOfRangeException("vector should have minimum 3 elements");

				Base = vector[0];
				PastSimple = vector[0];
				PastParticiple = vector[0];
			}

			public string Base { get; set; }
			public string PastSimple { get; set; }
			public string PastParticiple { get; set; }
		}
	}
}
