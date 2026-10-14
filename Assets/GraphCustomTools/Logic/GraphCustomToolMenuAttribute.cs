using System;
using UnityEngine;

namespace GraphCustomTools.Logic
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class GraphCustomToolMenuAttribute : Attribute
    {
        public string Path { get; }

        public GraphCustomToolMenuAttribute(string path) => Path = path;
    }
}
