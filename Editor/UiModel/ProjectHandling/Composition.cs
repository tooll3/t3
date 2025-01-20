#nullable enable
using T3.Core.Operator;

namespace T3.Editor.UiModel.ProjectHandling;

/// <todo> Should probably be refactored into (or split into) ProjectManager, Structure, ProjectView, etc </todo>
/// <summary>
/// This class contains logic for the sake of repairing read-only symbols after user modification. It keeps a pool of these objects to avoid garbage,
/// and a reference count per object, without caching the instance itself for compatibility with runtime compilation.
///
/// In effect, this class only really stores the instance child ID, and its parent symbol Id if it exists.
/// </summary>
internal sealed class Composition : IDisposable
{
    // runtime-cached fields taken from the instance
    public readonly Guid SymbolChildId;
    
    // we don't expose these publicly so the Symbol must be fetched, ensuring correctness of any retrieved symbol Id
    private readonly Guid _symbolId;
    private readonly Guid? _parentSymbolId;
    private readonly Guid? _parentSymbolChildId;
    private readonly bool _hasParent;
    
    // true if the symbol's package is read-only (i.e. not an editable project)
    private readonly bool _isReadOnly;
    
    // runtime-computed fields for tracking references of this composition
    private int _checkoutCount;
    private bool _disposed;
    
    /// <summary>
    /// The symbol Ui of the instance this composition refers to
    /// </summary>
    public SymbolUi SymbolUi => Symbol.GetSymbolUi();

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
                if(!Symbol.TryGetParentlessInstance(out var instance))
                    throw new Exception("No parentless instance found or created.");
                
                return instance;
            }

            if (!SymbolRegistry.TryGetSymbol(_parentSymbolId!.Value, out var parentSymbol))
            {
                throw new Exception($"Could not find parent symbol with id {_parentSymbolId}");
            }

            var parent = parentSymbol.InstancesOfSelf.First(x => x.SymbolChildId == _parentSymbolChildId);
            return parent!.Children[SymbolChildId];
        }
    }

    private Composition(Instance instance)
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
        _isReadOnly = symbol.SymbolPackage.IsReadOnly;
        _symbolId = symbol.Id;
    }

    internal static Composition GetForInstance(Instance instance)
    {
        if (instance.IsDisposed)
        {
            throw new InvalidOperationException("Cannot get composition for disposed instance.");
        }
        
        Composition? composition;
        var childId = instance.SymbolChildId;
        var key = new CompositionKey(childId, instance.Parent?.Symbol.Id);
        lock (_compositionsBySymbolChildId)
        {
            if (!_compositionsBySymbolChildId.TryGetValue(key, out composition))
            {
                composition = new Composition(instance);
                _compositionsBySymbolChildId[key] = composition;
            }

            composition._checkoutCount++;
        }

        return composition;
    }

    // It must be the last instance checked out, read only, and modified to qualify for reload
    public bool NeedsReload => _checkoutCount == 0 && _isReadOnly && SymbolUi.HasBeenModified;

    public void Dispose()
    {
        _checkoutCount--;
        if (_checkoutCount > 0)
            return;

        if (_disposed)
            throw new Exception("Composition already disposed.");

        _disposed = true;

        lock (_compositionsBySymbolChildId)
        {
            var key = new CompositionKey(SymbolChildId, _parentSymbolId);
            _compositionsBySymbolChildId.Remove(key);
        }
    }
    
    /// <summary>
    /// A key for looking up the relevant composition object
    /// Requires the parent symbol Id as a symbol can have multiple live instances with the same SymbolChildId if they belong to multiple
    /// instances of the same parent
    /// </summary>
    /// <param name="SymbolChildId">The symbol.child Id of this instance reference</param>
    /// <param name="ParentSymbolId">The symbol id of the parent, if it exists</param>
    private readonly record struct CompositionKey(Guid SymbolChildId, Guid? ParentSymbolId);

    private static readonly Dictionary<CompositionKey, Composition> _compositionsBySymbolChildId = new();

    public bool Is(Instance newCompositionOp)
    {
        var othersKey = new CompositionKey(newCompositionOp.SymbolChildId, newCompositionOp.Parent?.Symbol.Id);
        var myKey = new CompositionKey(SymbolChildId, _parentSymbolId);
        return othersKey == myKey;
    }
}