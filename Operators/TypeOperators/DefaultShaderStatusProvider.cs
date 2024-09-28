#nullable enable
namespace Types;

public class DefaultShaderStatusProvider : IStatusProvider
{
    public string? Warning { private get; set; }
    public IStatusProvider.StatusLevel GetStatusLevel()
    {
        return string.IsNullOrWhiteSpace(Warning) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
    }

    public string GetStatusMessage() => Warning ?? string.Empty;
}