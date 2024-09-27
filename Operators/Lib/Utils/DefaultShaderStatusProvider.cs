#nullable enable
namespace lib.Utils;

public class DefaultShaderStatusProvider : IStatusProvider
{
    public string? Warning { private get; set; }
    public IStatusProvider.StatusLevel GetStatusLevel()
    {
        return string.IsNullOrWhiteSpace(Warning) ? IStatusProvider.StatusLevel.Success : IStatusProvider.StatusLevel.Warning;
    }

    public string GetStatusMessage() => Warning ?? string.Empty;
}