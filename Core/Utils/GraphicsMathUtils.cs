using System;
using System.Numerics;

namespace T3.Core.Utils;

public static class Math3DUtils
{
    /// <summary>
    /// System.Numerics implementation of SharpDX's LookAtRH method
    /// </summary>
    public static Matrix4x4 LookAtRH(Vector3 eye, Vector3 target, Vector3 up)
    {
        var zAxis = Vector3.Normalize(eye - target);
        var xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis));
        var yAxis = Vector3.Cross(zAxis, xAxis);

        return new Matrix4x4(
                             xAxis.X, yAxis.X, zAxis.X, 0,
                             xAxis.Y, yAxis.Y, zAxis.Y, 0,
                             xAxis.Z, yAxis.Z, zAxis.Z, 0,
                             -Vector3.Dot(xAxis, eye), -Vector3.Dot(yAxis, eye), -Vector3.Dot(zAxis, eye), 1
                            );
    }

    private const float FovEpsilon = 0.0001f;
    private const float MaxFov = MathF.PI - FovEpsilon;

    public static Matrix4x4 PerspectiveFovRH(float fov, float aspect, float zNear, float zFar)
    {
        fov = MathF.Max(FovEpsilon, MathF.Min(fov, MaxFov));
        aspect = MathF.Max(FovEpsilon, aspect);
        zNear = MathF.Max(FovEpsilon, zNear);
        zFar = MathF.Max(zNear + FovEpsilon, zFar);

        var yScale = 1.0f / MathF.Tan(fov * 0.5f);
        var xScale = yScale / aspect;
        var differenceZNearZFar = zNear - zFar;

        Matrix4x4 result;

        result.M11 = xScale;
        result.M12 = result.M13 = result.M14 = 0.0f;

        result.M22 = yScale;
        result.M21 = result.M23 = result.M24 = 0.0f;

        result.M33 = zFar / differenceZNearZFar;
        result.M31 = result.M32 = 0.0f;
        result.M34 = -1.0f;

        result.M41 = result.M42 = result.M44 = 0.0f;
        result.M43 = zNear * zFar / differenceZNearZFar;

        return result;
    }

    /// <summary>
    /// Gets or sets the translation of the matrix; that is M41, M42, and M43.
    /// </summary>
    public static Vector3 GetTranslationVector(this Matrix4x4 matrix)
    {
        return new Vector3(matrix.M41, matrix.M42, matrix.M43);
    }

    public static void SetTranslationVector(this Matrix4x4 matrix, Vector3 translation)
    {
        matrix.M41 = translation.X;
        matrix.M42 = translation.Y;
        matrix.M43 = translation.Z;
    }

    public static Vector3 GetScaleVector(this Matrix4x4 matrix)
    {
        return new Vector3(matrix.M11, matrix.M22, matrix.M33);
    }

    public static void SetScaleVector(this Matrix4x4 matrix, Vector3 scale)
    {
        matrix.M11 = scale.X;
        matrix.M22 = scale.Y;
        matrix.M33 = scale.Z;
    }

    /// <summary>
    /// Replicates SharpDX's TransformCoordinate function
    /// </summary>
    public static Vector3 TransformCoordinate(Vector3 coordinate, Matrix4x4 transform)
    {
        Vector4 result;
        result.X = (coordinate.X * transform.M11 + coordinate.Y * transform.M21 + coordinate.Z * transform.M31) + transform.M41;
        result.Y = (coordinate.X * transform.M12 + coordinate.Y * transform.M22 + coordinate.Z * transform.M32) + transform.M42;
        result.Z = (coordinate.X * transform.M13 + coordinate.Y * transform.M23 + coordinate.Z * transform.M33) + transform.M43;
        result.W = 1.0f / (coordinate.X * transform.M14 + coordinate.Y * transform.M24 + coordinate.Z * transform.M34 + transform.M44);

        return new Vector3(result.X * result.W, result.Y * result.W, result.Z * result.W);
    }

    public static Matrix4x4 CreateTransformationMatrix(in Vector3 scalingCenter,
                                                       in Quaternion scalingRotation,
                                                       in Vector3 scaling,
                                                       in Vector3 rotationCenter,
                                                       in Quaternion rotation,
                                                       in Vector3 translation)
    {
        // Create rotation matrices from quaternions
        Matrix4x4 scalingRotationMatrix = Matrix4x4.CreateFromQuaternion(scalingRotation);
        Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(rotation);

        // Create inverse rotation matrix from the conjugate of the quaternion
        Matrix4x4 inverseScalingRotationMatrix = Matrix4x4.CreateFromQuaternion(Quaternion.Conjugate(scalingRotation));

        // Create translation matrices for the rotation and scaling centers
        Matrix4x4 scalingCenterTranslation = Matrix4x4.CreateTranslation(-scalingCenter);
        Matrix4x4 rotationCenterTranslation = Matrix4x4.CreateTranslation(-rotationCenter);

        // Create the inverse translations
        Matrix4x4 inverseScalingCenterTranslation = Matrix4x4.CreateTranslation(scalingCenter);
        Matrix4x4 inverseRotationCenterTranslation = Matrix4x4.CreateTranslation(rotationCenter);

        // Create the scaling matrix
        Matrix4x4 scalingMatrix = Matrix4x4.CreateScale(scaling);

        // Create the final translation matrix
        Matrix4x4 finalTranslationMatrix = Matrix4x4.CreateTranslation(translation);

        // Combine the matrices to form the transformation matrix
        Matrix4x4 transformationMatrix =
            scalingCenterTranslation *
            inverseScalingRotationMatrix *
            scalingMatrix *
            scalingRotationMatrix *
            inverseScalingCenterTranslation *
            rotationCenterTranslation *
            rotationMatrix *
            inverseRotationCenterTranslation *
            finalTranslationMatrix;

        return transformationMatrix;
    }
}

public static class VectorT3
{
    public static readonly Vector3 Up = new(0, 1, 0);
    public static readonly Vector3 Down = new(0, -1, 0);
    public static readonly Vector3 Right = new(1, 0, 0);
    public static readonly Vector3 Left = new(-1, 0, 0);
    
    /// <summary>
    /// Represents a vector pointing forwards (0, 0, -1) in a right-handed coordinate system.
    /// </summary>
    public static readonly Vector3 Forward = new(0, 0, -1); 
    public static readonly Vector3 ForwardRH = new(0, 0, -1);
    public static readonly Vector3 ForwardLH = new(0, 0, 1);
    
    /// <summary>
    /// Represents a vector pointing backwards (0, 0, 1) in a right-handed coordinate system.
    /// </summary>
    public static readonly Vector3 Backward = new(0, 0, 1);
    public static readonly Vector3 BackwardRH = new(0, 0, 1);
    public static readonly Vector3 BackwardLH = new(0, 0, -1);
    
    
    public static void Normalize(this ref Vector3 vec) => Vector3.Normalize(vec);
    public static Vector4 ToVector4(this Vector3 vec, float w) => new(vec.X, vec.Y, vec.Z, w);
    public static float Axis(this Vector3 vec, int axis) => axis switch
    {
        0 => vec.X,
        1 => vec.Y,
        2 => vec.Z,
        _ => throw new ArgumentOutOfRangeException(nameof(axis))
    };
}

public static class VectorT4
{
    
}

public struct Ray
{
    public Vector3 Origin { get; set; }
    public Vector3 Direction { get; set; }

    /// <summary>
    /// An alias for <see cref="Origin"/> for those familiar with SharpDX
    /// </summary>
    public Vector3 Position
    {
        get => Origin;
        set => Origin = value;
    }

    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction;
    }

    public bool Intersects(in Plane plane, out float distance)
    {
        // Get the dot product of the direction vector of the ray and the normal of the plane
        float denominator = Vector3.Dot(Direction, plane.Normal);

        // If the dot product is close to zero, the ray is parallel to the plane (no intersection)
        if (Math.Abs(denominator) < float.Epsilon)
        {
            distance = 0;
            return false;
        }

        // Calculate the distance from the ray origin to the plane
        float nominator = Vector3.Dot(Origin - plane.Normal * plane.D, -plane.Normal);
        distance = nominator / denominator;

        // If the distance is less than zero, the plane is behind the ray origin (we consider this as no intersection)
        if (distance < 0)
        {
            distance = 0;
            return false;
        }

        return true;
    }
}

public static class PlaneExtensions
{
    public static bool Intersects(this Plane plane, in Ray ray, out Vector3 point)
    {
        float denominator = Vector3.Dot(plane.Normal, ray.Direction);

        if (Math.Abs(denominator) < float.Epsilon)
        {
            point = default;
            return false;
        }

        float t = (plane.D - Vector3.Dot(plane.Normal, ray.Origin)) / denominator;

        if (t < 0)
        {
            point = default;
            return false;
        }

        point = ray.Origin + t * ray.Direction;
        return true;
    }

    /// <summary>
    /// Creates a plane from a point that lies on the plane and the normal vector.
    /// Implementation basically copied from SharpDX.
    /// </summary>
    /// <param name="point">Any point that lies along the plane.</param>
    /// <param name="normal">The normal vector to the plane.</param>
    public static Plane PlaneFromPointAndNormal(Vector3 point, Vector3 normal)
    {
        var d = -Vector3.Dot(normal, point);
        return new Plane(normal, d);
    }
}

public static class MatrixExtensions
{
    public static bool Invert(this ref Matrix4x4 mat)
    {
        return Matrix4x4.Invert(mat, out mat);
    }

    public static Vector4 Row1(this Matrix4x4 mat)
    {
        return new Vector4(mat.M11, mat.M12, mat.M13, mat.M14);
    }

    public static Vector4 Row2(this Matrix4x4 mat)
    {
        return new Vector4(mat.M21, mat.M22, mat.M23, mat.M24);
    }

    public static Vector4 Row3(this Matrix4x4 mat)
    {
        return new Vector4(mat.M31, mat.M32, mat.M33, mat.M34);
    }

    public static Vector4 Row4(this Matrix4x4 mat)
    {
        return new Vector4(mat.M41, mat.M42, mat.M43, mat.M44);
    }

    /// <summary>
    /// Transposes the specified matrix. -Copied from SharpDX
    /// For use with row-major matrices (like DirectX matrices)
    /// </summary>
    /// <param name="mat"></param>
    public static void Transpose(ref this Matrix4x4 mat)
    {
        Transpose(ref mat, out mat);
    }

    /// <summary>
    /// Calculates the transpose of the specified matrix. -Copied from SharpDX
    /// </summary>
    /// <param name="value">The matrix whose transpose is to be calculated.</param>
    /// <param name="result">When the method completes, contains the transpose of the specified matrix.</param>
    public static void Transpose(ref Matrix4x4 value, out Matrix4x4 result)
    {
        Matrix4x4 temp = new Matrix4x4();
        temp.M11 = value.M11;
        temp.M12 = value.M21;
        temp.M13 = value.M31;
        temp.M14 = value.M41;
        temp.M21 = value.M12;
        temp.M22 = value.M22;
        temp.M23 = value.M32;
        temp.M24 = value.M42;
        temp.M31 = value.M13;
        temp.M32 = value.M23;
        temp.M33 = value.M33;
        temp.M34 = value.M43;
        temp.M41 = value.M14;
        temp.M42 = value.M24;
        temp.M43 = value.M34;
        temp.M44 = value.M44;

        result = temp;
    }
}