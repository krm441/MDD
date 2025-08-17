using System;
using System.Collections.Generic;
using UnityEngine;

namespace GG
{
    public class GraphGrammar
    {
        public float radiusA = 9f;
        public float radiusB = 7f;
        public float radiusC = 7f;
        public float radiusStart = 6f;
        public float radiusBoss = 7f;

        float RadiusFor(NodeLabel L)
        {
            switch (L)
            {
                case NodeLabel.A: return radiusA;
                case NodeLabel.B: return radiusB;
                case NodeLabel.C: return radiusC;
                case NodeLabel.Start: return radiusStart;
                case NodeLabel.Boss: return radiusBoss;
                default: return 0f;
            }
        }

        public class RuleOption
        {
            public float weight = 1f;
            public Func<SpecNode, System.Random, Fragment> build;
        }

        public class Rule
        {
            public NodeLabel lhs;
            public readonly List<RuleOption> options = new List<RuleOption>();
        }

        public class Fragment
        {
            public GraphSpec g = new GraphSpec();
            public int anchorLocalId = 0;
        }

        private readonly Dictionary<NodeLabel, Rule> rules = new Dictionary<NodeLabel, Rule>();
        private readonly System.Random rng;

        public GraphGrammar(int seed = 12345)
        {
            rng = new System.Random(seed);
        }

        public void AddRule(NodeLabel lhs, Func<SpecNode, System.Random, Fragment> builder, float weight = 1f)
        {
            if (!rules.TryGetValue(lhs, out var rule))
            {
                rule = new Rule { lhs = lhs };
                rules[lhs] = rule;
            }
            rule.options.Add(new RuleOption { weight = weight, build = builder });
        }

        public GraphSpec Derive(NodeLabel start, int steps)
        {
            var g = new GraphSpec();
            var root = g.AddNode(start, RadiusFor(start));

            for (int s = 0; s < steps; s++)
            {
                int idx = FindExpandableNode(g);
                if (idx < 0) break;

                var oldNode = g.nodes[idx];
                if (!rules.TryGetValue(oldNode.label, out var rule) || rule.options.Count == 0)
                    break;

                var frag = Pick(rule).build(oldNode, rng);
                ApplyFragment(g, idx, frag);
            }
            return g;
        }

        //[Obsolete]
        //public GraphSpec Derive(NodeLabel start, Vector2 center, int steps)
        //{
        //    return Derive(start, steps);
        //}

        int FindExpandableNode(GraphSpec g)
        {
            for (int i = 0; i < g.nodes.Count; i++)
            {
                var L = g.nodes[i].label;
                if (L == NodeLabel.S || L == NodeLabel.A || L == NodeLabel.B || L == NodeLabel.C)
                    if (rules.ContainsKey(L)) return i;
            }
            return -1;
        }

        RuleOption Pick(Rule rule)
        {
            if (rule.options.Count == 1) return rule.options[0];
            float sum = 0f; foreach (var o in rule.options) sum += Mathf.Max(0.0001f, o.weight);
            double r = rng.NextDouble() * sum;
            foreach (var o in rule.options)
            {
                r -= Mathf.Max(0.0001f, o.weight);
                if (r <= 0) return o;
            }
            return rule.options[rule.options.Count - 1];
        }

        void ApplyFragment(GraphSpec g, int nodeIndex, Fragment frag)
        {
            // 0) original node info
            var oldId = g.nodes[nodeIndex].id;

            // 1) collect and remove edges touching old node
            var neighbors = new List<int>();
            for (int i = 0; i < g.edges.Count; i++)
            {
                var e = g.edges[i];
                if (e.u == oldId) neighbors.Add(e.v);
                else if (e.v == oldId) neighbors.Add(e.u);
            }
            g.edges.RemoveAll(e => e.u == oldId || e.v == oldId);

            // 2) set anchor (keep old id, swap label+radius to fragment anchor's)
            var anchorLocal = frag.anchorLocalId;
            var anchorFrag = frag.g.nodes[anchorLocal];
            g.nodes[nodeIndex].label = anchorFrag.label;
            g.nodes[nodeIndex].radius = anchorFrag.radius;

            // 3) append other fragment nodes (create id map; anchor maps to oldId)
            var map = new Dictionary<int, int>();
            map[anchorLocal] = oldId;

            for (int i = 0; i < frag.g.nodes.Count; i++)
            {
                if (i == anchorLocal) continue;
                var fn = frag.g.nodes[i];
                var appended = g.AddNode(fn.label, fn.radius);
                map[i] = appended.id;
            }

            // 4) add fragment edges
            foreach (var fe in frag.g.edges)
            {
                int U = map.ContainsKey(fe.u) ? map[fe.u] : oldId; // anchor maps to oldId
                int V = map.ContainsKey(fe.v) ? map[fe.v] : oldId;
                g.AddEdge(U, V);
            }

            // 5) rewire old neighbors to the anchor
            foreach (var nb in neighbors) g.AddEdge(oldId, nb);
        }

        // ========= Default rule set  =========
        public void AddDefaultRules()
        {
            // S -> Start - A - Boss
            AddRule(NodeLabel.S, (old, R) =>
            {
                var f = new Fragment();
                var start = f.g.AddNode(NodeLabel.Start, RadiusFor(NodeLabel.Start));
                var a = f.g.AddNode(NodeLabel.A, RadiusFor(NodeLabel.A));
                var boss = f.g.AddNode(NodeLabel.Boss, RadiusFor(NodeLabel.Boss));
                f.g.AddEdge(start.id, a.id);
                f.g.AddEdge(a.id, boss.id);
                f.anchorLocalId = a.id; // neighbors connect to the A in the middle
                return f;
            });

            // A -> A - A   (linear extension)
            AddRule(NodeLabel.A, (old, R) =>
            {
                var f = new Fragment();
                var left = f.g.AddNode(NodeLabel.A, RadiusFor(NodeLabel.A));
                var right = f.g.AddNode(NodeLabel.A, RadiusFor(NodeLabel.A));
                f.g.AddEdge(left.id, right.id);
                f.anchorLocalId = left.id; // old neighbors attach to the left A
                return f;
            }, weight: 1f);

            // A -> A - B   (branch to a special island)
            AddRule(NodeLabel.A, (old, R) =>
            {
                var f = new Fragment();
                var a = f.g.AddNode(NodeLabel.A, RadiusFor(NodeLabel.A));
                var b = f.g.AddNode(NodeLabel.B, RadiusFor(NodeLabel.B));
                f.g.AddEdge(a.id, b.id);
                f.anchorLocalId = a.id;
                return f;
            }, weight: 1f);

            // B -> A - C - A (small hub)
            AddRule(NodeLabel.B, (old, R) =>
            {
                var f = new Fragment();
                var a1 = f.g.AddNode(NodeLabel.A, RadiusFor(NodeLabel.A));
                var c = f.g.AddNode(NodeLabel.C, RadiusFor(NodeLabel.C));
                var a2 = f.g.AddNode(NodeLabel.A, RadiusFor(NodeLabel.A));
                f.g.AddEdge(a1.id, c.id);
                f.g.AddEdge(c.id, a2.id);
                f.anchorLocalId = c.id;
                return f;
            }, weight: 1f);

            // C -> C hub with 3 A spokes
            AddRule(NodeLabel.C, (old, R) =>
            {
                var f = new Fragment();
                var c = f.g.AddNode(NodeLabel.C, RadiusFor(NodeLabel.C));
                var a1 = f.g.AddNode(NodeLabel.A, RadiusFor(NodeLabel.A));
                var a2 = f.g.AddNode(NodeLabel.A, RadiusFor(NodeLabel.A));
                var a3 = f.g.AddNode(NodeLabel.A, RadiusFor(NodeLabel.A));
                f.g.AddEdge(c.id, a1.id);
                f.g.AddEdge(c.id, a2.id);
                f.g.AddEdge(c.id, a3.id);
                f.anchorLocalId = c.id;
                return f;
            }, weight: 1f);
        }
    }
}


