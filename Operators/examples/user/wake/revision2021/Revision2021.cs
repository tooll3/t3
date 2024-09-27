namespace examples.user.wake.revision2021;

[Guid("93184ac2-d545-4611-a941-d5e8769c999b")]
public class Revision2021 : Instance<Revision2021>
{
    [Output(Guid = "c5060347-e04a-493a-9b24-1d5c08baaded")]
    public readonly Slot<Texture2D> TextureOutput = new();


}