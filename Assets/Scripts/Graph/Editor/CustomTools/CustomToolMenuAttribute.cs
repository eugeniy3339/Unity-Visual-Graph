using System;

namespace GraphEditor.CustomTools
{
    /// <summary>
    /// Registers a custom tool in the Graph Editor's "Custom Tools" toolbar dropdown.
    /// Can be used as [CustomToolMenu("Path/To/Tool")] or [CustomToolMenuAttribute("Path/To/Tool")].
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CustomToolMenuAttribute : Attribute
    {
        public string Path { get; }

        public CustomToolMenuAttribute(string path)
        {
            Path = path;
        }
    }
}
