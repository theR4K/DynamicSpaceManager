using BaseX;
using FrooxEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ComponentsReplacer
{
    public class Replacer
    {
        public static void Apply(Harmony harmony)
        {
            Type[] types = AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly());

            foreach (Type t in types)
            {
                if (t.GetCustomAttribute<ReplaceComponentAttribute>() != null &&
                    Array.IndexOf(t.GetInterfaces(), typeof(IWorker)) >= 0)
                {
                    MethodInfo miOriginal = AccessTools.Method(t.BaseType, "__New");
                    MethodInfo miPrefix = AccessTools.Method(t, "__NewOverride");
                    if (miOriginal != null && miPrefix != null)
                    {
                        harmony.Patch(miOriginal, new HarmonyMethod(miPrefix));
                        WorkerManagerPath.Replacements.Add(t.BaseType, t);
                        UniLog.Log($"Created replacement: {t.BaseType.FullName} -> {t.FullName}");
                    }
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ReplaceComponentAttribute : Attribute { }

    [HarmonyPatch(typeof(WorkerManager))]
    public static class WorkerManagerPath
    {
        internal static Dictionary<Type, Type> Replacements = new Dictionary<Type, Type>();

        [HarmonyPrefix]
        [HarmonyPatch(nameof(WorkerManager.Instantiate), typeof(Type))]
        public static bool InstantiatePrefix(ref IWorker __result, Type type)
        {
            if (Replacements.TryGetValue(type, out var replacement))
            {
                __result = Activator.CreateInstance(replacement) as IWorker;
                return false;
            }

            return true;
        }
    }

    /*
     * template
     * 

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
     */
}
