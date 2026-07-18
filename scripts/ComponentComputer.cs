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
