using BaseX;
using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Reflection;
using ComponentsReplacer;

namespace DynamicSpaceManager
{
    public class DynamicSpaceManagerMod : NeosMod
    {
        public override string Name => "DynamicSpaceManager";
        public override string Author => "The_R4K_";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/theR4K/DynamicSpaceManager";

        public override void OnEngineInit()
        {
            var harmony = new Harmony("org.theR4K.DynamicSpaceManager");
            Replacer.Apply(harmony);
            harmony.PatchAll();
        }
    }


    [ReplaceComponent]
    public partial class DynamicVariableSpace : FrooxEngine.DynamicVariableSpace, IWorker
    {
        // override type for saving routine
        Type IWorker.WorkerType => GetType().BaseType;
        string IWorker.WorkerTypeName => ((IWorker)this).WorkerType.FullName;

        // fix saving for non zero version types
        public override int Version => 0;
        public int OriginalVersion => base.Version;

        public override DataTreeNode Save(SaveControl control)
        {
            // allow all checks to complete
            DataTreeNode result = base.Save(control);

            if (OriginalVersion > 0)
            {
                control.RegisterTypeVersion(WorkerType, OriginalVersion);
            }
            return result;
        }

        public static bool __NewOverride(ref IWorker __result)
        {
            __result = Activator.CreateInstance(MethodBase.GetCurrentMethod().DeclaringType) as IWorker;
            return false;
        }
    }
}
