using System;
using System.Collections.Generic;
using ImGuiNET;
using T3.Core.Operator;
using UiHelpers;

namespace T3.Gui.ChildUi
{
    public static class CustomChildUiRegistry
    {
        public static Dictionary<Type, Func<Instance, ImDrawListPtr, ImRect, bool>> Entries { get; } = new Dictionary<Type, Func<Instance, ImDrawListPtr, ImRect, bool>>();
    }
}