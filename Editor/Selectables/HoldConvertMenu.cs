using System;
using Onion.UI.Selectables;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Onion.UI.Editor {
    /// <summary>
    /// "Upgrade/Convert to Hold Button" on Button and "Upgrade/Convert to Hold Toggle" on Toggle. The hold types
    /// derive from them, so only the script is swapped: values and references to the component are kept.
    /// </summary>
    internal static class HoldConvertMenu {
        private const string ButtonMenu = "CONTEXT/Button/Upgrade/Convert to Hold Button";
        private const string ToggleMenu = "CONTEXT/Toggle/Upgrade/Convert to Hold Toggle";

        // Runs once per selected object.
        [MenuItem(ButtonMenu)]
        private static void ConvertButton(MenuCommand command) {
            Convert(command.context, typeof(Button), typeof(HoldButton));
        }

        [MenuItem(ButtonMenu, true)]
        private static bool CanConvertButton(MenuCommand command) {
            return CanConvert(command.context, typeof(Button));
        }

        [MenuItem(ToggleMenu)]
        private static void ConvertToggle(MenuCommand command) {
            Convert(command.context, typeof(Toggle), typeof(HoldToggle));
        }

        [MenuItem(ToggleMenu, true)]
        private static bool CanConvertToggle(MenuCommand command) {
            return CanConvert(command.context, typeof(Toggle));
        }

        // Only Unity's own type, not a subclass (a hold type included), whose fields would be lost.
        private static bool CanConvert(UnityEngine.Object context, Type from) {
            return context != null && context.GetType() == from;
        }

        private static void Convert(UnityEngine.Object context, Type from, Type to) {
            if (!CanConvert(context, from)) {
                return;
            }

            var script = FindScript(to);
            if (script == null) {
                Debug.LogError($"Can't find the script of {to.Name}.");
                return;
            }

            var serializedObject = new SerializedObject(context);
            serializedObject.FindProperty("m_Script").objectReferenceValue = script;
            serializedObject.ApplyModifiedProperties();
        }

        private static MonoScript FindScript(Type type) {
            foreach (var script in MonoImporter.GetAllRuntimeMonoScripts()) {
                if (script.GetClass() == type) {
                    return script;
                }
            }

            return null;
        }
    }
}
