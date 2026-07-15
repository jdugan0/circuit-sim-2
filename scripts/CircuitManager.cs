using System;
using System.Collections.Generic;
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

    private DisjointSet<Vector2I> connected = new DisjointSet<Vector2I>();

    [Export]
    private PackedScene pin;

    [Export]
    private PackedScene wire;

    private List<Pin> pins = new List<Pin>();
    private List<Wire> wires = new List<Wire>();

    private Dictionary<Vector2I, Node> occupied = new Dictionary<Vector2I, Node>();

    private Vector2I? wireStart;
    public static CircuitManager instance;

    public override void _Ready()
    {
        instance = this;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
        Vector2I mouseCell = PositionToCell(GetGlobalMousePosition());
        // GD.Print(PositionToCell(GetGlobalMousePosition()));
        GD.Print(connected.Roots().Count);
        if (Input.IsActionJustPressed("click"))
        {
            if (!occupied.ContainsKey(mouseCell))
            {
                Pin n = pin.Instantiate<Pin>();
                n.GlobalPosition = CellToPosition(mouseCell);
                pins.Add(n);
                AddChild(n);
            }
        }
        if (Input.IsActionJustPressed("wire"))
        {
            if (!occupied.ContainsKey(mouseCell))
            {
                wireStart = mouseCell;
            }
        }
        if (Input.IsActionJustReleased("wire"))
        {
            if (
                wireStart.HasValue
                && wireStart != mouseCell
                && connected.Find(wireStart.Value) != connected.Find(mouseCell)
            )
            {
                Wire n = wire.Instantiate<Wire>();
                n.Start = wireStart.Value;
                n.End = mouseCell;
                wires.Add(n);
                AddChild(n);
                Vector2I d = n.End - n.Start;
                if (d.X == 0 || d.Y == 0)
                {
                    Vector2I step = new(Math.Sign(d.X), Math.Sign(d.Y));
                    Vector2I cur = n.Start;
                    while (cur != n.End)
                    {
                        Vector2I next = cur + step;
                        connected.Union(cur, next);
                        cur = next;
                    }
                }
            }
            wireStart = null;
        }
    }

    public Vector2I PositionToCell(Vector2 worldPosition)
    {
        Vector2 local = (worldPosition - origin) / gridSize + new Vector2(0.5f, 0.5f);
        return new Vector2I(Mathf.RoundToInt(local.X), Mathf.RoundToInt(local.Y));
    }

    public Vector2 CellToPosition(Vector2I g)
    {
        return new Vector2(g.X - 0.5f, g.Y - 0.5f) * gridSize;
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
