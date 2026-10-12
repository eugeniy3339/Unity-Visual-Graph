using System;
using System.Collections.Generic;

[Serializable]
public class ExcelTab<T>
{
    public string name;
    public List<T> classes;

    public ExcelTab()
    {
        classes = new List<T>();
    }

    public ExcelTab(string name, List<T> classes)
    {
        this.name = name;
        this.classes = classes;
    }
}
