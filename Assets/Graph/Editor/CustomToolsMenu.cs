using Graph.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Graph.Editor
{
    /// <summary>
    /// Builds the "Custom Tools" toolbar dropdown for <see cref="GraphEditorWindow"/>.
    /// Entries come from every class marked with GraphCustomTools.Logic.CustomToolMenuAttribute.
    /// Choosing an entry calls the tool's static <c>Open(GraphAsset)</c> method.
    /// Both the attribute and the tool are resolved by reflection because GraphCustomToolsLogic already
    /// references GraphEditor, so GraphEditor cannot reference them back (cyclic asmdef reference).
    /// </summary>
    internal static class CustomToolsMenu
    {
        private const string AttributeAssemblyName = "GraphCustomToolsEditor";
        private const string AttributeTypeName = "GraphCustomTools.Editor.CustomToolMenuAttribute";
        private const string AttributePathProperty = "Path";
        private const string OpenMethodName = "Open";

        /// <param name="getGraph">Returns the graph currently loaded in the window (may be null).</param>
        public static ToolbarMenu Create(Func<GraphAsset> getGraph)
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

            foreach (var (type, path) in tools)
            {
                Type toolType = type;
                menu.menu.AppendAction(
                    path,
                    _ => Open(toolType, getGraph()),
                    // Tools work on a graph, so they are unavailable until one is loaded.
                    _ => getGraph() != null
                        ? DropdownMenuAction.Status.Normal
                        : DropdownMenuAction.Status.Disabled);
            }

            return menu;
        }

        /// <summary>Opens a custom tool for the given graph by calling its static Open(GraphAsset).</summary>
        private static void Open(Type toolType, GraphAsset graph)
        {
            if (graph == null)
            {
                Debug.LogWarning($"Cannot open {toolType.Name}: no graph is loaded.");
                return;
            }

            MethodInfo open = toolType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == OpenMethodName
                    && !m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 1
                    && m.GetParameters()[0].ParameterType.IsAssignableFrom(typeof(GraphAsset)));

            if (open == null)
            {
                Debug.LogWarning($"{toolType.Name} has no public static {OpenMethodName}({nameof(GraphAsset)}) method.");
                return;
            }

            try
            {
                open.Invoke(null, new object[] { graph });
            }
            catch (TargetInvocationException e)
            {
                Debug.LogException(e.InnerException ?? e);
            }
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
