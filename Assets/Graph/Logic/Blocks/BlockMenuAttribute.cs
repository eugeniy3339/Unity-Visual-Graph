using System;

namespace Graph.Logic
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class BlockMenuAttribute : Attribute
    {
        public string Path { get; }

        public BlockMenuAttribute(string path) => Path = path;
    }
}
