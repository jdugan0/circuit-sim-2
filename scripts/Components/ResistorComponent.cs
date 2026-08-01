using System;
using System.Collections.Generic;
using Godot;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

[GlobalClass]
public partial class ResistorComponent : ComponentComputer
{
    [Export]
    public double R;

    public override void Stamp(
        Matrix<double> A,
        Vector<double> b,
        Dictionary<Vector2I, int> nodeIndex,
        List<Pin> pins,
        DisjointSet<Vector2I> nodes,
        int n,
        int m,
        int vSourceIndex,
        Component state,
        double delta,
        int stage
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

    public override double ComputeVoltage(
        List<Pin> pins,
        DisjointSet<Vector2I> nodes,
        Dictionary<Vector2I, double> nodeVoltages
    )
    {
        var v1 = nodeVoltages[nodes.Find(pins[0].Cell)];
        var v2 = nodeVoltages[nodes.Find(pins[1].Cell)];
        return v1 - v2;
    }

    public override double ComputeCurrent(
        Vector<double> x,
        List<Pin> pins,
        DisjointSet<Vector2I> nodes,
        Dictionary<Vector2I, double> nodeVoltages,
        int n,
        int vSourceIndex,
        Component state,
        double delta,
        int stage
    )
    {
        var v1 = nodeVoltages[nodes.Find(pins[0].Cell)];
        var v2 = nodeVoltages[nodes.Find(pins[1].Cell)];
        return (v1 - v2) / R;
    }
}
