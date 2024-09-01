#nullable enable
using System;

namespace T3.Core.Compilation;

public readonly record struct ExtractableTypeInfo(bool IsExtractable, Type? ExtractableType);