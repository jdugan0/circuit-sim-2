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
        List<Pin> pins,
        DisjointSet<Vector2I> nodes,
        int n,
        int m,
        int vSourceIndex,
        Component state,
        double delta,
        int stage
    ) { }

    public virtual double ComputeVoltage(
        List<Pin> pins,
        DisjointSet<Vector2I> nodes,
        Dictionary<Vector2I, double> nodeVoltages
    )
    {
        return 0;
    }

    public virtual double ComputeCurrent(
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
        return 0;
    }
}
