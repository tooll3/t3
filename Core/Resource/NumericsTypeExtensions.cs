using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using T3.Core.Extensions;
using T3.Core.Operator;
using Newtonsoft.Json.Linq;

namespace T3.Core
{
    public sealed class NumericsTypeExtensions : ITypeExtension
    {
        public void RegisterPersistedTypes(Action<PersistedTypeDescription> registerAction)
        {
            registerAction(new PersistedTypeDescription(typeof(System.Numerics.Vector2), "Vector2",
             () => new InputValue<System.Numerics.Vector2>(),
             (writer, obj) =>
             {
                 var vec = (System.Numerics.Vector2)obj;
                 writer.WriteStartObject();
                 writer.WriteValue("X", vec.X);
                 writer.WriteValue("Y", vec.Y);
                 writer.WriteEndObject();
             },
             jsonToken =>
             {
                 float x = jsonToken["X"].Value<float>();
                 float y = jsonToken["Y"].Value<float>();
                 return new System.Numerics.Vector2(x, y);
             }));

            registerAction(new PersistedTypeDescription(typeof(System.Numerics.Vector3), "Vector3",
                 () => new InputValue<System.Numerics.Vector3>(),
                 (writer, obj) =>
                 {
                     var vec = (System.Numerics.Vector3)obj;
                     writer.WriteStartObject();
                     writer.WriteValue("X", vec.X);
                     writer.WriteValue("Y", vec.Y);
                     writer.WriteValue("Z", vec.Z);
                     writer.WriteEndObject();
                 },
                 jsonToken =>
                 {
                     float x = jsonToken["X"].Value<float>();
                     float y = jsonToken["Y"].Value<float>();
                     float z = jsonToken["Z"].Value<float>();
                     return new System.Numerics.Vector3(x, y, z);
                 }));

            registerAction(new PersistedTypeDescription(typeof(System.Numerics.Vector4), "Vector4",
                 () => new InputValue<System.Numerics.Vector4>(),
                 (writer, obj) =>
                 {
                     var vec = (System.Numerics.Vector4)obj;
                     writer.WriteStartObject();
                     writer.WriteValue("X", vec.X);
                     writer.WriteValue("Y", vec.Y);
                     writer.WriteValue("Z", vec.Z);
                     writer.WriteValue("W", vec.W);
                     writer.WriteEndObject();
                 },
                 jsonToken =>
                 {
                     float x = jsonToken["X"].Value<float>();
                     float y = jsonToken["Y"].Value<float>();
                     float z = jsonToken["Z"].Value<float>();
                     float w = jsonToken["W"].Value<float>();
                     return new System.Numerics.Vector4(x, y, z, w);
                 }));

            registerAction(new PersistedTypeDescription(typeof(System.Numerics.Quaternion), "Quaternion",
                 () => new InputValue<System.Numerics.Quaternion>(),
                 (writer, obj) =>
                 {
                     var vec = (System.Numerics.Quaternion)obj;
                     writer.WriteStartObject();
                     writer.WriteValue("X", vec.X);
                     writer.WriteValue("Y", vec.Y);
                     writer.WriteValue("Z", vec.Z);
                     writer.WriteValue("W", vec.W);
                     writer.WriteEndObject();
                 },
                 jsonToken =>
                 {
                     float x = jsonToken["X"].Value<float>();
                     float y = jsonToken["Y"].Value<float>();
                     float z = jsonToken["Z"].Value<float>();
                     float w = jsonToken["W"].Value<float>();
                     return new System.Numerics.Quaternion(x, y, z, w);
                 }));
        }

        public void RegisterTypes(Action<TypeDescription> registerAction)
        {

        }
    }
}
