using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Build
{
	public class SetCoreVersion : Task
	{
		public string InputFile
		{
			get;
			set;
		}

		public string AssemblyFile
		{
			get;
			set;
		}

		public override bool Execute()
		{
			var input = File.ReadAllText(InputFile);
			var assemblyFile = string.IsNullOrEmpty(AssemblyFile) ? null : File.ReadAllText(AssemblyFile);
			string version = assemblyFile == null ? "1.0.0.0" : Get(assemblyFile, "AssemblyVersion");

			input = Regex.Replace(input, "\"version\" : \"(.*?)\"", e=>
			{
				return "\"version\" : \"" + version + "\"";
			});

			File.WriteAllText(InputFile, input);
			return true;
		}

		private string Get(string file, string attribute)
		{
			var match = Regex.Match(file, "\\[assembly: " + attribute + "\\(\"(.*?)\"\\)\\]");
			return match.Groups[1].Value;
		}

	}
}

