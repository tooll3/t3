using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T3.Gui.InputUi;
using T3.Gui.OutputUi;

namespace T3.Gui.Extensions
{
    public struct UiTypeDescription
    {
        public Type Type;
        public ITypeUiProperties UiProperties;
        public Func<IInputUi> InputUi;
        public Func<IOutputUi> OutputUi;

        public UiTypeDescription(Type type, ITypeUiProperties uiProperties, Func<IInputUi> inputUi, Func<IOutputUi> outputUi)
        {
            this.Type = type;
            this.UiProperties = uiProperties;
            this.InputUi = inputUi;
            this.OutputUi = outputUi;
        }
    }
}
