using System.Runtime.InteropServices;
using SharpDX;

[StructLayout(LayoutKind.Explicit, Size = 4 * 4 * 4 * 10)]
public struct TransformBufferLayout
{
    public TransformBufferLayout(Matrix cameraToClipSpace, Matrix worldToCamera, Matrix objectToWorld)
    {
        Matrix clipSpaceToCamera = cameraToClipSpace;
        clipSpaceToCamera.Invert();
        Matrix cameraToWorld = worldToCamera;
        cameraToWorld.Invert();
        Matrix worldToObject = objectToWorld;
        worldToObject.Invert();

        CameraToClipSpace = cameraToClipSpace;
        ClipSpaceToCamera = clipSpaceToCamera;
        WorldToCamera = worldToCamera;
        CameraToWorld = cameraToWorld;
        WorldToClipSpace = Matrix.Multiply(worldToCamera, cameraToClipSpace);
        ClipSpaceToWorld = Matrix.Multiply(clipSpaceToCamera, cameraToWorld);
        ObjectToWorld = objectToWorld;
        WorldToObject = worldToObject;
        ObjectToCamera = Matrix.Multiply(objectToWorld, worldToCamera);
        ObjectToClipSpace = Matrix.Multiply(ObjectToCamera, cameraToClipSpace);

        // transpose all as mem layout in hlsl constant buffer is row based
        CameraToClipSpace.Transpose();
        ClipSpaceToCamera.Transpose();
        WorldToCamera.Transpose();
        CameraToWorld.Transpose();
        WorldToClipSpace.Transpose();
        ClipSpaceToWorld.Transpose();
        ObjectToWorld.Transpose();
        WorldToObject.Transpose();
        ObjectToCamera.Transpose();
        ObjectToClipSpace.Transpose();
    }

    [FieldOffset(0)]
    public Matrix CameraToClipSpace;

    [FieldOffset(64)]
    public Matrix ClipSpaceToCamera;

    [FieldOffset(128)]
    public Matrix WorldToCamera;

    [FieldOffset(192)]
    public Matrix CameraToWorld;

    [FieldOffset(256)]
    public Matrix WorldToClipSpace;

    [FieldOffset(320)]
    public Matrix ClipSpaceToWorld;

    [FieldOffset(384)]
    public Matrix ObjectToWorld;

    [FieldOffset(448)]
    public Matrix WorldToObject;

    [FieldOffset(512)]
    public Matrix ObjectToCamera;

    [FieldOffset(576)]
    public Matrix ObjectToClipSpace;
}