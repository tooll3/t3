namespace user.pixtur.examples
{
	[Guid("1984a755-39b0-4f0b-9c31-f2b67cab6db1")]
    public class RibbonsOnMeshExample : Instance<RibbonsOnMeshExample>
    {
        [Output(Guid = "b2e25cdf-a743-4e10-bf1f-c9b0f7474e11")]
        public readonly Slot<Texture2D> Output = new();


    }
}

