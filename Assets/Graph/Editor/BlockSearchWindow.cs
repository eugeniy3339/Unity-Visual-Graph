using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Graph.Logic;

namespace Graph.Editor
{
    /// <summary>
    /// Unreal-style block search window.
    /// Scans all loaded assemblies for GraphBlock subclasses tagged with [BlockMenu],
    /// builds a categorized tree, and calls back with the chosen block instance.
    /// </summary>
    public class BlockSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private GraphAsset _asset;
        private Vector2 _spawnPosition;
        private Action<GraphBlock> _onBlockSelected;

        // Cached list rebuilt whenever the window opens
        private List<SearchTreeEntry> _cachedTree;

        // ── Static open helper ───────────────────────────────────────────────────
        public static void Open(GraphAsset asset, Vector2 spawnPosition, Action<GraphBlock> onSelected)
        {
            var provider = CreateInstance<BlockSearchWindow>();
            provider._asset = asset;
            provider._spawnPosition = spawnPosition;
            provider._onBlockSelected = onSelected;

            var context = new SearchWindowContext(GUIUtility.GUIToScreenPoint(spawnPosition));
            SearchWindow.Open(context, provider);
        }

        // ── ISearchWindowProvider ────────────────────────────────────────────────
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            if (_cachedTree != null) return _cachedTree;

            _cachedTree = new List<SearchTreeEntry>();

            // Root header
            _cachedTree.Add(new SearchTreeGroupEntry(new GUIContent("Add Block"), 0));

            // Collect all [BlockMenu]-tagged GraphBlock subclasses
            var entries = CollectBlockEntries();

            // Sort to guarantee consistent ordering
            entries.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.Ordinal));

            // Build tree with category groups
            var usedGroups = new HashSet<string>();

            foreach (var (path, type) in entries)
            {
                var parts = path.Split('/');

                // Emit group headers for every path segment except the final leaf
                for (int d = 0; d < parts.Length - 1; d++)
                {
                    string groupKey = string.Join("/", parts, 0, d + 1);
                    if (usedGroups.Add(groupKey))
                    {
                        _cachedTree.Add(new SearchTreeGroupEntry(
                            new GUIContent(parts[d]), d + 1));
                    }
                }

                // Leaf entry — userData holds the Type so we can instantiate it
                var leaf = new SearchTreeEntry(new GUIContent(parts[^1]))
                {
                    level    = parts.Length,
                    userData = type
                };
                _cachedTree.Add(leaf);
            }

            return _cachedTree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is Type blockType)
            {
                try
                {
                    var block = (GraphBlock)Activator.CreateInstance(blockType);

                    // Position the block at the right-click location
                    block.position = new System.Numerics.Vector2(
                        _spawnPosition.x, _spawnPosition.y);

                    _onBlockSelected?.Invoke(block);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BlockSearch] Failed to create block of type {blockType.Name}: {ex}");
                }
            }
            return false;
        }

        // ── Assembly scanner ─────────────────────────────────────────────────────
        private static List<(string path, Type type)> CollectBlockEntries()
        {
            var result = new List<(string, Type)>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Skip Unity engine internals and system assemblies for speed
                var asmName = assembly.GetName().Name;
                if (asmName.StartsWith("System") ||
                    asmName.StartsWith("mscorlib") ||
                    asmName.StartsWith("Unity.") ||
                    asmName.StartsWith("UnityEngine") ||
                    asmName.StartsWith("UnityEditor") ||
                    asmName.StartsWith("Microsoft") ||
                    asmName.StartsWith("Mono."))
                    continue;

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.IsAbstract || type.IsInterface) continue;
                        if (!typeof(GraphBlock).IsAssignableFrom(type)) continue;

                        var attr = type.GetCustomAttribute<BlockMenuAttribute>();
                        if (attr == null) continue;

                        result.Add((attr.Path, type));
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Some assemblies may fail to reflect; skip them gracefully
                }
            }

            return result;
        }
    }
}
