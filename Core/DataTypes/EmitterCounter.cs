namespace T3.Core.DataTypes;

public static class EmitterCounter
{
    public static int GetId()
    {
        return ++_id;
    }

    private static int _id = 0;
}