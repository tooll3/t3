#nullable enable
using System.Runtime.CompilerServices;
using T3.Core.Operator;

namespace T3.Editor.UiModel.ProjectHandling;

internal sealed partial class ProjectView
{
    /// <summary>
    /// This class contains logic for the sake of repairing read-only symbols after user modification. It keeps a pool of these objects to avoid garbage,
    /// and a reference count per object, without caching the instance itself for compatibility with runtime compilation.
    ///
    /// In effect, this class only really stores the instance child ID, and its parent symbol Id if it exists.
    /// </summary>
    public sealed class InstanceView : IDisposable
    {
        // runtime-cached fields taken from the instance
        public readonly Guid SymbolChildId;

        // we don't expose these publicly so the Symbol must be fetched, ensuring correctness of any retrieved symbol Id
        private readonly Guid _symbolId;
        private readonly Guid? _parentSymbolId;
        private readonly Guid? _parentSymbolChildId;
        private readonly bool _hasParent;

        // true if the symbol's package is read-only (i.e. not an editable project)
        public bool IsReadOnly { get; }
        public int CheckoutCount { get; private set; }

        public bool HasBeenModified => SymbolUi.HasBeenModified;

        // runtime-computed fields for tracking references of this composition
        private bool _disposed;

        /// <summary>
        /// The symbol Ui of the instance this composition refers to
        /// </summary>
        public SymbolUi SymbolUi
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Symbol.GetSymbolUi();
        }

        /// <summary>
        /// The symbol of the instance this composition refers to
        /// </summary>
        /// <exception cref="Exception"></exception>
        public Symbol Symbol
        {
            get
            {
                if (!SymbolRegistry.TryGetSymbol(_symbolId, out var symbol))
                {
                    throw new Exception($"Could not find symbol with id {_symbolId}");
                }

                return symbol;
            }
        }

        /// <summary>
        /// After recompilation (e.g. after adding a new parameter) the Instance can't be
        /// inferred from the parent because it has been discarded and the parent has no
        /// more instances of self. This would lead to an exception in the Instance property.
        ///
        /// Because we want the Instance to be not-null, testing this with the try catch is
        /// somewhat difficult. This is a work-around, but there are probably better ways to
        /// do that. 
        /// </summary>
        public bool IsValid
        {
            get
            {
                try
                {
                    return Instance != null!;
                }
                catch
                {
                    return false;
                }
            }
        }
        

        /// <summary>
        /// Returns the instance this composition refers to
        /// </summary>
        /// <exception cref="Exception"></exception>
        public Instance Instance
        {
            get
            {
                // if an instance does not have a parent (e.g. the root instance), we cannot get the instance with a simple lookup from the parent
                // as we do below, which we would prefer rather than relying on a cached instance.
                if (!_hasParent)
                {
                    // warning: this avoids caching our instance locally, but we must ensure that no more than one parentless instance of any symbol exists
                    // at any given time. The following function should do so.
                    if (!Symbol.TryGetParentlessInstance(out var instance))
                        throw new Exception("No parentless instance found or created.");

                    return instance;
                }

                if (!SymbolRegistry.TryGetSymbol(_parentSymbolId!.Value, out var parentSymbol))
                {
                    throw new Exception($"Could not find parent symbol with id {_parentSymbolId}");
                }

                if (!parentSymbol.InstancesOfSelf.Any())
                {
                    throw new Exception($"Could not find any instances parent symbol with id {_parentSymbolId}");
                }


                return parentSymbol.InstancesOfSelf // all instances of our parent's symbol
                                   .First(x => x.SymbolChildId == _parentSymbolChildId) // find our specific parent instance
                                   .Children[SymbolChildId]; // find us!
            }
        }

        private InstanceView(Instance instance)
        {
            var parent = instance.Parent;
            if (parent != null)
            {
                _hasParent = true;
                _parentSymbolId = parent.Symbol.Id;
                _parentSymbolChildId = parent.SymbolChildId;
            }

            SymbolChildId = instance.SymbolChildId;
            var symbol = instance.Symbol;
            IsReadOnly = symbol.SymbolPackage.IsReadOnly;
            _symbolId = symbol.Id;
        }

        internal static InstanceView GetForInstance(Instance instance)
        {
            if (instance.IsDisposed)
            {
                throw new InvalidOperationException("Cannot get composition for disposed instance.");
            }

            InstanceView? composition;
            var childId = instance.SymbolChildId;
            var key = new InstanceViewKey(childId, instance.Parent?.Symbol.Id);
            lock (_viewsBySymbolChildId)
            {
                if (!_viewsBySymbolChildId.TryGetValue(key, out composition))
                {
                    composition = new InstanceView(instance);
                    _viewsBySymbolChildId[key] = composition;
                }

                composition.CheckoutCount++;
            }

            return composition;
        }

        public bool Is(Instance newCompositionOp)
        {
            var othersKey = new InstanceViewKey(newCompositionOp.SymbolChildId, newCompositionOp.Parent?.Symbol.Id);
            var myKey = new InstanceViewKey(SymbolChildId, _parentSymbolId);
            return othersKey == myKey;
        }

        public void Dispose()
        {
            CheckoutCount--;
            if (CheckoutCount > 0)
                return;

            if (_disposed)
                throw new Exception("Composition already disposed.");

            _disposed = true;

            lock (_viewsBySymbolChildId)
            {
                var key = new InstanceViewKey(SymbolChildId, _parentSymbolId);
                _viewsBySymbolChildId.Remove(key);
            }
        }

        /// <summary>
        /// A key for looking up the relevant <see cref="InstanceView"/> object
        /// Requires the parent symbol ID as a symbol can have multiple live instances with the same SymbolChildId if they belong to multiple
        /// instances of the same parent
        /// </summary>
        /// <param name="SymbolChildId">The <see cref="Symbol.Child.Id"/> of this instance reference</param>
        /// <param name="ParentSymbolId">The symbol id of the parent, if it exists</param>
        private readonly record struct InstanceViewKey(Guid SymbolChildId, Guid? ParentSymbolId);

        private static readonly Dictionary<InstanceViewKey, InstanceView> _viewsBySymbolChildId = new();
    }
}