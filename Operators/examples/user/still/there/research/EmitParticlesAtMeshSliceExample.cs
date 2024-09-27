namespace examples.user.still.there.research
{
	[Guid("ccafae36-6001-4ee8-b0b5-76c1adebcdde")]
    public class EmitParticlesAtMeshSliceExample : Instance<EmitParticlesAtMeshSliceExample>
    {
        [Output(Guid = "c50025cd-95d0-4aa4-b9fe-6088b5c9cda6")]
        public readonly Slot<Texture2D> TextureOutput = new();

        [Output(Guid = "9c7f20a9-5b1a-42c9-ab98-22cc1a9552c9")]
        public readonly Slot<Texture2D> DepthBuffer = new();


    }
}

