using System;
using System.Collections.Generic;
using Godot;

public partial class Component : Node2D
{
    [Export]
    PackedScene pin;

    [Export]
    public Godot.Collections.Array<Vector2I> pinResources;

    public List<Pin> pins = new List<Pin>();

    [Export]
    public ComponentComputer computer;

    public double? Current = null;

    public override void _Ready()
    {
        foreach (Vector2I p in pinResources)
        {
            Pin pObj = pin.Instantiate<Pin>();
            pObj.parent = this;
            AddChild(pObj);
            GD.Print($"OFFSET: {(Vector2)p * CircuitManager.instance.gridSize}");
            pObj.GlobalPosition = GlobalPosition + (Vector2)p * CircuitManager.instance.gridSize;
            pins.Add(pObj);
        }
    }
}
