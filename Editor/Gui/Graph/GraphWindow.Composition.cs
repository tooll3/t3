#nullable enable
using T3.Core.Operator;
using T3.Editor.UiModel;

namespace T3.Editor.Gui.Graph;

internal sealed partial class GraphWindow
{
    internal sealed class Composition : IDisposable
    {
        public SymbolUi SymbolUi => _symbolPackage.SymbolUis[_symbolId];
        public Symbol Symbol => _symbolPackage.Symbols[_symbolId];
        public Instance Instance => _hasParent ? _parent!.Children[SymbolChildId] : _instance;
        
        public readonly Guid SymbolChildId;
        private readonly Guid _symbolId;
        
        private readonly Instance? _parent;
        private readonly Instance _instance;
        private readonly bool _hasParent;
        private readonly EditorSymbolPackage _symbolPackage;

        private Composition(Instance instance)
        {
            _symbolPackage = (EditorSymbolPackage)instance.Symbol.SymbolPackage;
            _parent = instance.Parent;
            _hasParent = _parent != null;
            _instance = instance;
            SymbolChildId = instance.SymbolChildId;
            _symbolId = instance.Symbol.Id;
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
                    composition = new Composition(instance);
                    Compositions[instance] = composition;
                }
            }

            composition._needsReload |= needsReload;
            return composition;
        }

        private void ReloadIfNecessary()
        {
            if (_needsReload)
            {
                _symbolPackage.Reload(SymbolUi);
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