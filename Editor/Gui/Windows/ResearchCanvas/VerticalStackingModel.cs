using System;
using System.Collections.Generic;
using System.Numerics;
using T3.Editor.Gui.Selection;

namespace T3.Editor.Gui.Windows.ResearchCanvas;


public class Block : ISelectableCanvasObject
{
    public Block()
    {
    }

    public Block(int x, int y, string name)
    {
        Id = Guid.NewGuid();
        Name = name;
        PosOnCanvas = new Vector2(x, y) * ResearchWindow.BlockSize;
        Inputs = new List<Slot>()
                     {
                         new() { Block = this, AnchorPos = new Vector2(0.5f, 0.0f), IsInput = true },
                         new() { Block = this, AnchorPos = new Vector2(0.0f, 0.5f), IsInput = true },
                     };

        Outputs = new List<Slot>()
                      {
                          new() { Block = this, AnchorPos = new Vector2(0.5f, 1.0f) },
                          new() { Block = this, AnchorPos = new Vector2(1f, 0.5f) },
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

    public int UnitHeight;
    public string Name;
    public Type PrimaryType = typeof(float);

    public List<Slot> Inputs = new();
    public List<Slot> Outputs = new();
    public Guid Id { get; }
    public Vector2 PosOnCanvas { get; set; }
    public Vector2 Size { get; set; }
    public bool IsSelected { get; }

    public override string ToString()
    {
        return Name;
    }
}

public class Group
{
    public List<Block> Blocks;
}

public class Slot
{
    public Block Block;
    public Type Type = typeof(float);
    public Vector2 AnchorPos;
    public bool IsInput;
    public List<Connection> Connections = new List<Connection>(); // not sure if merging inout and output connection is a good idea.
    public bool IsShy;

    public enum Visibility
    {
        Visible,
        ShyButConnected,
        ShyButRevealed,
        Shy,
    }
}

public class Connection
{
    public Slot Source;
    public Slot Target;
    public bool IsSnapped;
}