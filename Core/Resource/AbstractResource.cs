#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Slots;
using T3.Core.Utils;

namespace T3.Core.Resource
{
    public sealed class Resource<T> : IDisposable
    {
        public T? Value
        {
            get
            {
                #if DEBUG
                if (_slot is not null && !_hasWarned)
                {
                    Log.Warning($"[{_owner}] It is recommended to use {nameof(GetValue)} instead of the Value property when using a slot." +
                                $" This is because the Value property will not update the slot value when the file is moved. " +
                                $"Slot: {_slot}", _owner!);
                    _hasWarned = true;
                }
                #endif
                return _lazyValue.Value;
            }
        }
        

        public T? GetValue(EvaluationContext context)
        {
            if (_slot == null)
                return _lazyValue.Value;
            
            var newPath = _slot.GetValue(context);

            TryReplaceFile(newPath);
            
            return _lazyValue.Value;

            //SubstituteForwardSlashesInSlot(_slot);
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
        }
        
        internal event EventHandler<T?>? Changed;

        public bool TryGetValue(EvaluationContext context, [NotNullWhen(true)] out T? value)
        {
            value = GetValue(context);
            return !_equalityComparer.Equals(value, default);
        }

        public void MarkFileAsChanged()
        {
            OnFileUpdate(_owner, WatcherChangeTypes.Changed);
        }

        #region Constructors
        public Resource(InputSlot<string> slot, TryGenerate<T> tryGenerate, bool allowDisposal = true, EqualityComparer<T?>? comparer = null)
            : this(slot.Value, slot.Parent, tryGenerate, allowDisposal, comparer)
        {
            _slot = slot;
                                

            // update slot value when file is moved
            _onFileMoved += (newPath) =>
                            {
                                if (ResourceManager.TryConvertToRelativePath(newPath, out var relativePath))
                                    newPath = relativePath;

                                slot.SetTypedInputValue(newPath.ToForwardSlashes());
                            };
        }

        public Resource(string? relativePath, IResourceConsumer? owner,
                        TryGenerate<T> tryGenerate, bool allowDisposal = true,
                        EqualityComparer<T?>? equalityComparer = null)
        {
            ArgumentNullException.ThrowIfNull(tryGenerate, nameof(tryGenerate));

            _tryGenerate = tryGenerate;
            _equalityComparer = equalityComparer ?? EqualityComparer<T?>.Default;
            _owner = owner;
            _allowDisposal = allowDisposal;

            _onFileChanged = OnFileUpdate;
            _valueFactory = ValueFactory;
            _onDispose = Dispose;

            ResetLazyValue(ref _lazyValue);

            if (owner != null)
                owner.Disposing += _onDispose;

            TryReplaceFile(relativePath);
        }
        #endregion

        #region File Handling
        private bool TryReplaceFile(string? relativePath)
        {
            ReleaseFileResource();
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            relativePath = relativePath.ToForwardSlashes();
            _relativePath = relativePath;
            return TryGetFileResource(relativePath);
        }

        private bool TryGetFileResource(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return false;

            if (!FileResource.TryGetFileResource(relativePath, _owner, out var fileResource))
                return false;

            fileResource.Claim(this);
            _fileResource = fileResource;
            fileResource.FileChanged += _onFileChanged;
            return true;
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
            ResetLazyValue(ref _lazyValue);

            if (changeTypes.WasMoved() && _onFileMoved != null)
            {
                _onFileMoved.Invoke(_fileResource!.AbsolutePath);
            }
        }
        #endregion

        #region Value Evaluation
        private void ResetLazyValue([NotNull] ref Lazy<T?>? lazyValue)
        {
            if (lazyValue == null)
            {
                _previouslyHadValue = false;
                lazyValue = new Lazy<T?>(_valueFactory, LazyThreadSafetyMode.None);
                return;
            }

            _previouslyHadValue = lazyValue.IsValueCreated && !_equalityComparer.Equals(lazyValue.Value, default);

            if (!_previouslyHadValue)
                return;

            DisposeValue();
            lazyValue = new Lazy<T?>(_valueFactory, LazyThreadSafetyMode.None);
        }

        private T? ValueFactory()
        {
            if (_currentlyEvaluating)
            {
                throw new
                    InvalidOperationException("Circular dependency detected - do not use the Resource.Value property within the change handler - instead reference the argument passed to the change handler.");
            }

            _currentlyEvaluating = true;

            string? failureReason;
            bool success;
            T? newValue;
            var hasFileResource = _fileResource != null;

            if (!hasFileResource)
            {
                hasFileResource = TryReplaceFile(_relativePath);
            }

            if (hasFileResource && _fileResource!.FileInfo is { Exists : true })
            {
                try
                {
                    // get current value without creating it if it hasn't been created yet
                    var currentValue = _lazyValue.IsValueCreated ? _lazyValue.Value : default;
                    success = _tryGenerate.Invoke(_fileResource, currentValue, out newValue, out failureReason);
                }
                catch (Exception e)
                {
                    failureReason = e.ToString();
                    success = false;
                    newValue = default;
                }
            }
            else
            {
                success = false;
                
                #if !DEBUG
                failureReason = "File not found";
                #else
                failureReason = "File not found:\n" + Environment.StackTrace;
                #endif
                
                newValue = default;
            }

            if (!success)
            {
                var errorLog = $"Failed to generate {typeof(T)} from file {GetPathLog()}: {failureReason ?? "No reason given"}";

                if (_owner != null)
                    Log.Error(errorLog, _owner);
                else
                    Log.Error(errorLog);
            }

            if (success || _previouslyHadValue)
                Changed?.Invoke(this, newValue);

            _currentlyEvaluating = false;
            return newValue;

            string GetPathLog() => $"'{_relativePath}' ({_fileResource?.AbsolutePath ?? "unresolved"})'";
        }
        #endregion

        #region Value Disposal
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            ReleaseFileResource();

            if (DisposeValue())
                Changed?.Invoke(this, default);

            if (_owner != null)
                _owner.Disposing -= _onDispose;

            Changed = null;
        }

        /// <summary>
        /// Returns true if item was disposed and changed
        /// </summary>
        /// <returns></returns>
        private bool DisposeValue()
        {
            if (!_allowDisposal || !IsDisposableType)
                return false;

            if (_lazyValue is not {IsValueCreated: true})
                return false;

            var value = _lazyValue.Value;
            if (_equalityComparer.Equals(value, default))
                return false;

            ((IDisposable)value!).Dispose();
            return true;
        }
        #endregion

        // type handling
        private static readonly bool IsDisposableType = typeof(IDisposable).IsAssignableFrom(typeof(T));
        private readonly EqualityComparer<T?> _equalityComparer;

        // file handling
        private FileResource? _fileResource;
        private readonly EventHandler<WatcherChangeTypes> _onFileChanged;
        private readonly Action<string>? _onFileMoved;
        private readonly IResourceConsumer? _owner;
        private string? _relativePath;
        private readonly InputSlot<string>? _slot;

        // Value handling
        private Lazy<T?> _lazyValue;
        private readonly Func<T?> _valueFactory;
        private bool _currentlyEvaluating;
        private bool _previouslyHadValue;
        private bool _disposed;
        private readonly TryGenerate<T> _tryGenerate;
        private readonly Action _onDispose;
        private readonly bool _allowDisposal;
        
        #if DEBUG
        private bool _hasWarned;
        #endif
    }

    public delegate bool TryGenerate<T>(FileResource file,
                                        T? currentValue,
                                        [NotNullWhen(true)] out T? newValue,
                                        [NotNullWhen(false)] out string? failureReason);
}