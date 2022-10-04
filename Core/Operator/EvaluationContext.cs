using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D11;
using T3.Core.Animation;
using T3.Core.DataTypes;
using T3.Core.Operator.Interfaces;
using T3.Core.Rendering;
using Vector3 = SharpDX.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace T3.Core.Operator
{
    public enum GizmoVisibility
    {
        Inherit = -1,
        Off = 0,
        On = 1,
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
            var fov = MathUtil.DegreesToRadians(45);
            var aspectRatio = (float)RequestedResolution.Width / RequestedResolution.Height;
            CameraToClipSpace = Matrix.PerspectiveFovRH(fov, aspectRatio, 0.01f, 1000);

            Vector3 eye = new Vector3(camera.CameraPosition.X, camera.CameraPosition.Y, camera.CameraPosition.Z);
            Vector3 target = new Vector3(camera.CameraTarget.X, camera.CameraTarget.Y, camera.CameraTarget.Z);
            Vector3 up = Vector3.Up;
            WorldToCamera = Matrix.LookAtRH(eye, target, up);

            ObjectToWorld = Matrix.Identity;
        }

        public void SetDefaultCamera()
        {
            ObjectToWorld = Matrix.Identity;
            WorldToCamera = Matrix.LookAtRH(new Vector3(0, 0, 2.4141f), Vector3.Zero, Vector3.Up);
            var fov = MathUtil.DegreesToRadians(45);
            float aspectRatio = (float)RequestedResolution.Width / RequestedResolution.Height;
            CameraToClipSpace = Matrix.PerspectiveFovRH(fov, aspectRatio, 0.01f, 1000);
        }

        private static ICamera _defaultCamera = new ViewCamera();

        
        #region timing
        public Playback Playback { get; private set; }

        /// <summary>
        /// The primary time used for user interactions and keyframe manipulation.
        /// This is where there time marker in the timeline is displayed.
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
        
        public Size2 RequestedResolution { get; set; }

        public Matrix CameraToClipSpace { get; set; } = Matrix.Identity;
        public Matrix WorldToCamera { get; set; } = Matrix.Identity;
        public Matrix ObjectToWorld { get; set; } = Matrix.Identity;
        
        // Render settings
        public Buffer FogParameters { get; set; } = FogSettings.DefaultSettingsBuffer;
        public Buffer PbrMaterialParams { get; set; }
        public PbrMaterialTextures PbrMaterialTextures { get; set; } = new PbrMaterialTextures();
        
        /// <summary>
        /// A structure that is used by SetTexture  
        /// </summary>
        public Dictionary<string, Texture2D> ContextTextures { get; set; } = new(10);
        public Texture2D PrbPrefilteredSpecular { get; set; }
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

        public ParticleSystem ParticleSystem;
    }

}