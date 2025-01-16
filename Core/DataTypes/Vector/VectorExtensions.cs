namespace T3.Core.DataTypes.Vector;

public static class VectorExtensions
{
    static Color NanToZero(this Color color) => new(color.R.NanToZero(), color.G.NanToZero(), color.B.NanToZero(), color.A.NanToZero());

    public static Vector4 NanToZero(this Vector4 vector) => new(vector.X.NanToZero(), vector.Y.NanToZero(), vector.Z.NanToZero(), vector.W.NanToZero());

    public static Vector3 NanToZero(this Vector3 vector) => new(vector.X.NanToZero(), vector.Y.NanToZero(), vector.Z.NanToZero());

    public static Vector2 NanToZero(this Vector2 vector) => new(vector.X.NanToZero(), vector.Y.NanToZero());

    public static float NanToZero(this float value) => float.IsNaN(value) ? 0 : value;
}