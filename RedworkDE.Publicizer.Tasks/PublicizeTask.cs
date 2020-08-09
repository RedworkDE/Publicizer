#if NUGET

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using dnlib.DotNet;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace RedworkDE.Publicizer.Tasks
{
	public class PublicizeTask : Task
	{
		[Required]
		public ITaskItem[] References { get; set; }

		[Required]
		public ITaskItem[] PublicizeAssemblies { get; set; }

		[Required]
		public string IntermediatePath { get; set; }

		public bool PublicizeAll { get; set; }
		public bool DefaultPublicizeInternal { get; set; }
		public bool DefaultPublicizePrivate { get; set; }
		public bool DefaultPublicizeReadonly { get; set; }
		public bool DefaultPublicizeEventBackingField { get; set; }


		[Output]
		public ITaskItem[] AddedReferences { get; set; }

		[Output]
		public ITaskItem[] RemovedReferences { get; set; }

		[Output]
		public ITaskItem[] GeneratedCodeFiles { get; set; }

		[Output]
		public string AdditionalDefines { get; set; }

		public override bool Execute()
		{
			if (References is null) throw new ArgumentNullException(nameof(References));
			if (PublicizeAssemblies is null) throw new ArgumentNullException(nameof(PublicizeAssemblies));
			if (IntermediatePath is null) throw new ArgumentNullException(nameof(IntermediatePath));

			Directory.CreateDirectory(IntermediatePath);

			var publicize = PublicizeAssemblies.ToDictionary(asm => asm.ItemSpec);
			if (!PublicizeAll && publicize.Count == 0)
			{
				Log.LogMessage(MessageImportance.Normal, "Publicize: no assemblies to process specified");
				return true;
			}
			else
			{
				Log.LogMessage(MessageImportance.Normal, "Publicize: assemblies to process: {0}", string.Join(", ", publicize.Select(kv => kv.Key)));
			}

			var added = new List<ITaskItem>();
			var removed = new List<ITaskItem>();
			var assemblies = new List<string>();

			var additionalDefines = new StringBuilder();

			foreach (var reference in References)
			{
				try
				{
					using var md = ModuleDefMD.Load(File.ReadAllBytes(reference.ItemSpec));
					var mdName = md.Assembly.Name.String;
					additionalDefines.Append(";");
					additionalDefines.Append("PUBLICIZER_");
					additionalDefines.Append(Regex.Replace(mdName.ToUpperInvariant(), "[^A-Za-z0-9]+", "_").Trim('_'));
					if (!publicize.TryGetValue(mdName, out var item) && !PublicizeAll)
					{
						Log.LogMessage(MessageImportance.Normal, "Publicize: not processing {0} / {1}", mdName, reference);
						continue;
					}
					Log.LogMessage(MessageImportance.Normal, "Publicize: processing {0} / {1}", mdName, reference);
					assemblies.Add(mdName);

					Patcher.Patch(md, 
						GetProperty(item, "Internal", DefaultPublicizeInternal),
						GetProperty(item, "Private", DefaultPublicizePrivate),
						GetProperty(item, "Readonly", DefaultPublicizeReadonly),
						GetProperty(item, "EventBackingField", DefaultPublicizeEventBackingField),
						false
						);

					var targetFile = Path.GetFullPath(Path.Combine(IntermediatePath, Path.GetFileName(reference.ItemSpec)));

					md.Write(targetFile);

					removed.Add(reference);
					var newItem = new TaskItem(targetFile);
					reference.CopyMetadataTo(newItem);
					newItem.SetMetadata("OriginalItemSpec", reference.GetMetadata("OriginalItemSpec"));
					added.Add(newItem);
				}
				catch (Exception ex)
				{
					Log.LogErrorFromException(ex);
					return false;
				}
			}

			if (assemblies.Count == 0)
			{
				Log.LogMessage(MessageImportance.Normal, "Publicize: no assemblies processed");
				return true;
			}
			Log.LogMessage(MessageImportance.Normal, "Publicize: processed {0} assemblies", assemblies.Count);
			
			AddedReferences = added.ToArray();
			RemovedReferences = removed.ToArray();
			AdditionalDefines = additionalDefines.ToString();

			GenerateAttributes(assemblies);

			return true;
		}

		private static bool GetProperty(ITaskItem item, string name, bool defaultValue)
		{
			return item is {} && bool.TryParse(item.GetMetadata(name), out var value) ? value : defaultValue;
		}


		public void GenerateAttributes(IEnumerable<string> assemblies)
		{
				var attributes = string.Concat(assemblies.Select(a => $"[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo(\"{a}\")]\n"));

				var content = attributes + @"
namespace System.Runtime.CompilerServices
{
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	internal sealed class IgnoresAccessChecksToAttribute : Attribute
	{
		public IgnoresAccessChecksToAttribute(string assemblyName)
		{
		}
	}
}";
				var filePath = Path.Combine(IntermediatePath, "IgnoresAccessChecksToAttribute.cs");
				File.WriteAllText(filePath, content);

				GeneratedCodeFiles = new ITaskItem[] { new TaskItem(filePath) };
		}
	}
}

#endif
