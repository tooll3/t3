namespace T3.Editor.Gui.Windows;

internal class CircularBuffer<T>
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

    // Add a value to the buffer
    public void Enqueue(T value)
    {
        if (IsFull)
            throw new InvalidOperationException("Buffer is full.");

        _buffer[_tail] = value;
        _tail = (_tail + 1) % _capacity;
        _size++;
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
        Span<T> span = _size == 0
                           ? Span<T>.Empty
                           : _head < _tail
                               ? new Span<T>(_buffer, _head, _size)
                               : new Span<T>(_buffer, _head, _capacity - _head).Slice(0, _size);

        return span;
    }

    // Optional: Convert span to a list if needed
    public List<T> GetList()
    {
        return GetSpan().ToArray().ToList();
    }
}