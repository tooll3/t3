using System.IO;
using T3.Core;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_a908cc64_e8cb_490c_ae45_c2c5fbfcedfb
{
    public class GeometryShader : Instance<GeometryShader>
    {
        [Output(Guid = "85B65C27-D5B3-4FE1-88AF-B1F6ABAA4515")]
        public readonly Slot<SharpDX.Direct3D11.GeometryShader> Shader = new Slot<SharpDX.Direct3D11.GeometryShader>();

        private uint _geometryShaderResId;
        public GeometryShader()
        {
            Shader.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();
            
            

            
            if (Source.DirtyFlag.IsDirty || EntryPoint.DirtyFlag.IsDirty || DebugName.DirtyFlag.IsDirty)
            {
                string sourcePath = Source.GetValue(context);
                if (!File.Exists(sourcePath))
                {
                    Log.Warning($"GeometryShader has incorrect or missing path {sourcePath}", SymbolChildId);
                    return;
                }
                
                string entryPoint = EntryPoint.GetValue(context);
                string debugName = DebugName.GetValue(context);
                if (string.IsNullOrEmpty(debugName))
                {
                    try
                    {
                        debugName = new FileInfo(sourcePath).Name;
                    }
                    catch
                    {
                        debugName = "unknownPath";
                    }
                }
                _geometryShaderResId = resourceManager.CreateGeometryShaderFromFile(sourcePath, entryPoint, debugName,
                                                                                () => Shader.DirtyFlag.Invalidate());
            }
            else
            {
                resourceManager.UpdateGeometryShaderFromFile(Source.Value, _geometryShaderResId, ref Shader.Value);
            }
            
            if (_geometryShaderResId != ResourceManager.NullResource)
            {
                Shader.Value = resourceManager.GetGeometryShader(_geometryShaderResId);
            }
        }

        [Input(Guid = "258c53e6-7708-49b7-88e2-1e40d2a4f88d")]
        public readonly InputSlot<string> Source = new InputSlot<string>();

        [Input(Guid = "9675eb2e-ae6a-4826-a53e-07bed7d5b8a0")]
        public readonly InputSlot<string> EntryPoint = new InputSlot<string>();

        [Input(Guid = "08789371-8193-49af-9ef4-97b12d9e6981")]
        public readonly InputSlot<string> DebugName = new InputSlot<string>();
    }
}