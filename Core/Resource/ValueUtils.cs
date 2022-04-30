using System;
using System.Collections.Generic;
using SharpDX;
using T3.Core;
using T3.Core.Operator;
using Quaternion = System.Numerics.Quaternion;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;
// ReSharper disable RedundantNameQualifier

namespace Core.Resource
{
    public static class ValueUtils
    {

        public static readonly Dictionary<Type, Func<InputValue, InputValue, float, InputValue>> BlendMethods =
            new()
                {
                    {
                        typeof(float), (a, b, t) =>
                                       {
                                           if (a is not InputValue<float> aValue || b is not InputValue<float> bValue)
                                               return null;

                                           var r = MathUtils.Lerp(aValue.Value, bValue.Value, t);
                                           return new InputValue<float>(r);
                                       }
                    },
                    {
                        typeof(Vector2), (a, b, t) =>
                                         {
                                             if (a is not InputValue<Vector2> aValue || b is not InputValue<Vector2> bValue)
                                                 return null;

                                             var r = MathUtils.Lerp(aValue.Value, bValue.Value, t);
                                             return new InputValue<Vector2>(r);
                                         }
                    },
                    {
                        typeof(Vector3), (a, b, t) =>
                                         {
                                             if (a is not InputValue<Vector3> aValue || b is not InputValue<Vector3> bValue)
                                                 return null;

                                             var r = MathUtils.Lerp(aValue.Value, bValue.Value, t);
                                             return new InputValue<Vector3>(r);
                                         }
                    },
                    {
                        typeof(Vector4), (a, b, t) =>
                                         {
                                             if (a is not InputValue<Vector4> aValue || b is not InputValue<Vector4> bValue)
                                                 return null;

                                             var r = MathUtils.Lerp(aValue.Value, bValue.Value, t);
                                             return new InputValue<Vector4>(r);
                                         }
                    },

                    {
                        typeof(Quaternion), (a, b, t) =>
                                            {
                                                if (a is not InputValue<Quaternion> aValue || b is not InputValue<Quaternion> bValue)
                                                    return null;

                                                var r = Quaternion.Slerp(aValue.Value, bValue.Value, t);
                                                return new InputValue<Quaternion>(r);
                                            }
                    },
                    {
                        typeof(int), (a, b, t) =>
                                     {
                                         if (a is not InputValue<int> aValue || b is not InputValue<int> bValue)
                                             return null;

                                         var r = MathUtils.Lerp(aValue.Value, bValue.Value, t);
                                         return new InputValue<int>(r);
                                     }
                    },

                    {
                        typeof(SharpDX.Int3), (a, b, t) =>
                                              {
                                                  if (a is not InputValue<SharpDX.Int3> aValue || b is not InputValue<SharpDX.Int3> bValue)
                                                      return null;

                                                  var r = new SharpDX.Int3(MathUtils.Lerp(aValue.Value.X, bValue.Value.X, t),
                                                                           MathUtils.Lerp(aValue.Value.Y, bValue.Value.Y, t),
                                                                           MathUtils.Lerp(aValue.Value.Z, bValue.Value.Z, t)
                                                                          );
                                                  return new InputValue<SharpDX.Int3>(r);
                                              }
                    },

                    {
                        typeof(SharpDX.Size2), (a, b, t) =>
                                               {
                                                   if (a is not InputValue<SharpDX.Size2> aValue || b is not InputValue<SharpDX.Size2> bValue)
                                                       return null;

                                                   var r = new SharpDX.Size2(MathUtils.Lerp(aValue.Value.Width, bValue.Value.Width, t),
                                                                             MathUtils.Lerp(aValue.Value.Height, bValue.Value.Height, t)
                                                                            );
                                                   return new InputValue<SharpDX.Size2>(r);
                                               }
                    },
                };

        
        
        /// <summary>
        /// A set of functions that mix n values with blend factors
        /// </summary>
        /// <remarks>
        /// Note that Quaternions can't be easily weight blended.
        /// </remarks>
        public static readonly Dictionary<Type, Func<InputValue[], float[], InputValue>> WeightedBlendMethods =
            new()
                {
                    {
                        typeof(float), (values, weights) =>
                                       {
                                           var sum = 0f;
                                           for (var index = 0; index < values.Length; index++)
                                           {
                                               var inputV = values[index];
                                               if (inputV is not InputValue<float> v)
                                                   continue;
                                               
                                               sum += v.Value * weights[index];
                                           }
                                           return new InputValue<float>(sum);
                                       }
                    },
                    {
                        typeof(Vector2), (values, weights) =>
                                       {
                                           var sum = Vector2.Zero;
                                           for (var index = 0; index < values.Length; index++)
                                           {
                                               var inputV = values[index];
                                               if (inputV is not InputValue<Vector2> v)
                                                   continue;
                                               
                                               sum += v.Value * weights[index];
                                           }
                                           return new InputValue<Vector2>(sum);
                                       }
                    },
                    {
                        typeof(Vector3), (values, weights) =>
                                         {
                                             var sum = Vector3.Zero;
                                             for (var index = 0; index < values.Length; index++)
                                             {
                                                 var inputV = values[index];
                                                 if (inputV is not InputValue<Vector3> v)
                                                     continue;
                                               
                                                 sum += v.Value * weights[index];
                                             }
                                             return new InputValue<Vector3>(sum);
                                         }
                    },                    
                    {
                        typeof(Vector4), (values, weights) =>
                                         {
                                             var sum = Vector4.Zero;
                                             for (var index = 0; index < values.Length; index++)
                                             {
                                                 var inputV = values[index];
                                                 if (inputV is not InputValue<Vector4> v)
                                                     continue;
                                               
                                                 sum += v.Value * weights[index];
                                             }
                                             return new InputValue<Vector4>(sum);
                                         }
                    },                                    

                                        {
                        typeof(int), (values, weights) =>
                                       {
                                           var sum = 0;
                                           for (var index = 0; index < values.Length; index++)
                                           {
                                               var inputV = values[index];
                                               if (inputV is not InputValue<int> v)
                                                   continue;
                                               
                                               sum += (int)(v.Value * weights[index]);
                                           }
                                           return new InputValue<int>(sum);
                                       }
                    },
                    {
                        typeof(SharpDX.Size2), (values, weights) =>
                                       {
                                           var sum = new Size2();
                                           for (var index = 0; index < values.Length; index++)
                                           {
                                               var inputV = values[index];
                                               if (inputV is not InputValue<SharpDX.Size2> v)
                                                   continue;
                                               
                                               sum =  new Size2(sum.Width + (int)(v.Value.Width * weights[index]),
                                                                sum.Height + (int)(v.Value.Height * weights[index]));
                                           }
                                           return new InputValue<SharpDX.Size2>(sum);
                                       }
                    },
                    {
                        typeof(SharpDX.Int3), (values, weights) =>
                                               {
                                                   var sum = new Int3();
                                                   for (var index = 0; index < values.Length; index++)
                                                   {
                                                       var inputV = values[index];
                                                       if (inputV is not InputValue<SharpDX.Int3> v)
                                                           continue;
                                               
                                                       sum =  new Int3(sum.X + (int)(v.Value.X * weights[index]),
                                                                        sum.Y + (int)(v.Value.Y * weights[index]),
                                                                        sum.Z + (int)(v.Value.Z * weights[index])
                                                                        );
                                                   }
                                                   return new InputValue<SharpDX.Int3>(sum);
                                               }
                    },
                };
        
        
        public static readonly Dictionary<Type, Func<InputValue, InputValue, bool>> CompareFunctions =
            new()
                {
                    {
                        typeof(float), (a, b) =>
                                       {
                                           if (a is not InputValue<float> aValue || b is not InputValue<float> bValue)
                                               return false;

                                           return Math.Abs(aValue.Value - bValue.Value) < float.Epsilon;
                                       }
                    },
                    {
                        typeof(Vector2), (a, b) =>
                                         {
                                             if (a is not InputValue<Vector2> aValue || b is not InputValue<Vector2> bValue)
                                                 return false;

                                             return aValue.Value == bValue.Value;
                                         }
                    },
                    {
                        typeof(Vector3), (a, b) =>
                                         {
                                             if (a is not InputValue<Vector3> aValue || b is not InputValue<Vector3> bValue)
                                                 return false;

                                             return aValue.Value == bValue.Value;
                                         }
                    },
                    {
                        typeof(Vector4), (a, b) =>
                                         {
                                             if (a is not InputValue<Vector4> aValue || b is not InputValue<Vector4> bValue)
                                                 return false;

                                             return aValue.Value == bValue.Value;
                                         }
                    },

                    {
                        typeof(Quaternion), (a, b) =>
                                            {
                                                if (a is not InputValue<Quaternion> aValue || b is not InputValue<Quaternion> bValue)
                                                    return false;

                                                return aValue.Value == bValue.Value;
                                            }
                    },
                    {
                        typeof(int), (a, b) =>
                                     {
                                         if (a is not InputValue<int> aValue || b is not InputValue<int> bValue)
                                             return false;

                                         return aValue.Value == bValue.Value;
                                     }
                    },

                    {
                        typeof(SharpDX.Int3), (a, b) =>
                                              {
                                                  if (a is not InputValue<SharpDX.Int3> aValue || b is not InputValue<SharpDX.Int3> bValue)
                                                      return false;

                                                  return aValue.Value == bValue.Value;
                                              }
                    },

                    {
                        typeof(SharpDX.Size2), (a, b) =>
                                               {
                                                   if (a is not InputValue<SharpDX.Size2> aValue || b is not InputValue<SharpDX.Size2> bValue)
                                                       return false;

                                                   return aValue.Value == bValue.Value;
                                               }
                    },
                };        
    }
}