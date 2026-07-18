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

    public void Solve()
    {
        nodeVoltages.Clear();
        var islandRoots = connected.Roots();
        var nodeRoots = nodes.Roots();
        Dictionary<Vector2I, List<Component>> islandToComps = new();
        Dictionary<Vector2I, int> islandToNumVSource = new();
        Dictionary<Vector2I, List<Vector2I>> islandToNode = new();
        Dictionary<Vector2I, int> nodeToIndex = new();
        // setup:
        foreach (Component comp in components)
        {
            var island = connected.Find(nodes.Find(PositionToCell(comp.pins[0].GlobalPosition)));
            if (!islandToComps.ContainsKey(island))
            {
                islandToComps[island] = new List<Component>();
            }
            islandToComps[island].Add(comp);
            if (comp.computer.IsVSource)
            {
                islandToNumVSource[island] = islandToNumVSource.GetValueOrDefault(island) + 1;
            }
        }
        foreach (var node in nodeRoots)
        {
            var island = connected.Find(node);
            if (!islandToNode.ContainsKey(island))
            {
                islandToNode[island] = new List<Vector2I>();
            }
            nodeToIndex.Add(node, islandToNode[island].Count - 1);
            islandToNode[island].Add(node);
        }

        // construct matrix

        foreach (var island in islandRoots)
        {
            if (!islandToComps.ContainsKey(island))
            {
                continue;
            }
            var components = islandToComps[island];
            int numVSources = islandToNumVSource.GetValueOrDefault(island);
            var islandNodes = islandToNode[island];
            int numNodes = islandNodes.Count - 1;
            int size = numNodes + numVSources;
            GD.Print(
                $"[island {island}] nodeRoots={islandNodes.Count} numNodes={numNodes} numVSources={numVSources}"
            );
            foreach (var comp in components)
            {
                var i0 = nodeToIndex[nodes.Find(comp.pins[0].Cell)];
                var i1 = nodeToIndex[nodes.Find(comp.pins[1].Cell)];
                GD.Print($"  {comp.computer.GetType().Name}: pin0->idx {i0}, pin1->idx {i1}");
            }
            var A = Matrix<double>.Build.Dense(size, size);
            Vector<double> b = Vector<double>.Build.Dense(size);
            int vSourceIndex = 0;
            foreach (var comp in components)
            {
                comp.computer.Stamp(
                    A,
                    b,
                    nodeToIndex,
                    comp.pins,
                    nodes,
                    numNodes,
                    numVSources,
                    vSourceIndex
                );
                if (comp.computer.IsVSource)
                {
                    vSourceIndex++;
                }
            }
            GD.Print(A);
            // store solved data
            Vector<double> x = A.Solve(b);
            foreach (var node in islandNodes)
            {
                int i = nodeToIndex[node];
                if (i >= 0)
                {
                    nodeVoltages[node] = x[i];
                    GD.Print($"V_{i}: {x[i]}");
                }
            }
            int vs = 0;
            foreach (var comp in components)
            {
                if (comp.computer.IsVSource)
                {
                    comp.Current = x[numNodes + vs];
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
            var c0 = PositionToCell(comp.pins[0].GlobalPosition);
            var c1 = PositionToCell(comp.pins[1].GlobalPosition);
            GD.Print(
                $"{comp.computer.GetType().Name}: pin0 cell {c0} root {nodes.Find(c0)}, pin1 cell {c1} root {nodes.Find(c1)}"
            );
        }
        connected.Clear();
        foreach (Component comp in components)
        {
            Vector2I start = nodes.Find(PositionToCell(comp.pins[0].GlobalPosition));
            foreach (Pin p in comp.pins)
            {
                Vector2I node = nodes.Find(PositionToCell(p.GlobalPosition));
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
