using System.Reflection;

namespace T3.Editor.Compilation;

public static class EditorAssemblyInfo
{
    public static readonly Assembly CoreEditor = typeof(EditorAssemblyInfo).Assembly;
}