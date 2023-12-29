using System;
using System.Collections.Generic;
using ImGuiNET;
using T3.Core.Operator;
using T3.Editor.Gui.UiHelpers;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.ChildUi
{
    public static class CustomChildUiRegistry
    {
        public static Dictionary<Type, Func<Instance, ImDrawListPtr, ImRect, SymbolChildUi.CustomUiResult>> Entries { get; } = new();
    }

}