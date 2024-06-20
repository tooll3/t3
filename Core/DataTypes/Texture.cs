using System;
using SharpDX.Direct3D11;

namespace T3.Core.DataTypes;

public sealed class Texture2D(SharpDX.Direct3D11.Texture2D texture) : Texture<SharpDX.Direct3D11.Texture2D>(texture)
{
    public override string Name { get => TextureObject.DebugName; set => TextureObject.DebugName = value; }
    public readonly Texture2DDescription Description = texture.Description;
}
public sealed class Texture3D(SharpDX.Direct3D11.Texture3D texture) : Texture<SharpDX.Direct3D11.Texture3D>(texture)
{
    public override string Name { get => TextureObject.DebugName; set => TextureObject.DebugName = value; }
    public readonly Texture3DDescription Description = texture.Description;
}

public abstract class Texture<T>(T texture) : AbstractTexture(texture)
    where T : SharpDX.Direct3D11.Resource
{
    public static implicit operator T(Texture<T> texture) => texture.TextureObject;
    public static implicit operator SharpDX.Direct3D11.Resource(Texture<T> texture) => texture.TextureObject;
    protected readonly T TextureObject = texture;
    public bool IsDisposed => TextureObject.IsDisposed;
}

public abstract class AbstractTexture(IDisposable disposable) : IDisposable
{
    private IDisposable _disposable = disposable;
    public abstract string Name { get; set; }
    
    public static implicit operator SharpDX.Direct3D11.Resource(AbstractTexture texture) => (SharpDX.Direct3D11.Resource)texture._disposable;

    public void Dispose()
    {
        _disposable?.Dispose();
        _disposable = null;
        GC.SuppressFinalize(this);
    }
    
    ~AbstractTexture()
    {
        Dispose();
    }
}