using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Operator;

namespace T3.Core.Extensions
{
    public interface ISymbolExtension
    {
        public void RegisterSymbols(Action<Symbol> registerSymbolAction);
    }
}
