using System;
using Godot;

public partial class Pin : Node2D
{
    [Export]
    public Component parent;

    public Vector2I Cell => CircuitManager.instance.PositionToCell(GlobalPosition);
}
