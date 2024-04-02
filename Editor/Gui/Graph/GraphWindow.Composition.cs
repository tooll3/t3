#nullable enable
using T3.Core.Operator;
using T3.Editor.SystemUi;
using T3.Editor.UiModel;
using T3.SystemUi;

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
                
                composition.CheckoutCount++;
            }

            composition._needsReload |= needsReload;
            return composition;
        }

        public int CheckoutCount;

        private void ReloadIfNecessary()
        {
            if (_needsReload)
            {
                _symbolPackage.Reload(SymbolUi);
            }
        }

        public bool CheckForDuplicateNeeded()
        {
            if(CheckoutCount > 1)
                return false;
            
            if (_needsDuplicate != null)
                return _needsDuplicate.Value;

            var symbolUi = SymbolUi;
            if (_needsReload && symbolUi.HasBeenModified)
            {
                var result = EditorUi.Instance.ShowMessageBox(text: "You've made changes to a read-only operator. " +
                                                                    "Do you want to save your changes as a new operator?",
                                                              title: $"[{symbolUi.Symbol.Name}] is read-only!",
                                                              buttons: PopUpButtons.YesNo);
                _needsDuplicate = result == PopUpResult.Yes;
            }
            else
            {
                _needsDuplicate = false;
            }

            return _needsDuplicate.Value;
        }

        public void Dispose()
        {
            CheckoutCount--;
            if(CheckoutCount > 0)
                return;
            
            if (_disposed)
                throw new Exception("Composition already disposed.");

            if (_needsDuplicate == true)
                throw new InvalidOperationException("Duplication not completed.");
                                                    
            _disposed = true;

            lock (Compositions)
            {
                ReloadIfNecessary();
                Compositions.Remove(_instance);
            }
        }

        public void MarkDuplicationComplete()
        {
            if(_needsDuplicate is null or false)
                throw new InvalidOperationException("Duplication not needed.");
            
            _needsDuplicate = false;
        }

        private bool _disposed;
        private bool _needsReload;
        private bool? _needsDuplicate;

        private static readonly Dictionary<Instance, Composition> Compositions = new();
    }
}