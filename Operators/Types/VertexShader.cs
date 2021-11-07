using System.IO;
using T3.Core;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_646f5988_0a76_4996_a538_ba48054fd0ad
{
    public class VertexShader : Instance<VertexShader>
    {
        [Output(Guid = "ED31838B-14B5-4875-A0FC-DC427E874362")]
        public readonly Slot<SharpDX.Direct3D11.VertexShader> Shader = new Slot<SharpDX.Direct3D11.VertexShader>();

        private uint _vertexShaderResId;
        public VertexShader()
        {
            Shader.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var resourceManager = ResourceManager.Instance();

            if (Source.DirtyFlag.IsDirty || EntryPoint.DirtyFlag.IsDirty || DebugName.DirtyFlag.IsDirty)
            {
                string sourcePath = Source.GetValue(context);
                string entryPoint = EntryPoint.GetValue(context);
                string debugName = DebugName.GetValue(context);
                if (string.IsNullOrEmpty(debugName))
                {
                    debugName = new FileInfo(sourcePath).Name;
                }
                _vertexShaderResId = resourceManager.CreateVertexShaderFromFile(sourcePath, entryPoint, debugName,
                                                                                () => Shader.DirtyFlag.Invalidate());
            }
            else
            {
                resourceManager.UpdateVertexShaderFromFile(Source.Value, _vertexShaderResId, ref Shader.Value);
            }

            if (_vertexShaderResId != ResourceManager.NullResource)
            {
                Shader.Value = resourceManager.GetVertexShader(_vertexShaderResId);
            }
        }

        [Input(Guid = "78FB7501-74D9-4A27-8DB2-596F25482C87")]
        public readonly InputSlot<string> Source = new InputSlot<string>();

        [Input(Guid = "9A8B500E-C3B1-4BE1-8270-202EF3F90793")]
        public readonly InputSlot<string> EntryPoint = new InputSlot<string>();

        [Input(Guid = "C8A59CF8-6612-4D57-BCFD-3AEEA351BA50")]
        public readonly InputSlot<string> DebugName = new InputSlot<string>();
    }
}