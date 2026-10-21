using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Graph.Editor
{
    /// <summary>
    /// Builds the "Custom Tools" toolbar dropdown for <see cref="GraphEditorWindow"/>.
    /// Entries come from every class marked with GraphCustomTools.Logic.CustomToolMenuAttribute.
    /// The attribute is resolved by name (reflection) because GraphCustomToolsLogic already
    /// references GraphEditor, so GraphEditor cannot reference it back (cyclic asmdef reference).
    /// </summary>
    internal static class CustomToolsMenu
    {
        private const string AttributeAssemblyName = "GraphCustomToolsLogic";
        private const string AttributeTypeName = "GraphCustomTools.Logic.CustomToolMenuAttribute";
        private const string AttributePathProperty = "Path";

        public static ToolbarMenu Create()
        {
            var menu = new ToolbarMenu
            {
                text = "Custom Tools",
                tooltip = "Custom tools available for graphs."
            };

            var tools = FindTools();

            if (tools.Count == 0)
            {
                menu.menu.AppendAction(
                    "No custom tools found",
                    _ => { },
                    _ => DropdownMenuAction.Status.Disabled);
                return menu;
            }

            foreach (var (_, path) in tools)
            {
                // Intentionally empty: tool execution is not wired up yet.
                menu.menu.AppendAction(path, _ => { });
            }

            return menu;
        }

        private static List<(Type type, string path)> FindTools()
        {
            var result = new List<(Type, string)>();

            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == AttributeAssemblyName);
            Type attributeType = assembly?.GetType(AttributeTypeName);
            if (attributeType == null)
                return result;

            PropertyInfo pathProperty = attributeType.GetProperty(AttributePathProperty);

            foreach (Type type in TypeCache.GetTypesWithAttribute(attributeType))
            {
                if (type.IsAbstract)
                    continue;

                object attribute = type.GetCustomAttribute(attributeType, false);
                string raw = pathProperty?.GetValue(attribute) as string;
                result.Add((type, MenuPath(type, raw)));
            }

            return result
                .OrderBy(x => x.Item2, StringComparer.Ordinal)
                .ToList();
        }

        private static string MenuPath(Type type, string raw)
        {
            raw = string.IsNullOrWhiteSpace(raw) ? "Misc/" + type.Name : raw.Trim('/');

            // Nicify only the leaf so group names stay as authored.
            int slash = raw.LastIndexOf('/');
            return slash < 0
                ? ObjectNames.NicifyVariableName(raw)
                : raw[..slash] + "/" + ObjectNames.NicifyVariableName(raw[(slash + 1)..]);
        }
    }
}
