namespace T3.Core.Rendering;

public static class MeshUtils
{
    public static void CalcTBNSpace(Vector3 p0, Vector2 uv0, Vector3 p1, Vector2 uv1, Vector3 p2, Vector2 uv2, Vector3 normal, out Vector3 tangent, out Vector3 bitangent)
    {
        var q1 = p1 - p0;
        var q2 = p2 - p0;
        var st1 = uv1 - uv0;
        var st2 = uv2 - uv0;
        var s1 = st1.X;
        var t1 = st1.Y;
        var s2 = st2.X;
        var t2 = st2.Y;

        var t = new Vector3(q1.X*t2 - q2.X*t1, q1.Y*t2 - q2.Y*t1, q1.Z*t2 - q2.Z*t1)*1.0f/(s1*t2 - s2*t1);
        //var bt = new Vector3(-q1.X*s2 + q2.X*s1, -q1.Y*s2 + q2.Y*s1, -q1.Z*s2 + q2.Z*s1)*1.0f/(s1*t2 - s2*t1);

        bitangent = Vector3.Cross(normal, t);
        bitangent = Vector3.Normalize(bitangent);
        tangent = Vector3.Cross(bitangent, normal);
        tangent = Vector3.Normalize(tangent);
    }
}