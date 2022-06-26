using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T3.Core.Extensions
{
    public interface ITypeExtension
    {
        void RegisterTypes(Action<TypeDescription> registerAction);
        void RegisterPersistedTypes(Action<PersistedTypeDescription> registerAction);
    }
}
