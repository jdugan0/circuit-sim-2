using System;
using Godot;

public partial class Pin : Node2D
{
    [Export]
    public Component parent { get; private set; }

    public Vector2I Cell => CircuitManager.instance.PositionToCell(GlobalPosition);
}
