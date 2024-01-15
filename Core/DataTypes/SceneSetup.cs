using System;
using System.Collections.Generic;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.Logging;
using T3.Core.Rendering.Material;
using T3.Core.Resource;
using T3.Core.Utils.Geometry;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace T3.Core.DataTypes;

/// <summary>
/// Combines buffers required for mesh rendering
/// </summary>
public class SceneSetup : IEditableInputType
{
    // private List<MeshBuffers> _meshBuffers;
    // private Point[] Positions;
    // private List<PbrMaterial> Materials;

    // private Dictionary<string, string> MaterialAssignments;

    public List<SceneNode> Nodes = new List<SceneNode>();

    /// <summary>
    /// Recursive description of the loaded nodes...
    /// This can be translated into a list of draw dispatches.
    /// </summary>
    /// <remarks>
    /// This closely follows the gltf format structure but should be
    /// agnostic to other multi-node formats like obj.</remarks>
    public class SceneNode
    {
        public string Name;
        public List<SceneNode> ChildNodes = new();
        public Matrix4x4 CombinedTransform;
        public Transform Transform;
        public string MeshName;
    }

    public struct Transform
    {
        public Vector3 Translation;
        public Vector3 RotationYawPitchRoll;
        public Vector3 Scale;

        public Matrix4x4 ToTransform()
        {
            return GraphicsMath.CreateTransformationMatrix(
                                                           scalingCenter: Vector3.Zero,
                                                           scalingRotation: Quaternion.Identity,
                                                           scaling: Scale,
                                                           rotationCenter: Vector3.Zero,
                                                           rotation: Quaternion.CreateFromYawPitchRoll(RotationYawPitchRoll.Y,
                                                                                                       RotationYawPitchRoll.X,
                                                                                                       RotationYawPitchRoll.Z),
                                                           translation: Translation);
        }
    }

    public List<NodeSetting> NodeSettings;

    /// <summary>
    /// Holds settings for a node inside the scene
    /// </summary>
    public struct NodeSetting
    {
        public int NodeHashId;
        public string PbrMaterialId;
        public NodeVisibilities Visibility;

        public enum NodeVisibilities
        {
            Default = 0,
            Visible,
            HiddenSelf,
            HiddenBranch,
        }
    }

    // private List<DrawBatch> _drawBatches;

    // struct DrawBatch
    // {
    //     public PbrMaterial Material;
    //     public MeshBuffers Mesh;
    //     public int StartFaceIndex;
    //     public int FaceCount;
    //     public int[] PointIndices;
    //     public Buffer PointIndexBuffer;
    // }

    #region serialization
    public void Write(JsonTextWriter writer)
    {
        writer.WritePropertyName(nameof(SceneSetup));
        writer.WriteStartObject();

        // writer.WriteObject("Interpolation", Interpolation);
        writer.WritePropertyName("NodeSettings");
        writer.WriteStartArray();

        if (NodeSettings != null)
        {
            lock (NodeSettings)
            {
                foreach (var setting in NodeSettings)
                {
                    writer.WriteStartObject();
                    writer.WriteValue(nameof(setting.NodeHashId), setting.NodeHashId);
                    //writer.WriteObject(nameof(setting.Visibility), (int)setting.Visibility);

                    if (setting.Visibility != NodeSetting.NodeVisibilities.Default)
                        writer.WriteObject(nameof(setting.Visibility), setting.Visibility.ToString());
                    writer.WriteEndObject();
                }
            }
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    public virtual void Read(JToken inputToken)
    {
        if (NodeSettings == null)
        {
            NodeSettings = new List<NodeSetting>();
        }
        else
        {
            NodeSettings.Clear();
        }

        var nodeSettingsToken = inputToken[nameof(SceneSetup)];
        if (nodeSettingsToken == null)
            return;

        try
        {
            var jArray = (JArray)nodeSettingsToken[nameof(NodeSettings)];
            if (jArray == null)
                return;

            foreach (var keyEntry in jArray)
            {
                NodeSettings.Add(new NodeSetting()
                                     {
                                         NodeHashId = (keyEntry[nameof(NodeSetting.NodeHashId)] ?? -1).Value<int>(),
                                         Visibility = (inputToken[nameof(NodeSetting.Visibility)] == null)
                                                          ? NodeSetting.NodeVisibilities.Default
                                                          : (NodeSetting.NodeVisibilities)Enum.Parse(typeof(NodeSetting.NodeVisibilities),
                                                                                                     inputToken[nameof(NodeSetting.Visibility)].ToString()),
                                     });
            }
        }
        catch (Exception e)
        {
            Log.Warning("Can't read scene setting property " + e);
        }
    }

    public object Clone() => TypedClone();

    public SceneSetup TypedClone()
    {
        return new SceneSetup()
                   {
                       NodeSettings = NodeSettings == null ? new List<NodeSetting>() : new List<NodeSetting>(NodeSettings),
                   };
    }
    #endregion
}