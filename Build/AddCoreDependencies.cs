using Microsoft.Build.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Build
{
	public class AddCoreDependencies : Task
	{
		public string InputFile
		{
			get;
			set;
		}

		public string ProjectJsonFile
		{
			get;
			set;
		}

		public string OutputFile
		{
			get;
			set;
		}

		public override bool Execute()
		{
			var projectJson = JObject.Parse(File.ReadAllText(ProjectJsonFile));
			StringBuilder builder = new StringBuilder();
			foreach(var dep in projectJson["dependencies"].Children().OfType<JProperty>())
			{
				builder.AppendLine("<dependency id=\"" + dep.Name + "\" version=\"[" + (string)dep.Value + ", )\" />");
			}
			var nuspec = File.ReadAllText(InputFile);
			var group = "<group targetFramework=\".NETStandard1.3\">\r\n";
			nuspec = nuspec.Replace(group, group + builder.ToString());
			File.WriteAllText(OutputFile, nuspec);
			return true;
		}
	}
}
