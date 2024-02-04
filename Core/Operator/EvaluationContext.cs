using System.Collections.Generic;
using System.Numerics;
using SharpDX.Direct3D11;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.DataTypes.Vector;
using T3.Core.Operator.Interfaces;
using T3.Core.Rendering;
using T3.Core.Rendering.Material;
using T3.Core.Utils;
using T3.Core.Utils.Geometry;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Core.Operator
{
    public enum GizmoVisibility
    {
        Inherit = -1,
        Off = 0,
        On = 1,
        IfSelected = 2,
    }

    public class EvaluationContext
    {
        public EvaluationContext()
        {
            Reset();
        }
        
        public void Reset()
        {
            // TODO: this should be replaced with a solution that supports multiple playback sources 
            Playback = Playback.Current;
            
            LocalTime = Playback.TimeInBars;
            LocalFxTime = Playback.FxTimeInBars;
            PointLights.Clear();
            
            
            PbrContextSettings.SetDefaultToContext(this);
        }

        public void SetViewFromCamera(ICamera camera)
        {
            var fov = GraphicsMath.DefaultCamFovDegrees.ToRadians();
            var aspectRatio = (float)RequestedResolution.Width / RequestedResolution.Height;
            CameraToClipSpace = GraphicsMath.PerspectiveFovRH(fov, aspectRatio, 0.01f, 1000);

            Vector3 eye = new Vector3(camera.CameraPosition.X, camera.CameraPosition.Y, camera.CameraPosition.Z);
            Vector3 target = new Vector3(camera.CameraTarget.X, camera.CameraTarget.Y, camera.CameraTarget.Z);
            Vector3 up = VectorT3.Up;
            WorldToCamera = GraphicsMath.LookAtRH(eye, target, up);

            ObjectToWorld = Matrix4x4.Identity;
        }

        public void SetDefaultCamera()
        {
            ObjectToWorld = Matrix4x4.Identity;
            WorldToCamera = GraphicsMath.LookAtRH(new Vector3(0, 0, GraphicsMath.DefaultCameraDistance), Vector3.Zero, VectorT3.Up);
            var fov =  GraphicsMath.DefaultCamFovDegrees.ToRadians();
            float aspectRatio = (float)RequestedResolution.Width / RequestedResolution.Height;
            CameraToClipSpace = GraphicsMath.PerspectiveFovRH(fov, aspectRatio, 0.01f, 1000);
        }

        private static ICamera _defaultCamera = new ViewCamera();

        
        #region timing
        public Playback Playback { get; private set; }

        /// <summary>
        /// The primary time used for user interactions and keyframe manipulation.
        /// This is where there time marker in the timeline is displayed unless overridden by operators.
        ///
        /// While evaluating the graph it can be overridden for sub graphs by <see cref="SetCommandTime"/>.
        /// </summary>
        /// <remarks>Also see <see cref="EvaluationContext"/>.<see cref="GlobalTimeForEffects"/> and .<see cref="GlobalTimeInSecs"/></remarks>
        public double LocalTime { get; set; }
        
        /// <summary>
        /// Although similar to KeyframeTime, this one keeps running in pause mode, if Keep Running is active.
        /// While evaluating the graph it can be overridden for sub graphs by <see cref="SetCommandTime"/>.
        /// </summary>
        public double LocalFxTime { get; set; }
        
        #endregion
        
        public Int2 RequestedResolution { get; set; }

        public Matrix4x4 CameraToClipSpace { get; set; } = Matrix4x4.Identity;
        public Matrix4x4 WorldToCamera { get; set; } = Matrix4x4.Identity;
        public Matrix4x4 ObjectToWorld { get; set; } = Matrix4x4.Identity;
        
        
        // Render settings
        public Buffer FogParameters { get; set; } = FogSettings.DefaultSettingsBuffer;
        
        //public PbrMaterialTextures PbrMaterialTextures { get; set; } = new();
        public PbrMaterial PbrMaterial { get; set; }
        public List<PbrMaterial> Materials { get; set; } = new(8);
        
        /// <summary>
        /// A structure that is used by SetTexture  
        /// </summary>
        public Dictionary<string, Texture2D> ContextTextures { get; set; } = new(10);
        // public Texture2D PrbPrefilteredSpecular { get; set; }
        public PointLightStack PointLights { get; } = new();
        
        /// <summary>
        /// This should be set by RenderTargets and other ops can could be directly used by SetFog.
        /// </summary>
        public System.Numerics.Vector4 BackgroundColor { get; set; } = new(0.1f, 0.1f, 0.1f, 1.0f);
        
        /// <summary>
        /// Can be set by [SetMaterial] [Group] and other ops to fade out groups  
        /// </summary>
        public System.Numerics.Vector4 ForegroundColor { get; set; } = Vector4.One;
        
        public GizmoVisibility ShowGizmos { get; set; }

        public Dictionary<string, float> FloatVariables { get; } = new();
        public Dictionary<string, int> IntVariables { get; } = new();
        public StructuredList IteratedList { get; set; }
        public int IteratedListIndex { get; set; }
        public bool BypassCameras { get; set; }

        public LegacyParticleSystem LegacyParticleSystem;
        public ParticleSystem ParticleSystem;
    }
}