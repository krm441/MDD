using System.Collections.Generic;
using UnityEngine;

namespace GG
{
    public enum NodeLabel
    {
        None,
        S,          // grammar start symbol
        A, B, C,    // island types
        Start,
        Boss,
        Corridor,   // used when painting roads on Voronoi

        Shore,      // one ring around islands
        LandDecor   //non-walkable decorative land fill
    }

    public class SpecNode
    {
        public int id;
        public NodeLabel label;
        public float radius; // world units - island radius
    }

    public struct SpecEdge
    {
        public int u, v;
        public SpecEdge(int u, int v) { this.u = u; this.v = v; }
    }

    public class GraphSpec
    {
        public readonly List<SpecNode> nodes = new List<SpecNode>();
        public readonly List<SpecEdge> edges = new List<SpecEdge>();

        public SpecNode AddNode(NodeLabel label, float radius)
        {
            var n = new SpecNode { id = nodes.Count, label = label, radius = radius };
            nodes.Add(n);
            return n;
        }

        public void AddEdge(int u, int v)
        {
            if (u == v) return;
            edges.Add(new SpecEdge(u, v));
        }
    }

    public class Node
    {
        public int id;
        public NodeLabel label;
        public Vector2 pos;     
        public float radius;   
    }

    public struct Edge
    {
        public int u, v;
        public Edge(int u, int v) { this.u = u; this.v = v; }
    }

    public class Graph
    {
        public readonly List<Node> nodes = new List<Node>();
        public readonly List<Edge> edges = new List<Edge>();

        public Node AddNode(NodeLabel label, Vector2 pos)
        {
            var n = new Node { id = nodes.Count, label = label, pos = pos, radius = 0f };
            nodes.Add(n);
            return n;
        }

        public Node AddNode(NodeLabel label, Vector2 pos, float radius)
        {
            var n = new Node { id = nodes.Count, label = label, pos = pos, radius = radius };
            nodes.Add(n);
            return n;
        }

        public void AddEdge(int u, int v)
        {
            if (u == v) return;
            edges.Add(new Edge(u, v));
        }

        public static Graph FromSpec(GraphSpec spec, IDictionary<int, Vector2> positions)
        {
            var g = new Graph();

            for (int i = 0; i < spec.nodes.Count; i++)
            {
                var sn = spec.nodes[i];
                Vector2 p = positions != null && positions.TryGetValue(sn.id, out var got) ? got : Vector2.zero;
                g.AddNode(sn.label, p, sn.radius);
            }

            foreach (var e in spec.edges)
                g.AddEdge(e.u, e.v);

            return g;
        }
    }
}
