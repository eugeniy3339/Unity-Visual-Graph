using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GraphEditor
{
    /// <summary>
    /// Populates the "Add Block" search window from every non-abstract
    /// <see cref="GraphBlock"/> subclass in the project, grouped by
    /// <see cref="BlockMenuAttribute"/> path.
    /// </summary>
    internal sealed class BlockSearchWindowProvider : ScriptableObject, ISearchWindowProvider
    {
        private Action<Type, Vector2> _onSelect;

        public void Init(Action<Type, Vector2> onSelect) => _onSelect = onSelect;

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Add Block"), 0),
            };

            var items = TypeCache.GetTypesDerivedFrom<GraphBlock>()
                .Where(t => !t.IsAbstract && t != typeof(EntryBlock))
                .Select(t => (type: t, path: MenuPath(t)))
                .OrderBy(x => x.path, StringComparer.Ordinal)
                .ToList();

            var createdGroups = new HashSet<string>();

            foreach (var (type, path) in items)
            {
                string[] parts = path.Split('/');

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    string groupKey = string.Join("/", parts.Take(i + 1));
                    if (createdGroups.Add(groupKey))
                        tree.Add(new SearchTreeGroupEntry(new GUIContent(parts[i]), i + 1));
                }

                tree.Add(new SearchTreeEntry(new GUIContent(parts[^1]))
                {
                    level = parts.Length,
                    userData = type,
                });
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is Type type)
            {
                _onSelect?.Invoke(type, context.screenMousePosition);
                return true;
            }
            return false;
        }

        private static string MenuPath(Type type)
        {
            var attr = (BlockMenuAttribute)Attribute.GetCustomAttribute(type, typeof(BlockMenuAttribute));
            string raw = attr != null && !string.IsNullOrWhiteSpace(attr.Path)
                ? attr.Path
                : "Misc/" + type.Name;

            // Nicify only the leaf so group names stay as authored.
            int slash = raw.LastIndexOf('/');
            return slash < 0
                ? ObjectNames.NicifyVariableName(raw)
                : raw[..slash] + "/" + ObjectNames.NicifyVariableName(raw[(slash + 1)..]);
        }
    }
}
