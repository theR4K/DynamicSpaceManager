using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace DynamicSpaceManager
{
    public partial class DynamicVariableSpace : ICustomInspector
    {
        private ICollection<ValueManager> ValueManagers;

        public DynamicVariableSpace() : base()
        {
            FieldInfo fi = AccessTools.Field(typeof(FrooxEngine.DynamicVariableSpace), "_dynamicValues");
            IDictionary my_dynamicValues = fi.GetValue(this) as IDictionary;
            ValueManagers = my_dynamicValues?.Values as ICollection<ValueManager>;
        }

        private void OnDynVarNameChanged(IChangeable a)
        {
            Sync<string> newVal = a as Sync<string>;
            foreach (ReferenceField<IDynamicVariable> rps in a.FindNearestParent<Slot>().GetComponents<ReferenceField<IDynamicVariable>>())
            {
                FieldInfo fi = rps.Reference.Target.GetType().GetField("VariableName");
                Sync<string> name = fi.GetValue(rps.Reference.Target) as Sync<string>;
                name.Value = newVal.Value;
            }
        }

        void ICustomInspector.BuildInspectorUI(UIBuilder ui)
        {
            WorkerInspector.BuildInspectorUI(this, ui);

            LocaleString localeText = "Registred variables:";
            Text textField = ui.Text(localeText, bestFit: true, Alignment.MiddleLeft, parseRTF: false);

            foreach (ValueManager valueManager in ValueManagers)
            {
                FieldInfo fi = AccessTools.Field(valueManager.GetType(), "values");
                IEnumerable values = fi.GetValue(valueManager) as IEnumerable;

                ui.HorizontalLayout(4f);
                Slot pmeRoot = ui.Root;
                pmeRoot.PersistentSelf = false;

                ui.Style.FlexibleWidth = 10f;
                ValueField<string> dynVarValueField = pmeRoot.AttachComponent<ValueField<string>>();
                dynVarValueField.Value.Value = CurrentName + (string.IsNullOrEmpty(CurrentName) ? string.Empty : "/") + valueManager.Name;
                dynVarValueField.Value.Changed += OnDynVarNameChanged;
                ui.PrimitiveMemberEditor(dynVarValueField.Value, "");

                ui.Style.FlexibleWidth = -1f;
                ui.Style.MinWidth = 60f;
                ui.Next("text");
                ui.Nest();
                localeText = $"<{valueManager.GetType().GenericTypeArguments.GetOrNull(0)?.Name ?? "unknown"}>";
                textField = ui.Text(localeText, true, Alignment.MiddleLeft, false);
                ui.NestOut();

                ui.Style.MinWidth = -1f;
                ui.Style.FlexibleWidth = -1f;
                ui.NestOut();

                foreach (object value in values)
                {
                    IDynamicVariable dynamicVariable = value as IDynamicVariable;

                    ui.Style.Height = -1f;
                    ui.Style.FlexibleHeight = 1f;
                    localeText = $" {dynamicVariable.Parent.ReferenceID} on {dynamicVariable.Parent.Name}(parent: {dynamicVariable.Parent.Parent.Name})";
                    textField = ui.Text(localeText, true, Alignment.MiddleLeft, false);
                    InteractionElement.ColorDriver colorDriver = textField.Slot.AttachComponent<Button>().ColorDrivers.Add();
                    colorDriver.ColorDrive.Target = textField.Color;
                    colorDriver.NormalColor.Value = color.Black;
                    colorDriver.HighlightColor.Value = color.Blue;
                    colorDriver.PressColor.Value = color.Blue;
                    textField.Slot.AttachComponent<ReferenceProxySource>().Reference.Target = dynamicVariable;
                    textField.Slot.PersistentSelf = false;
                    textField.VerticalAutoSize.Value = false;
                    textField.AutoSizeMax.Value = 20f;
                    ui.Style.FlexibleHeight = -1f;
                    ui.Style.Height = 24f;

                    // attach reference for rename functionality
                    pmeRoot.AttachComponent<ReferenceField<IDynamicVariable>>().Reference.Target = dynamicVariable;
                }
            }
        }
    }
}
