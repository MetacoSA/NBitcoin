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

		public string TargetFramework
		{
			get;
			set;
		}

		public string FrameworkName
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
				AddDependency(builder, dep);
			}

			if(!String.IsNullOrEmpty(FrameworkName))
			{
				var deps = projectJson["frameworks"][FrameworkName]["dependencies"];
				if(deps != null)
				{
					foreach(var dep in deps.Children().OfType<JProperty>())
					{
						AddDependency(builder, dep);
					}
				}
			}

			var nuspec = File.ReadAllText(InputFile);
			var group = "<group targetFramework=\"" + TargetFramework + "\">\r\n";
			nuspec = nuspec.Replace(group, group + builder.ToString());
			File.WriteAllText(OutputFile, nuspec);
			return true;
		}

		private static void AddDependency(StringBuilder builder, JProperty dep)
		{
			builder.AppendLine("<dependency id=\"" + dep.Name + "\" version=\"[" + (string)dep.Value + ", )\" />");
		}
	}
}
