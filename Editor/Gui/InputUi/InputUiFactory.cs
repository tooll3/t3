using System;
using System.Collections.Generic;
using T3.Editor.Gui.InputUi;

namespace Editor.Gui.InputUi
{
    public static class InputUiFactory
    {
        public static Dictionary<Type, Func<IInputUi>> Entries { get; } = new Dictionary<Type, Func<IInputUi>>();
    }
}