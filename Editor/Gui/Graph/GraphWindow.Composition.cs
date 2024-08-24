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
        public Instance Instance
        {
            get
            {
                if (!_hasParent)
                    return _instance;

                if (!SymbolRegistry.TryGetSymbol(_parentSymbolId!.Value, out var parentSymbol))
                {
                    throw new Exception($"Could not find parent symbol with id {_parentSymbolId}");
                }
                
                var parent = parentSymbol.InstancesOfSelf.First(x => x.SymbolChildId == _parentSymbolChildId);
                return parent!.Children[SymbolChildId];
            }
        }

        public readonly Guid SymbolChildId;
        private readonly Guid _symbolId;

        private readonly Guid? _parentSymbolId;
        private readonly Guid? _parentSymbolChildId;
        private readonly Instance _instance;
        private readonly bool _hasParent;
        private readonly EditorSymbolPackage _symbolPackage;
        private int _checkoutCount;

        private Composition(Instance instance)
        {
            _symbolPackage = (EditorSymbolPackage)instance.Symbol.SymbolPackage;
            var parent = instance.Parent;
            if (parent != null)
            {
                _hasParent = true;
                _parentSymbolId = parent.Symbol.Id;
                _parentSymbolChildId = parent.SymbolChildId;
            }
            _instance = instance;
            SymbolChildId = instance.SymbolChildId;
            _symbolId = instance.Symbol.Id;
            _isReadOnly = _symbolPackage.IsReadOnly;
        }

        internal static Composition GetFor(Instance instance)
        {
            Composition? composition;
            lock (Compositions)
            {
                if (!Compositions.TryGetValue(instance, out composition))
                {
                    composition = new Composition(instance);
                    Compositions[instance] = composition;
                }
                
                composition._checkoutCount++;
            }

            return composition;
        }


        private void ReloadIfNecessary()
        {
            if (_isReadOnly && SymbolUi.HasBeenModified)
            {
                _symbolPackage.Reload(SymbolUi);
            }
        }
        
        // it must be the last instance checked out, read only, and modified to qualify for reload
        public bool NeedsReload => _checkoutCount == 1 && _isReadOnly && SymbolUi.HasBeenModified;

        public void Dispose()
        {
            _checkoutCount--;
            if(_checkoutCount > 0)
                return;
            
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
        private bool _isReadOnly;

        private static readonly Dictionary<Instance, Composition> Compositions = new();
    }
}