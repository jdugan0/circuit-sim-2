using System.Collections.Generic;
using Godot;

public class DisjointSet<T>
{
    private readonly Dictionary<T, T> parent = new();
    private readonly Dictionary<T, int> rank = new();

    private void MakeSet(T x)
    {
        if (parent.ContainsKey(x))
            return;
        parent[x] = x;
        rank[x] = 0;
    }

    public T Find(T x)
    {
        MakeSet(x);
        T root = x;
        while (!EqualityComparer<T>.Default.Equals(parent[root], root))
            root = parent[root];
        while (!EqualityComparer<T>.Default.Equals(parent[x], root))
        {
            T next = parent[x];
            parent[x] = root;
            x = next;
        }
        return root;
    }

    public void Union(T a, T b)
    {
        T ra = Find(a),
            rb = Find(b);
        if (EqualityComparer<T>.Default.Equals(ra, rb))
            return;
        if (rank[ra] < rank[rb])
            (ra, rb) = (rb, ra);
        parent[rb] = ra;
        if (rank[ra] == rank[rb])
            rank[ra]++;
    }

    public List<T> Roots()
    {
        HashSet<T> roots = new();
        foreach (T x in parent.Keys)
            roots.Add(Find(x));
        return new List<T>(roots);
    }

    public bool Connected(T a, T b) => EqualityComparer<T>.Default.Equals(Find(a), Find(b));
}
