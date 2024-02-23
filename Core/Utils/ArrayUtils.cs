namespace T3.Core.Utils;

public class ArrayUtils
{
    public static void Insert<T>(ref T[] array, T value, int index)
    {
        var newArray = new T[array.Length + 1];
        for (var i = 0; i < index; i++)
        {
            newArray[i] = array[i];
        }

        newArray[index] = value;
        for (var i = index + 1; i < array.Length + 1; i++)
        {
            newArray[i] = array[i - 1];
        }

        array = newArray;
    }
    
    public static void RemoveAt<T>(ref T[] array, int index)
    {
        var newArray = new T[array.Length - 1];
        for (var i = 0; i < index; i++)
        {
            newArray[i] = array[i];
        }

        for (var i = index; i < array.Length - 1; i++)
        {
            newArray[i] = array[i + 1];
        }

        array = newArray;
    }
}