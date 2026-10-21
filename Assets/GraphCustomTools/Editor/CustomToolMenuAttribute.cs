using System;
using UnityEngine;

namespace GraphCustomTools.Editor
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CustomToolMenuAttribute : Attribute
    {
        public string Path { get; }

        public CustomToolMenuAttribute(string path) => Path = path;
    }
}
