/*
 * コンパイルしてReiPatcher\Patchesに置いてください
 * csc /t:library /r:..\ReiPatcher.exe /r:..\mono.cecil.dll /r:..\mono.cecil.rocks.dll CM3D2.ArchiveReplacer.Patcher.cs
 */
// @AB_addarg /r:mono.cecil.dll
// @AB_addarg /r:mono.cecil.rocks.dll
// @AB_addarg /r:ReiPatcher.exe
// @AB_addarg /lib:%reipatcher%
// @AB_install %reipatcher%\Patches
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.Linq;

namespace CM3D2.ArchiveReplacer.Patcher
{
    public class ArchiveReplacerPatcher : ReiPatcher.Patch.PatchBase
    {
        const string patchTag = "CM3D2_ArchiveReplacer_PATCHED";
        AssemblyDefinition hookdef;

        public override bool CanPatch(ReiPatcher.Patch.PatcherArguments args)
        {
            return args.Assembly.Name.Name == "Assembly-CSharp" && !base.GetPatchedAttributes(args.Assembly).Any(a => a.Info == patchTag);

        }
        public override void PrePatch()
        {
            ReiPatcher.RPConfig.RequestAssembly("Assembly-CSharp.dll");
            string path = Path.Combine(base.AssembliesDir, "CM3D2.ArchiveReplacer.Hook.dll");
            using (Stream str = File.OpenRead(path))
            {
                hookdef = AssemblyDefinition.ReadAssembly(str);
            }
        }
        public override void Patch(ReiPatcher.Patch.PatcherArguments args)
        {
            TypeDefinition gameuty = args.Assembly.MainModule.GetType("GameUty");
            var initmethod = gameuty.Methods.First((MethodDefinition def) => def.Name == "Init");
            foreach (var il in initmethod.Body.Instructions)
            {
                if (il.OpCode == OpCodes.Newobj)
                {
                    MethodReference oprnd = (MethodReference)il.Operand;
                    if (oprnd.DeclaringType.ToString() == "FileSystemArchive")
                    {
                        var defHook_ctor = hookdef.MainModule.GetType("CM3D2.ArchiveReplacer.Hook.HookArchive")
                                .Methods.First((MethodDefinition def) => def.Name == ".ctor");

                        il.Operand = args.Assembly.MainModule.Import(defHook_ctor);
                    }
                }
            }
            base.SetPatchedAttribute(args.Assembly, patchTag);
        }
    }
}
