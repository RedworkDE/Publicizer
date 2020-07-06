#if STANDALONE

using System.IO;
using dnlib.DotNet;

namespace RedworkDE.Publicizer
{
	public class Program
	{
		public static void Main(string[] args)
		{
			foreach (var arg in args)
				DoPatch(arg);
		}

		private static void DoPatch(string asm)
		{
			var pubName = Path.Combine(Path.GetDirectoryName(asm), Path.GetFileNameWithoutExtension(asm) + ".Public" + Path.GetExtension(asm));
			
			var module = ModuleDefMD.Load(asm);

			Patcher.Patch(module, true, true, true, true, true);

			module.Write(pubName);
		}
	}
}

#endif