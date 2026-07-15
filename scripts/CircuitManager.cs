using System;
using Godot;

public partial class CircuitManager : Node2D
{
    [Export]
    private Vector2 origin;

    [Export]
    private float gridSize;

    [Export]
    private Color gridColor = new Color(0.3f, 0.3f, 0.3f);

    [Export]
    private float gridLineWidth = 1.0f;

    public override void _Process(double delta)
    {
        base._Process(delta);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (gridSize <= 0f)
            return;

        Transform2D screenToWorld = GetViewportTransform().AffineInverse();
        Rect2 screen = GetViewportRect();
        Rect2 view = new Rect2(screenToWorld * screen.Position, Vector2.Zero).Expand(
            screenToWorld * (screen.Position + screen.Size)
        );
        float startX = origin.X + Mathf.Floor((view.Position.X - origin.X) / gridSize) * gridSize;
        float startY = origin.Y + Mathf.Floor((view.Position.Y - origin.Y) / gridSize) * gridSize;
        float endX = view.Position.X + view.Size.X;
        float endY = view.Position.Y + view.Size.Y;
        for (float x = startX; x <= endX; x += gridSize)
        {
            DrawLine(
                new Vector2(x, view.Position.Y),
                new Vector2(x, endY),
                gridColor,
                gridLineWidth
            );
        }
        for (float y = startY; y <= endY; y += gridSize)
        {
            DrawLine(
                new Vector2(view.Position.X, y),
                new Vector2(endX, y),
                gridColor,
                gridLineWidth
            );
        }
    }
}
