using System.Diagnostics;
using T3.Core.DataTypes.ShaderGraph;
using T3.Core.Utils;

// ReSharper disable UnusedMember.Local

namespace Lib.field.adjust;

[Guid("82270977-07b5-4d86-8544-5aebc638d46c")]
internal sealed class CombineFields : Instance<CombineFields>, IGraphNodeOp
{
    [Output(Guid = "db0bbde0-18b6-4c53-8cf7-a294177d2089")]
    public readonly Slot<ShaderGraphNode> Result = new();

    public CombineFields()
    {
        ShaderNode = new ShaderGraphNode(this, InputFields);
        Result.UpdateAction += Update;
        Result.Value = ShaderNode;
    }

    private void Update(EvaluationContext context)
    {
        var combineMethod = CombineMethod.GetEnumValue<CombineMethods>(context);
        if (combineMethod != _combineMethod)
        {
            _combineMethod = combineMethod;
            ShaderNode.FlagCodeChanged();
        }

        ShaderNode.Update(context);

        // Get all parameters to clear operator dirty flag
        InputFields.DirtyFlag.Clear();
    }

    public void GetPreShaderCode(CodeAssembleContext cac, int inputIndex)
    {
        // Register global method
        switch (_combineMethod)
        {
            case CombineMethods.SmoothUnion:
                cac.Globals["fOpSmoothUnion"]
                    = """
                      float fOpSmoothUnion(float a, float b, float k) {
                          float h = max(k - abs(a - b), 0.0);
                          return min(a, b) - (h * h) / (4.0 * k);
                      };
                      """;
                break;

            case CombineMethods.UnionChamfer:
                cac.Globals["fOpUnionChamfer"]
                    = """
                      // The "Chamfer" flavour makes a 45-degree chamfered edge (the diagonal of a square of size <r>):
                      float fOpUnionChamfer(float a, float b, float r) {
                          return min(min(a, b), (a - r + b)*sqrt(0.5));
                      }
                      """;
                break;
            case CombineMethods.IntersectionChamfer:
            case CombineMethods.DifferenceChamfer:
                cac.Globals["fOpIntersectionChamfer"]
                    = """
                      // Intersection has to deal with what is normally the inside of the resulting object
                      // when using union, which we normally don't care about too much. Thus, intersection
                      // implementations sometimes differ from union implementations.
                      float fOpIntersectionChamfer(float a, float b, float r) {
                          return max(max(a, b), (a + r + b)*sqrt(0.5));
                      }
                      """;
                
                cac.Globals["fOpDifferenceChamfer"]
                    = """
                      // Difference can be built from Intersection or Union:
                      float fOpDifferenceChamfer (float a, float b, float r) {
                          return fOpIntersectionChamfer(a, -b, r);
                      }
                      """;
                break;
            case CombineMethods.UnionRound:
                cac.Globals["fOpUnionRound"]
                    = """
                       // The "Round" variant uses a quarter-circle to join the two objects smoothly:
                       float fOpUnionRound(float a, float b, float r) {
                           float2 u = max(float2(r - a,r - b), float2(0));
                           return max(r, min (a, b)) - length(u);
                       }
                      """;
                break;

            case CombineMethods.DifferenceRound:
                cac.Globals["fOpDifferenceRound"]
                    = """
                      float fOpDifferenceRound (float a, float b, float r) {
                          return fOpIntersectionRound(a, -b, r);
                      }
                      """;
                break;

            // The "Columns" flavour makes n-1 circular columns at a 45 degree angle:
            case CombineMethods.UnionColumns:
                cac.Globals["fOpUnionColumns"]
                    = """
                      float fOpUnionColumns(float a, float b, float r, float n) {
                       if ((a < r) && (b < r)) {
                        float2 p = float2(a, b);
                        float columnradius = r*sqrt(2)/((n-1)*2+sqrt(2));
                        pR45(p);
                        p.x -= sqrt(2)/2*r;
                        p.x += columnradius*sqrt(2);
                        if (mod(n,2) == 1) {
                         p.y += columnradius;
                        }
                        // At this point, we have turned 45 degrees and moved at a point on the
                        // diagonal that we want to place the columns on.
                        // Now, repeat the domain along this direction and place a circle.
                        pMod1(p.y, columnradius*2);
                        float result = length(p) - columnradius;
                        result = min(result, p.x);
                        result = min(result, a);
                        return min(result, b);
                       } else {
                        return min(a, b);
                       }
                      }
                      """;
                break;

            case CombineMethods.DifferenceColumns:
            case CombineMethods.IntersectionColumns:
                cac.Globals["fOpDifferenceColumns"]
                    = """
                      float fOpDifferenceColumns(float a, float b, float r, float n) {
                          a = -a;
                          float m = min(a, b);
                          //avoid the expensive computation where not needed (produces discontinuity though)
                          if ((a < r) && (b < r)) {
                          float2 p = float2(a, b);
                          float columnradius = r*sqrt(2)/n/2.0;
                          columnradius = r*sqrt(2)/((n-1)*2+sqrt(2));
                      
                          pR45(p);
                          p.y += columnradius;
                          p.x -= sqrt(2)/2*r;
                          p.x += -columnradius*sqrt(2)/2;
                      
                          if (mod(n,2) == 1) {
                          p.y += columnradius;
                          }
                          pMod1(p.y,columnradius*2);
                      
                          float result = -length(p) + columnradius;
                          result = max(result, p.x);
                          result = min(result, a);
                          return -min(result, b);
                          } else {
                          return -m;
                          }
                      }
                      """;

                cac.Globals["fOpIntersectionColumns"]
                    = """
                      float fOpIntersectionColumns(float a, float b, float r, float n) {
                           return fOpDifferenceColumns(a,-b,r, n);
                      }
                      """;
                break;
            // The "Stairs" flavour produces n-1 steps of a staircase:
            // much less stupid version by paniq
            case CombineMethods.UnionStairs:
                cac.Globals["fOpUnionStairs"]
                    = """
                      float fOpUnionStairs(float a, float b, float r, float n) {
                          float s = r/n;
                          float u = b-r;
                          return min(min(a,b), 0.5 * (u + a + abs ((mod (u - a + s, 2 * s)) - s)));
                      }
                      """;
                break;

            // We can just call Union since stairs are symmetric.
            case CombineMethods.IntersectionStairs:
                cac.Globals["fOpIntersectionStairs"] = """
                                                       float fOpIntersectionStairs(float a, float b, float r, float n) {
                                                       	   return -fOpUnionStairs(-a, -b, r, n);
                                                       }
                                                       """;
                break;

            case CombineMethods.DifferenceStairs:
                cac.Globals["fOpDifferenceStairs"] = """
                                                     float fOpDifferenceStairs(float a, float b, float r, float n) {
                                                     	return -fOpUnionStairs(-a, b, r, n);
                                                     }
                                                     """;
                break;

            // Similar to fOpUnionRound, but more lipschitz-y at acute angles
            // (and less so at 90 degrees). Useful when fudging around too much
            // by MediaMolecule, from Alex Evans' siggraph slides
            case CombineMethods.UnionSoft:
                cac.Globals["fOpUnionSoft"] = """
                                              float fOpUnionSoft(float a, float b, float r) {
                                              	float e = max(r - abs(a - b), 0);
                                              	return min(a, b) - e*e*0.25/r;
                                              }
                                              """;
                break;

            // produces a cylindical pipe that runs along the intersection.
            // No objects remain, only the pipe. This is not a boolean operator.
            case CombineMethods.Pipe:
                cac.Globals["fOpPipe"] = """
                                         float fOpPipe(float a, float b, float r) {
                                         	return length(float2(a, b)) - r;
                                         }
                                         """;
                break;
            // first object gets a v-shaped engraving where it intersect the second
            case CombineMethods.Engrave:
                cac.Globals["fOpEngrave"] = """
                                            float fOpEngrave(float a, float b, float r) {
                                            	return max(a, (a + r - abs(b))*sqrt(0.5));
                                            }
                                            """;
                break;

            // first object gets a capenter-style groove cut out
            case CombineMethods.Groove:
                cac.Globals["fOpGroove"] = """
                                                                        float fOpGroove(float a, float b, float ra, float rb) {
                                           	return max(a, min(a + ra, rb - abs(b)));
                                           }
                                           """;
                break;

            // first object gets a capenter-style tongue attached
            case CombineMethods.Tongue:
                cac.Globals["fOpTongue"] = """
                                                                    float fOpTongue(float a, float b, float ra, float rb) {
                                           return min(a, max(a - ra, abs(b) - rb));
                                           }
                                                   
                                           """;
                break;
        }
    }

    public void GetPostShaderCode(CodeAssembleContext cac, int inputIndex)
    {
        // Just pass along subcontext if not enough connected fields...
        if (ShaderNode.InputNodes.Count <= 1)
        {
            cac.AppendCall("// skipping combine with single or no input...");
            return;
        }

        Debug.Assert(cac.ContextIdStack.Count >= 2);

        var contextId = cac.ContextIdStack[^2];
        var subContextId = cac.ContextIdStack[^1];

        if (inputIndex == 0)
        {
            // Keep initial value
            cac.AppendCall($"f{contextId} = f{subContextId};");
        }
        else
        {
            // Combine initial value with new value...
            switch (_combineMethod)
            {
                case CombineMethods.Add:
                    cac.AppendCall($"f{contextId}.w += f{subContextId}.w;");
                    break;
                case CombineMethods.Sub:
                    cac.AppendCall($"f{contextId}.w -=  f{subContextId}.w;");
                    break;
                case CombineMethods.Multiply:
                    cac.AppendCall($"f{contextId}.w *= f{subContextId}.w;");
                    break;
                case CombineMethods.Min:
                    cac.AppendCall($"f{contextId}.w = min(f{contextId}.w, f{subContextId}.w);");
                    break;
                case CombineMethods.Max:
                    cac.AppendCall($"f{contextId}.w = max(f{contextId}.w, f{subContextId}.w);");
                    break;
                case CombineMethods.SmoothUnion:
                    cac.AppendCall($"f{contextId}.w = fOpSmoothUnion(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.CutOut:
                    cac.AppendCall($"f{contextId}.w = max(f{contextId}.w, -f{subContextId}.w);");
                    break;
                
                case CombineMethods.UnionChamfer:
                    cac.AppendCall($"f{contextId}.w = fOpUnionChamfer(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.IntersectionChamfer:
                    cac.AppendCall($"f{contextId}.w = fOpIntersectionChamfer(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.DifferenceChamfer:
                    cac.AppendCall($"f{contextId}.w = fOpDifferenceChamfer(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.UnionRound:
                    cac.AppendCall($"f{contextId}.w = fOpUnionRound(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;

                    
                
                
                case CombineMethods.DifferenceRound:
                    cac.AppendCall($"f{contextId}.w = fOpSmoothUnion(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.UnionColumns:
                    cac.AppendCall($"f{contextId}.w = fOpUnionColumns(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.DifferenceColumns:
                    cac.AppendCall($"f{contextId}.w = fOpDifferenceColumns(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.IntersectionColumns:
                    cac.AppendCall($"f{contextId}.w = fOpIntersectionColumns(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.UnionStairs:
                    cac.AppendCall($"f{contextId}.w = fOpUnionStairs(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.IntersectionStairs:
                    cac.AppendCall($"f{contextId}.w = fOpIntersectionStairs(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.DifferenceStairs:
                    cac.AppendCall($"f{contextId}.w = fOpDifferenceStairs(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.UnionSoft:
                    cac.AppendCall($"f{contextId}.w = fOpUnionSoft(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.Pipe:
                    cac.AppendCall($"f{contextId}.w = fOpPipe(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.Engrave:
                    cac.AppendCall($"f{contextId}.w = fOpEngrave(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.Groove:
                    cac.AppendCall($"f{contextId}.w = fOpGroove(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
                case CombineMethods.Tongue:
                    cac.AppendCall($"f{contextId}.w = fOpTongue(f{contextId}.w, f{subContextId}.w, {ShaderNode}K);");
                    break;
            }

            cac.AppendCall($"f{contextId}.xyz = f{contextId}.w < f{subContextId}.w ? f{contextId}.xyz : f{subContextId}.xyz;");
        }
    }

    public ShaderGraphNode ShaderNode { get; }

    private CombineMethods _combineMethod;

    private enum CombineMethods
    {
        Add,
        Sub,
        Multiply,
        Min,
        Max,
        SmoothUnion,
        CutOut,

        UnionChamfer,
        IntersectionChamfer,
        DifferenceChamfer,
        UnionRound,
        
        DifferenceRound,
        UnionColumns,
        DifferenceColumns,
        IntersectionColumns,
        UnionStairs,
        IntersectionStairs,
        DifferenceStairs,
        UnionSoft,
        Pipe,
        Engrave,
        Groove,
        Tongue,
    }

    [Input(Guid = "7248C680-7279-4C1D-B968-3864CB849C77")]
    public readonly MultiInputSlot<ShaderGraphNode> InputFields = new();

    [GraphParam]
    [Input(Guid = "9E4F5916-722D-4C4B-B1CA-814958A5B836")]
    public readonly InputSlot<float> K = new();

    [Input(Guid = "4648E514-B48C-4A98-A728-3EBF9BCFA0B7", MappedType = typeof(CombineMethods))]
    public readonly InputSlot<int> CombineMethod = new();
}