using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T3.Gui.Extensions
{
    public interface IUiTypeExtension
    {
        void RegisterUiTypes(Action<UiTypeDescription> registerAction);
    }
}
