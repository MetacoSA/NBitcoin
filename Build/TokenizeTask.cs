using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Build
{
	public class TokenizeTask : Task
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

		public string OutputFile
		{
			get;
			set;
		}

		public string Configuration
		{
			get;
			set;
		}
		public override bool Execute()
		{
			var input = File.ReadAllText(InputFile);

			var assemblyFile = File.ReadAllText(AssemblyFile);


			string output = input;
			output = Replace(assemblyFile, output, "AssemblyVersion", "$version$");
			output = Replace(assemblyFile, output, "AssemblyProduct", "$author$");

			File.WriteAllText(OutputFile, output);
			return true;
		}

		private string Replace(string file, string input, string attribute, string token)
		{
			var match = Regex.Match(file, "\\[assembly: " + attribute + "\\(\"(.*?)\"\\)\\]");
			var value = match.Groups[1].Value;
			return input.Replace(token, value);
		}

	}
}
