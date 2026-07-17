using System;
using System.Collections.Generic;
using Godot;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

[GlobalClass]
public partial class ComponentComputer : Resource
{
    public virtual bool IsVSource => false;

    public virtual void Stamp(
        Matrix<double> A,
        Vector<double> b,
        Dictionary<Vector2I, int> nodeIndex,
        Pin[] pins,
        DisjointSet<Vector2I> nodes,
        int n,
        int m,
        int vSourceIndex
    ) { }
}

public partial class ResistorComponent : ComponentComputer
{
    [Export]
    public double R;

    public override void Stamp(
        Matrix<double> A,
        Vector<double> b,
        Dictionary<Vector2I, int> nodeIndex,
        Pin[] pins,
        DisjointSet<Vector2I> nodes,
        int n,
        int m,
        int vSourceIndex
    )
    {
        var n1 = nodeIndex[nodes.Find(pins[0].Cell)];
        var n2 = nodeIndex[nodes.Find(pins[1].Cell)];
        double g = 1.0 / R;
        if (n1 >= 0)
            A[n1, n1] += g;
        if (n2 >= 0)
            A[n2, n2] += g;
        if (n1 >= 0 && n2 >= 0)
        {
            A[n1, n2] -= g;
            A[n2, n1] -= g;
        }
    }
}

public partial class VoltageSourceComponent : ComponentComputer
{
    public override bool IsVSource => true;

    [Export]
    public double V;

    public override void Stamp(
        Matrix<double> A,
        Vector<double> b,
        Dictionary<Vector2I, int> nodeIndex,
        Pin[] pins,
        DisjointSet<Vector2I> nodes,
        int n,
        int m,
        int vSourceIndex
    )
    {
        var pos = nodeIndex[nodes.Find(pins[0].Cell)];
        var neg = nodeIndex[nodes.Find(pins[1].Cell)];
        int row = n + vSourceIndex;
        if (pos >= 0)
        {
            A[pos, row] += 1;
            A[row, pos] += 1;
        }
        if (neg >= 0)
        {
            A[neg, row] -= 1;
            A[row, neg] -= 1;
        }
        b[row] = V;
    }
}
