using System;
using System.Collections.Generic;

namespace T3.Gui.InputUi
{
    public static class InputUiFactory
    {
        public static Dictionary<Type, Func<IInputUi>> Entries { get; } = new Dictionary<Type, Func<IInputUi>>();
    }
}