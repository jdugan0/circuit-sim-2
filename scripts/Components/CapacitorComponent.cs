using System;
using System.Collections.Generic;
using Godot;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

[GlobalClass]
public partial class CapacitorComponent : ComponentComputer
{
    [Export]
    public double C;

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
        (double g, double Ieq) = TrBdf2.Capacitive(stage, C, state, delta);
        var n1 = nodeIndex[nodes.Find(pins[0].Cell)];
        var n2 = nodeIndex[nodes.Find(pins[1].Cell)];
        if (n1 >= 0)
            b[n1] -= Ieq;
        if (n2 >= 0)
            b[n2] += Ieq;

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
        return nodeVoltages[nodes.Find(pins[0].Cell)] - nodeVoltages[nodes.Find(pins[1].Cell)];
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
        var (g, I0) = TrBdf2.Capacitive(stage, C, state, delta);
        return g * (v1 - v2) + I0;
    }
}
