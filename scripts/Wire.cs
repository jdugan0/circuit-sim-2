using System;
using Godot;

public partial class Wire : Node2D
{
    public Vector2I Start;
    public Vector2I End;

    [Export]
    private float width = 5;

    [Export]
    private Color color;

    public override void _Ready()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawLine(
            CircuitManager.instance.CellToPosition(Start),
            CircuitManager.instance.CellToPosition(End),
            color,
            width
        );
    }
}
