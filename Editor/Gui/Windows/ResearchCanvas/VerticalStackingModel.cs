using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Editor.Gui.Selection;

namespace T3.Editor.Gui.Windows.ResearchCanvas;


public class OutSocket
{
    
}

public class InSocket
{
    
}


/// <summary>
/// Generate considerations:
/// - The layout model is a temporary data that is completely be generated from the t3ui data.
/// - Its computation is expensive and should only be done if required.
/// - This might be especially complicated if the state cannot be stored in the layout model, because it
///   relies on a temp. property like expanding a parameter type group.
/// </summary>
public class SnappedGroup
{
    public List<OutSocket> FreeOutSockets = new();
    public List<InSocket> FreeInSockets = new();

    public List<Block> SnappedBlocks = new();

    public List<Connection> SnappedConnections = new();

    /// <summary>
    /// This will move collect relevant snapped blocks and attempt to adjust the layout.
    /// Returns false.
    /// Note:
    /// - This change should result in a command that actually applies the changes to the operator positions
    /// - After being applied, the SnapGroup(s) should be recalculated.
    /// 
    /// </summary>
    public bool ApplyVerticalLayoutChange(Vector2 canvasPos, float delta)
    {
        return true;
    }
}


public class Block : ISelectableCanvasObject
{
    public Block()
    {
    }

    public Block(int x, int y, string name, Type type= null)
    {
        if (type != null)
        {
            PrimaryType = type;
        }
        Id = Guid.NewGuid();
        Name = name;
        Size = VerticalStackingCanvas.BlockSize;
        PosOnCanvas = new Vector2(x, y) * VerticalStackingCanvas.BlockSize;
        Inputs = new List<Slot>()
                     {
                         new()
                             {
                                 Block = this,
                                 AnchorPositions = new Vector2[]
                                                       {
                                                           new(0.5f, 0.0f),
                                                           new(0.0f, 0.5f),
                                                       },
                                 IsInput = true
                             },
                     };

        Outputs = new List<Slot>()
                      {
                          new()
                              {
                                  Block = this,
                                  AnchorPositions = new Vector2[]
                                                        {
                                                            new(0.5f, 1.0f),
                                                            new(1f, 0.5f),
                                                        },
                              },
                      };
    }

    public IEnumerable<Slot> GetSlots()
    {
        foreach (var input in Inputs)
        {
            yield return input;
        }

        foreach (var output in Outputs)
        {
            yield return output;
        }
    }

    //public int UnitHeight;
    public string Name;
    public readonly Type PrimaryType = typeof(float);

    public readonly List<Slot> Inputs = new();
    public readonly List<Slot> Outputs = new();
    public Guid Id { get; }
    public Vector2 PosOnCanvas { get; set; }
    public Vector2 Size { get; set; }
    public bool IsSelected { get; }

    public override string ToString()
    {
        return Name;
    }
}

public class SnapGroup
{
    public List<Block> Blocks;
}

public class Slot
{
    public Block Block;
    public Type Type = typeof(float);
    public Vector2[] AnchorPositions;
    public bool IsInput;

    public readonly List<Connection> Connections = new(); // not sure if merging inout and output connection is a good idea.
    // public bool IsShy;
    //
    // public enum Visibility
    // {
    //     Visible,
    //     ShyButConnected,
    //     ShyButRevealed,
    //     Shy,
    // }

    public Vector2 VerticalPosOnCanvas => Block.PosOnCanvas + AnchorPositions[0] * Block.Size;
    public Vector2 HorizontalPosOnCanvas => Block.PosOnCanvas + AnchorPositions[1] * Block.Size;
    public bool IsConnected => Connections.Count > 0;

    public IEnumerable<Connection> GetConnections(Connection.Orientations orientation)
    {
        if (Connections.Count == 0)
            yield break;

        foreach (var c in Connections)
        {
            if (c.GetOrientation() == orientation)
                yield return c;
        }
    }
}

public class Connection
{
    public Connection(Slot source, Slot target)
    {
        source.Connections.Add(this);
        target.Connections.Add(this);

        Source = source;
        Target = target;
    }

    public readonly Slot Source;
    public readonly Slot Target;

    public void GetEndPositions(out Vector2 sourcePos, out Vector2 targetPos)
    {
        switch (GetOrientation())
        {
            case Orientations.Vertical:
                sourcePos = Source.VerticalPosOnCanvas;
                targetPos = Target.VerticalPosOnCanvas;
                break;
            case Orientations.Horizontal:
                sourcePos = Source.HorizontalPosOnCanvas;
                targetPos = Target.HorizontalPosOnCanvas;
                break;
            default:
                sourcePos = Vector2.Zero;
                targetPos = Vector2.Zero;
                break;
        }
    }

    public Orientations GetOrientation()
    {
        if (Source == null || Target == null)
            return Orientations.Undefined;

        var delta=
        Source.Block.PosOnCanvas-
        Target.Block.PosOnCanvas;

        return (delta.X  < delta.Y)
                   ? Orientations.Horizontal
                   : Orientations.Vertical;
    }

    public bool IsSnapped
    {
        get
        {
            if (Source == null || Target == null)
                return false;

            {
                var p1 = Source.Block.PosOnCanvas + Source.AnchorPositions[0] * Source.Block.Size;
                var p2 = Target.Block.PosOnCanvas + Target.AnchorPositions[0] * Target.Block.Size;
                if (Vector2.Distance(p1, p2) < 1)
                    return true;
            }
            {
                var p1 = Source.Block.PosOnCanvas + Source.AnchorPositions[1] * Source.Block.Size;
                var p2 = Target.Block.PosOnCanvas + Target.AnchorPositions[1] * Target.Block.Size;
                if (Vector2.Distance(p1, p2) < 1)
                    return true;
            }
            return false;
        }
    }

    public bool IsConnecting(Slot slotA, Slot slotB)
    {
        return slotA == Source && slotB == Target
               || slotB == Source && slotA == Target;
    }

    public enum Orientations
    {
        Undefined,
        Both,
        Vertical,
        Horizontal,
    }
}