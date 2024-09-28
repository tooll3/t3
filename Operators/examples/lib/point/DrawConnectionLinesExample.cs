namespace Examples.lib.point;

[Guid("21198ce9-7ef8-4f5e-a26a-f29b6abbcdec")]
public class DrawConnectionLinesExample : Instance<DrawConnectionLinesExample>
{
    [Output(Guid = "3c42f59b-1595-423b-b605-2c6f24ff6ed5")]
    public readonly Slot<Texture2D> ColorBuffer = new();


}