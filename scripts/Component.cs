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

    public double I = 0;

    public double V = 0;

    // Values at intermediate timestep gamma.
    public double Vg = 0;
    public double Ig = 0;

    public override void _Ready()
    {
        foreach (Vector2I p in pinResources)
        {
            Pin pObj = pin.Instantiate<Pin>();
            pObj.parent = this;
            AddChild(pObj);
            pObj.GlobalPosition = GlobalPosition + (Vector2)p * CircuitManager.instance.gridSize;
            pins.Add(pObj);
        }
    }

    public override void _Process(double delta)
    {
        if (computer is CapacitorComponent)
        {
            GD.Print($"V:{V}, I:{I}");
        }
    }
}
