#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using T3.Core.Logging;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Core.Resource
{

    public sealed class Resource<T> : IDisposable
    {
        public T? Value { get; private set; }
        public event EventHandler<T?>? Changed;
        
        public static implicit operator T?(Resource<T> resource) => resource.Value;

        // todo - should resources be use the same sort of Dirty flag as slots and execute in the same way?
        public Resource(InputSlot<string> slot, TryGenerate<T> tryGenerate, bool allowDisposal = true, EqualityComparer<T?>? comparer = null) 
            : this(slot.Value, slot.Parent, tryGenerate, allowDisposal, comparer)
        {
            slot.UpdateAction = context =>
                                {
                                    var newValue = slot.GetValue(context);
                                    if (newValue == _relativePath)
                                        return;

                                    SubstituteForwardSlashesInSlot(slot);

                                    ReplaceFile(newValue);
                                    return;

                                    static void SubstituteForwardSlashesInSlot(InputSlot<string> slot)
                                    {
                                        // Ensure typed values use forward slashes
                                        // todo: this is a bit of a hack, but it works for now
                                        // basically, if the typed value is being used, this should also be updating "newValue"
                                        var typedValue = slot.TypedInputValue.Value;
                                        if (typedValue == null)
                                            return;

                                        typedValue.ToForwardSlashesUnsafe();
                                    }
                                };
            
            // update slot value when file is moved
            _onFileMoved += (newPath) =>
                              {
                                  if(ResourceManager.TryConvertToRelativePath(newPath, out var relativePath))
                                      newPath = relativePath;
                                  
                                  slot.SetTypedInputValue(newPath.ToForwardSlashes());
                              };
        }

        internal Resource(string? relativePath, IResourceConsumer? owner,
                          TryGenerate<T> tryGenerate, bool allowDisposal = true,
                          EqualityComparer<T?>? equalityComparer = null) 
        {
            ArgumentNullException.ThrowIfNull(tryGenerate, nameof(tryGenerate));

            _tryGenerate = tryGenerate;
            _equalityComparer = equalityComparer ?? EqualityComparer<T?>.Default;
            _onFileChanged += OnFileUpdate;
            _owner = owner;
            _allowDisposal = allowDisposal;
            
            if(owner != null)
                owner.Disposing += Dispose;

            ReplaceFile(relativePath);
            
            if (_fileResource != null)
                OnFileUpdate(_fileResource, WatcherChangeTypes.Created);
            
        }

        private void ReplaceFile(string? relativePath)
        {
            ReleaseFileResource();
            _relativePath = relativePath;

            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            if (FileResource.TryGetFileResource(relativePath, _owner, out var fileResource))
            {
                fileResource.Claim(this);
                _fileResource = fileResource;
                fileResource.FileChanged += _onFileChanged;
            }
        }

        private void ReleaseFileResource()
        {
            if (_fileResource == null) return;
            
            _fileResource.FileChanged -= _onFileChanged;
            _fileResource.Release(this);
            _fileResource = null;
        }

        private void OnFileUpdate(object? sender, WatcherChangeTypes changeTypes)
        {
            var hasMoved = changeTypes.WasMoved();
            var fileResource = _fileResource!;
            var deleted = changeTypes.WasDeleted() || fileResource.FileInfo?.Exists == false;
            T? newValue;

            if (hasMoved && _onFileMoved != null)
            {
                _onFileMoved.Invoke(fileResource.AbsolutePath);
                return;
            }

            if (deleted && !hasMoved)
            {
                newValue = default;
                _hasValue = false;
            }
            else if (!_tryGenerate.Invoke(fileResource, Value, out newValue, out var failureReason))
            {
                _hasValue = false;
                var errorLog = $"Failed to generate {typeof(T)} from file '{fileResource.AbsolutePath}': {failureReason}";
                
                if(_owner != null)
                    Log.Error(errorLog, _owner);
                else
                    Log.Error(errorLog);
            }
            else
            {
                _hasValue = true;
            }

            if (_equalityComparer.Equals(Value, newValue))
                return;

            // if the value has changed, notify listeners and dispose of the old value
            DisposeValue();
            Value = newValue;
            Changed?.Invoke(this, Value);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            ReleaseFileResource();

            if (DisposeValue())
                Changed?.Invoke(this, default);
            
            if(_owner != null)
                _owner.Disposing -= Dispose;

            Changed = null;
        }

        /// <summary>
        /// Returns true if item was disposed and changed
        /// </summary>
        /// <returns></returns>
        private bool DisposeValue()
        {
            if(!_allowDisposal || !IsDisposableType)
                return false;
            
            var notNull = !_equalityComparer.Equals(Value, default);
            if (notNull)
            {
                ((IDisposable)Value!).Dispose();
            }

            Value = default;
            _hasValue = false;
            return notNull;
        }

        private static readonly bool IsDisposableType = typeof(IDisposable).IsAssignableFrom(typeof(T));
        private readonly TryGenerate<T> _tryGenerate;
        private readonly EqualityComparer<T?> _equalityComparer;

        private FileResource? _fileResource;
        private readonly IResourceConsumer? _owner;

        private readonly EventHandler<WatcherChangeTypes> _onFileChanged;
        private readonly Action<string>? _onFileMoved;
        private string? _relativePath;

        private bool _disposed;
        private bool _allowDisposal;
        private bool _hasValue;

        public void InvokeChangeEvent()
        {
            OnFileUpdate(_owner, WatcherChangeTypes.Changed);
        }

        public bool TryGetValue([NotNullWhen(true)] out T? value)
        {
            if (!_hasValue)
            {
                value = default;
                return false;
            }
            
            value = Value!;
            return true;
        }
    }

    public delegate bool TryGenerate<T>(FileResource file, 
                                        T? currentValue, 
                                        [NotNullWhen(true)] out T? newValue, 
                                        [NotNullWhen(false)] out string? failureReason);
}