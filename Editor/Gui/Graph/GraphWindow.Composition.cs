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
                var instance = parent!.Children[SymbolChildId];
                
                if (instance == _instance)
                    return instance;
                
                lock (_compositions)
                {
                    _compositions.Remove(_instance, out var thisComposition);
                    System.Diagnostics.Debug.Assert(thisComposition == this);
                    _instance.Disposing -= InstanceOnDisposing;
                    _instance = instance;
                    instance.Disposing += InstanceOnDisposing;
                    _compositions[instance] = this;
                }

                return instance;
            }
        }

        public readonly Guid SymbolChildId;
        private readonly Guid _symbolId;

        private readonly Guid? _parentSymbolId;
        private readonly Guid? _parentSymbolChildId;
        private Instance _instance;
        private readonly bool _hasParent;
        private EditorSymbolPackage _symbolPackage;
        private int _checkoutCount;

        private Composition(Instance instance)
        {
            _instance = instance;
            var symbol = instance.Symbol;
            _symbolPackage = (EditorSymbolPackage)symbol.SymbolPackage;
            if (_symbolPackage is EditableSymbolProject project)
            {
                project.OnSymbolMoved += OnSymbolMoved;
            }
            
            var parent = instance.Parent;
            if (parent != null)
            {
                _hasParent = true;
                _parentSymbolId = parent.Symbol.Id;
                _parentSymbolChildId = parent.SymbolChildId;
            }
            
            SymbolChildId = instance.SymbolChildId;
            _symbolId = symbol.Id;
            _isReadOnly = _symbolPackage.IsReadOnly;
            
            instance.Disposing += InstanceOnDisposing;
        }

        private void OnSymbolMoved(Guid symbolId, EditableSymbolProject? newPackage)
        {
            if(_symbolId != symbolId)
                return;

            if (newPackage != null)
            {
                _symbolPackage = newPackage;
            }
            else
            {
                Log.Error($"Not implemented yet: symbol {symbolId} was deleted.");
            }
        }

        private void InstanceOnDisposing()
        {
            _instance.Disposing -= InstanceOnDisposing;
            // if(_disposed)
            //     return;
            //  Dispose();
        }

        internal static Composition GetFor(Instance instance)
        {
            Composition? composition;
            lock (_compositions)
            {
                if (!_compositions.TryGetValue(instance, out composition))
                {
                    composition = new Composition(instance);
                    _compositions[instance] = composition;
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

            lock (_compositions)
            {
                ReloadIfNecessary();
                _compositions.Remove(_instance);
            }
        }

        private bool _disposed;
        private readonly bool _isReadOnly;

        private static readonly Dictionary<Instance, Composition> _compositions = new();
    }
}