namespace T3.Editor.Gui.Windows;

internal sealed  class CircularBuffer<T>
{
    private readonly T[] _buffer;
    private int _head;
    private int _tail;
    private int _size;
    private readonly int _capacity;

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0.", nameof(capacity));

        _capacity = capacity;
        _buffer = new T[capacity];
        _head = 0;
        _tail = 0;
        _size = 0;
    }

    public bool IsEmpty => _size == 0;
    public bool IsFull => _size == _capacity;
    public int Count => _size;

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
            _size++;
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
        _size--;
        return value;
    }

    // Get a span of the FIFO elements
    public Span<T> GetSpan()
    {
        if (_size == 0)
            return Span<T>.Empty;

        // When the buffer is contiguous
        if (_head < _tail)
        {
            return new Span<T>(_buffer, _head, _size);
        }

        // When the buffer wraps around
        int firstPartLength = _capacity - _head;
        if (_size <= firstPartLength)
        {
            return new Span<T>(_buffer, _head, _size);
        }
        else
        {
            // Concatenating parts into a single array since Span cannot span across discontiguous memory
            T[] result = new T[_size];
            Array.Copy(_buffer, _head, result, 0, firstPartLength);
            Array.Copy(_buffer, 0, result, firstPartLength, _tail);
            return new Span<T>(result);
        }
    }
    
    public T[] ToArray()
    {
        T[] result = new T[_size];
        if (_size == 0)
            return result;

        if (_head < _tail)
        {
            // Single contiguous block
            Array.Copy(_buffer, _head, result, 0, _size);
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

    // Optional: Convert span to a list if needed
    public List<T> GetList()
    {
        return GetSpan().ToArray().ToList();
    }
}