using System;
using System.Collections.Generic;
using Godot;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

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

    private DisjointSet<Vector2I> nodes = new DisjointSet<Vector2I>();
    private DisjointSet<Vector2I> connected = new DisjointSet<Vector2I>();

    [Export]
    private PackedScene pin;

    [Export]
    private PackedScene wire;

    private List<Component> components = new();
    private List<Wire> wires = new List<Wire>();

    private Dictionary<Vector2I, Component> occupied = new Dictionary<Vector2I, Component>();
    private Dictionary<Vector2I, Pin> pins = new Dictionary<Vector2I, Pin>();

    private Dictionary<Vector2I, double> nodeVoltages = new();

    private Vector2I? wireStart;
    public static CircuitManager instance;

    [Export]
    public PackedScene resistor;

    [Export]
    public PackedScene battery;

    public override void _Ready()
    {
        instance = this;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("R"))
        {
            PlaceComponent(resistor);
        }
        if (Input.IsActionJustPressed("B"))
        {
            PlaceComponent(battery);
        }
        QueueRedraw();
        Vector2I mouseCell = PositionToCell(GetGlobalMousePosition());
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
                && nodes.Find(wireStart.Value) != nodes.Find(mouseCell)
            )
            {
                Wire n = wire.Instantiate<Wire>();
                n.Start = wireStart.Value;
                n.End = mouseCell;
                wires.Add(n);
                AddChild(n);
                RecomputeDSU();
            }
            wireStart = null;
        }
    }

    private class Island
    {
        public readonly List<Component> Comps = new();
        public readonly List<Vector2I> Nodes = new();
        public readonly Dictionary<Vector2I, int> Index = new();
        public int VSourceCount;

        public int NumNodes => Nodes.Count - 1;
        public int Size => NumNodes + VSourceCount;
    }

    private Vector2I NodeRoot(Vector2I cell) => nodes.Find(cell);

    private Vector2I IslandKey(Vector2I cell) => connected.Find(nodes.Find(cell));

    public void Solve()
    {
        nodeVoltages.Clear();
        Dictionary<Vector2I, Island> islands = new();

        Island IslandFor(Vector2I key)
        {
            if (!islands.TryGetValue(key, out var island))
            {
                island = new Island();
                islands[key] = island;
            }
            return island;
        }
        foreach (var node in nodes.Roots())
        {
            var island = IslandFor(connected.Find(node));
            island.Index[node] = island.Nodes.Count - 1;
            island.Nodes.Add(node);
        }

        foreach (Component comp in components)
        {
            var island = IslandFor(IslandKey(comp.pins[0].Cell));
            island.Comps.Add(comp);
            if (comp.computer.IsVSource)
                island.VSourceCount++;
        }
        foreach (var island in islands.Values)
        {
            if (island.Comps.Count == 0)
                continue;

            var A = Matrix<double>.Build.Dense(island.Size, island.Size);
            Vector<double> b = Vector<double>.Build.Dense(island.Size);
            int vSourceIndex = 0;
            foreach (var comp in island.Comps)
            {
                comp.computer.Stamp(
                    A,
                    b,
                    island.Index,
                    comp.pins,
                    nodes,
                    island.NumNodes,
                    island.VSourceCount,
                    vSourceIndex
                );
                if (comp.computer.IsVSource)
                    vSourceIndex++;
            }

            Vector<double> x = A.Solve(b);
            foreach (var node in island.Nodes)
            {
                int i = island.Index[node];
                if (i >= 0)
                {
                    nodeVoltages[node] = x[i];
                    GD.Print($"V_{i}: {x[i]}");
                }
            }
            int vs = 0;
            foreach (var comp in island.Comps)
            {
                if (comp.computer.IsVSource)
                {
                    comp.Current = x[island.NumNodes + vs];
                    GD.Print($"I_{vs}: {comp.Current}");
                    vs++;
                }
            }
            GD.Print("-------");
        }
        GD.Print("---END_RUN---");
    }

    public void RecomputeDSU()
    {
        nodes.Clear();
        foreach (Wire w in wires)
        {
            Vector2I d = w.End - w.Start;
            if (d.X == 0 || d.Y == 0)
            {
                Vector2I step = new(Math.Sign(d.X), Math.Sign(d.Y));
                Vector2I cur = w.Start;
                Vector2I prev = cur;
                while (prev != w.End)
                {
                    Vector2I next = prev + step;
                    if (pins.ContainsKey(next))
                    {
                        nodes.Union(cur, next);
                        cur = next;
                    }
                    prev = next;
                }
            }
            nodes.Union(w.Start, w.End);
        }
        GD.Print("=== RecomputeDSU ===");
        GD.Print($"pins cells: {string.Join(", ", pins.Keys)}");
        foreach (Wire w in wires)
            GD.Print(
                $"wire {w.Start} -> {w.End}  (startIsPin={pins.ContainsKey(w.Start)}, endIsPin={pins.ContainsKey(w.End)})"
            );
        foreach (Component comp in components)
        {
            var c0 = comp.pins[0].Cell;
            var c1 = comp.pins[1].Cell;
            GD.Print(
                $"{comp.computer.GetType().Name}: pin0 cell {c0} root {nodes.Find(c0)}, pin1 cell {c1} root {nodes.Find(c1)}"
            );
        }
        connected.Clear();
        foreach (Component comp in components)
        {
            Vector2I start = NodeRoot(comp.pins[0].Cell);
            foreach (Pin p in comp.pins)
            {
                Vector2I node = NodeRoot(p.Cell);
                if (node != start)
                {
                    connected.Union(node, start);
                }
            }
        }
        Solve();
    }

    public Vector2I PositionToCell(Vector2 worldPosition)
    {
        Vector2 local = (worldPosition - origin) / gridSize + new Vector2(0.5f, 0.5f);
        return new Vector2I(Mathf.RoundToInt(local.X), Mathf.RoundToInt(local.Y));
    }

    public Vector2 CellToPosition(Vector2I g)
    {
        return new Vector2(g.X - 0.5f, g.Y - 0.5f) * gridSize + origin;
    }

    public void PlaceComponent(PackedScene p)
    {
        Vector2I cell = PositionToCell(GetGlobalMousePosition());
        if (occupied.ContainsKey(cell))
        {
            return;
        }
        Component c = p.Instantiate<Component>();
        components.Add(c);
        c.GlobalPosition = CellToPosition(cell);
        AddChild(c);
        occupied.Add(cell, c);
        foreach (Pin pin in c.pins)
        {
            GD.Print(pin.Cell);
            pins.Add(pin.Cell, pin);
        }
        RecomputeDSU();
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
