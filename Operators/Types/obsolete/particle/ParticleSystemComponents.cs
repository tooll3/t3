using SharpDX.Direct3D11;
using T3.Core.Operator;
using T3.Core.Operator.Attributes;
using T3.Core.Operator.Slots;

namespace T3.Operators.Types.Id_c78a26f9_0c0e_4f1e_a2aa_6ca6136c52d0
{
    public class ParticleSystemComponents : Instance<ParticleSystemComponents>
    {
        [Output(Guid = "b658104b-95ee-4d5a-ab13-159c2a992e22")]
        public readonly Slot<int> MaxParticleCount = new Slot<int>();

        // [Output(Guid = "CD69742E-3581-4600-AEF6-EC908B450C5B")]
        // public readonly Slot<Buffer> ParticleBuffer = new Slot<Buffer>();
        [Output(Guid = "DBF61091-A9A7-4AD2-9DC1-68AFDC3EA9FD")]
        public readonly Slot<UnorderedAccessView> ParticleBufferUav = new Slot<UnorderedAccessView>();
        [Output(Guid = "43E0BC54-373C-4FD0-893B-622EDD293550")]
        public readonly Slot<ShaderResourceView> ParticleBufferSrv = new Slot<ShaderResourceView>();

        // [Output(Guid = "3D803B12-43CE-477B-BB37-EA90DE3BC8C9")]
        // public readonly Slot<Buffer> AliveParticleIndices = new Slot<Buffer>();
        [Output(Guid = "44319FB3-97DA-4B1E-AFCC-13A202D0E082")]
        public readonly Slot<UnorderedAccessView> AliveParticleIndicesUav = new Slot<UnorderedAccessView>();
        [Output(Guid = "0ABDCA40-DB84-4760-A079-94955555B97D")]
        public readonly Slot<ShaderResourceView> AliveParticleIndicesSrv = new Slot<ShaderResourceView>();

        // [Output(Guid = "CF554E22-79B8-4AD4-99D4-27F03155C5F9")]
        // public readonly Slot<Buffer> DeadParticleIndices = new Slot<Buffer>();
        [Output(Guid = "93803CE0-1E4F-4830-BCA9-3CA153999F36")]
        public readonly Slot<UnorderedAccessView> DeadParticleIndicesUav = new Slot<UnorderedAccessView>();

        [Output(Guid = "DBE9606B-AEFE-4C2F-82A2-0AC75D58916B")]
        public readonly Slot<Buffer> IndirectArgsBuffer = new Slot<Buffer>();
        [Output(Guid = "C48F3E70-6668-4965-91FD-044684765382")]
        public readonly Slot<UnorderedAccessView> IndirectArgsBufferUav = new Slot<UnorderedAccessView>();

        [Output(Guid = "C1829F6F-8BE2-4A14-BC9C-5AB88AB3F588")]
        public readonly Slot<Buffer> ParticleCountConstBuffer = new Slot<Buffer>();

        public ParticleSystemComponents()
        {
            MaxParticleCount.UpdateAction = Update;

            // ParticleBuffer.UpdateAction = Update;
            ParticleBufferUav.UpdateAction = Update;
            ParticleBufferSrv.UpdateAction = Update;
            // AliveParticleIndices.UpdateAction = Update;
            AliveParticleIndicesUav.UpdateAction = Update;
            AliveParticleIndicesSrv.UpdateAction = Update;
            // DeadParticleIndices.UpdateAction = Update;
            DeadParticleIndicesUav.UpdateAction = Update;
            IndirectArgsBuffer.UpdateAction = Update;
            IndirectArgsBufferUav.UpdateAction = Update;
            ParticleCountConstBuffer.UpdateAction = Update;
        }

        private void Update(EvaluationContext context)
        {
            var particleSystem = ParticleSystem.GetValue(context);
            if (particleSystem == null)
                return;

            MaxParticleCount.Value = particleSystem.MaxCount;

            // ParticleBuffer.Value = particleSystem.ParticleBuffer;
            ParticleBufferUav.Value = particleSystem.ParticleBufferUav;
            ParticleBufferSrv.Value = particleSystem.ParticleBufferSrv;

            // DeadParticleIndices.Value = particleSystem.DeadParticleIndices;
            DeadParticleIndicesUav.Value = particleSystem.DeadParticleIndicesUav;

            // AliveParticleIndices.Value = particleSystem.AliveParticleIndices;
            AliveParticleIndicesUav.Value = particleSystem.AliveParticleIndicesUav;
            AliveParticleIndicesSrv.Value = particleSystem.AliveParticleIndicesSrv;

            IndirectArgsBuffer.Value = particleSystem.IndirectArgsBuffer;
            IndirectArgsBufferUav.Value = particleSystem.IndirectArgsBufferUav;

            ParticleCountConstBuffer.Value = particleSystem.ParticleCountConstBuffer;

            //Log.Info("particle system components updated");
        }

        [Input(Guid = "E5CEBE45-C1D2-48FA-83AA-E321AEE14912")]
        public readonly InputSlot<T3.Core.DataTypes.ParticleSystem> ParticleSystem = new InputSlot<T3.Core.DataTypes.ParticleSystem>();
    }
}