#nullable enable

namespace T3.Editor.Gui.Windows;

internal sealed  class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _tail;
    private readonly int _capacity;

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0.", nameof(capacity));

        _capacity = capacity;
        _buffer = new T[capacity];
        _head = 0;
        _tail = 0;
        Count = 0;
    }

    private bool IsEmpty => Count == 0;
    private bool IsFull => Count == _capacity;
    public int Count { get; private set; }

    // Add a value to the buffer (overwrites oldest value when full)
    public void Enqueue(T value)
    {
        if (IsFull)
        {
            // Overwrite the oldest value when the buffer is full
            _head = (_head + 1) % _capacity;  // Move the head forward
        }
        else
        {
            Count++;
        }

        _buffer[_tail] = value;
        _tail = (_tail + 1) % _capacity;
    }

    // Remove a value from the buffer
    public T Dequeue()
    {
        if (IsEmpty)
            throw new InvalidOperationException("Buffer is empty.");

        T value = _buffer[_head];
        _head = (_head + 1) % _capacity;
        Count--;
        return value;
    }

    // Get a span of the FIFO elements
    private Span<T> GetSpan()
    {
        if (Count == 0)
            return Span<T>.Empty;

        // When the buffer is contiguous
        if (_head < _tail)
        {
            return new Span<T>(_buffer, _head, Count);
        }

        // When the buffer wraps around
        int firstPartLength = _capacity - _head;
        if (Count <= firstPartLength)
        {
            return new Span<T>(_buffer, _head, Count);
        }
        else
        {
            // Concatenating parts into a single array since Span cannot span across discontiguous memory
            T[] result = new T[Count];
            Array.Copy(_buffer, _head, result, 0, firstPartLength);
            Array.Copy(_buffer, 0, result, firstPartLength, _tail);
            return new Span<T>(result);
        }
    }
    
    public T[] ToArray()
    {
        T[] result = new T[Count];
        if (Count == 0)
            return result;

        if (_head < _tail)
        {
            // Single contiguous block
            Array.Copy(_buffer, _head, result, 0, Count);
        }
        else
        {
            // Two parts: from head to end of buffer, and from start to tail
            int firstPartLength = _capacity - _head;
            Array.Copy(_buffer, _head, result, 0, firstPartLength);
            Array.Copy(_buffer, 0, result, firstPartLength, _tail);
        }

        return result;
    }
    
    public void CopyTo(Span<T> destination)
    {
        if (destination.Length < Count)
            throw new ArgumentException("Destination array is too small to hold the buffer contents.", nameof(destination));

        if (Count == 0)
            return;

        if (_head < _tail)
        {
            // Single contiguous block
            _buffer.AsSpan(_head, Count).CopyTo(destination);
        }
        else
        {
            // Two parts: from head to end of buffer, and from start to tail
            int firstPartLength = _capacity - _head;
            _buffer.AsSpan(_head, firstPartLength).CopyTo(destination);
            _buffer.AsSpan(0, _tail).CopyTo(destination[firstPartLength..]);
        }
    }

    // Optional: Convert span to a list if needed
    public List<T> GetList()
    {
        return GetSpan().ToArray().ToList();
    }
}