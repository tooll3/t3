using T3.Core.Rendering;
using T3.Core.Utils;
using T3.Core.Utils.Geometry;


namespace Lib.render.shading.@_;

[Guid("5b538cf5-e3b6-4674-b23e-ab55fc59ada6")]
internal sealed class GetCamProperties2 : Instance<GetCamProperties2>
{
    [Output(Guid = "013B08CB-AF63-4FAC-BA28-DE5D1F5A869C")]
    public readonly Slot<Vector3> Position = new();

    [Output(Guid = "F9A31409-323C-43C8-B850-624050EA229E")]
    public readonly Slot<Vector4[]> CamToWorldRows = new();

    [Output(Guid = "40BD0840-10AD-46CD-B8E7-0BAD72222C32")]
    public readonly Slot<Vector4[]> WorldToClipSpaceRows = new();

    [Output(Guid = "0FDF4500-9582-49A5-B383-6ECAE14D8DD5")]
    public readonly Slot<int> CameraCount = new();

    public GetCamProperties2()
    {
        CameraCount.UpdateAction += Update;
        Position.UpdateAction += Update;
        CamToWorldRows.UpdateAction += Update;
        WorldToClipSpaceRows.UpdateAction += Update;
    }

    private List<ICameraPropertiesProvider> _cameraInstances = new();

    private void Update(EvaluationContext context)
    {
        try
        {
            _cameraInstances.Clear();
            foreach (var child in Parent.Parent.Children.Values)
            {
                if (child is not ICameraPropertiesProvider camera)
                    continue;

                _cameraInstances.Add(camera);
            }
        }
        catch (Exception e)
        {
            Log.Warning("Failed to access cameras: " + e.Message);
            return;
        }

        CameraCount.Value = _cameraInstances.Count;

        var index = CameraIndex.GetValue(context).Clamp(0, 10000);

        if (_cameraInstances.Count == 0)
        {
            Log.Debug("No cameras found", this);
            return;
        }

        var cam = _cameraInstances[index.Mod(_cameraInstances.Count)];

        if (cam is not ICameraPropertiesProvider camInstance)
        {
            Log.Warning($"Camera #{index}/{_cameraInstances.Count} is not a Camera", this);
            return;
        }

        Matrix4x4.Invert(cam.WorldToCamera, out var camToWorld);

        var pos = new Vector3(camToWorld.M41, camToWorld.M42, camToWorld.M43);
        Position.Value = pos;

        CamToWorldRows.Value = new[]
                                   {
                                       camToWorld.Row1(),
                                       camToWorld.Row2(),
                                       camToWorld.Row3(),
                                       camToWorld.Row4(),
                                   };
        WorldToClipSpaceRows.Value = new[]
                                         {
                                             cam.CameraToClipSpace.Row1(),
                                             cam.CameraToClipSpace.Row2(),
                                             cam.CameraToClipSpace.Row3(),
                                             cam.CameraToClipSpace.Row4(),
                                         };

        // Prevent double evaluation when accessing multiple outputs
        CameraCount.DirtyFlag.Clear();
        Position.DirtyFlag.Clear();
        CamToWorldRows.DirtyFlag.Clear();
        WorldToClipSpaceRows.DirtyFlag.Clear();
    }

    [Input(Guid = "F7D2B9BC-4D01-4E3B-91ED-4E41FF387196")]
    public readonly InputSlot<int> CameraIndex = new();
}