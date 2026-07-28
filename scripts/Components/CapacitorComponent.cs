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
        double delta
    )
    {
        double g = C / delta;
        double Ieq = g * state.V;
        var n1 = nodeIndex[nodes.Find(pins[0].Cell)];
        var n2 = nodeIndex[nodes.Find(pins[1].Cell)];
        if (n1 >= 0)
            b[n1] += Ieq;
        if (n2 >= 0)
            b[n2] -= Ieq;

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
