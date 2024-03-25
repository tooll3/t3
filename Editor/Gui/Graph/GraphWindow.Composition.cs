using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph;

internal sealed partial class GraphWindow
{
    internal sealed class Composition : IDisposable
    {
        public SymbolUi SymbolUi => _instance.GetSymbolUi();
        public Symbol Symbol => SymbolUi.Symbol;
        public Guid SymbolChildId => _instance.SymbolChildId;
        public Type Type => _instance.Type;
        public Instance Instance => _instance;
        public bool IsDisposed => _disposed;
        public readonly EditorSymbolPackage SymbolPackage;
        private readonly Instance _instance;

        private Composition(Instance instance, EditorSymbolPackage package)
        {
            SymbolPackage = package;
            _instance = instance;
        }

        internal static Composition GetFor(Instance instance, bool allowReload)
        {
            Composition? composition;
            var symbolPackage = (EditorSymbolPackage)instance.Symbol.SymbolPackage;
            var needsReload = allowReload && symbolPackage.IsReadOnly;
            lock (Compositions)
            {
                if (!Compositions.TryGetValue(instance, out composition))
                {
                    composition = new Composition(instance, symbolPackage);
                    Compositions[instance] = composition;
                }
            }

            composition!._needsReload |= needsReload;
            return composition;
        }

        private void ReloadIfNecessary()
        {
            if (_needsReload)
            {
                var symbol = Symbol;
                var symbolUi = SymbolUi;
                Log.Info($"Reloading symbol [{symbol.Name}] ({symbol.Id}).");
                SymbolPackage.Reload(SymbolUi);
                var newSymbol = Symbol;
                var newSymbolUi = SymbolUi;
                
                var sameSymbol = newSymbol == symbol;
                var sameSymbolUi = newSymbolUi == symbolUi;
                
                string msg = $"Reloaded symbol [{newSymbol.Name}] ({newSymbol.Id}). Same symbol? {sameSymbol} | Same symbol ui? {sameSymbolUi}";

                if (!sameSymbol && !sameSymbolUi)
                {
                    Log.Info(msg);
                }
                else
                {
                    throw new Exception(msg);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                throw new Exception("Composition already disposed.");

            _disposed = true;

            lock (Compositions)
            {
                ReloadIfNecessary();
                Compositions.Remove(_instance);
            }
        }

        private bool _disposed;
        private bool _needsReload;

        private static readonly Dictionary<Instance, Composition> Compositions = new();
    }
}