#nullable enable
using T3.Core.Operator.Interfaces;

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