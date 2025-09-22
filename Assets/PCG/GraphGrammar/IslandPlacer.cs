using System;
using System.Collections.Generic;
using UnityEngine;

namespace GG
{
    /// Places islands (GraphSpec nodes) in 2D so their circles (radius + gap) don't overlap.
    /// Deterministic given the seed and parameters.
    public class IslandPlacer
    {
        [Header("Placement")]
        public float gap = 2f;                 // extra spacing between island circles
        public int angleSamples = 16;          // candidate angles per ring
        public int ringTries = 4;              // how many times to increase radius if no angle fits
        public float ringStep = 3f;            // extra distance per ring when previous ring fails
        public bool useGoldenAngle = false;    // if true, use golden-angle spiral instead of uniform angles

        System.Random rng;

        public IslandPlacer(int seed)
        {
            rng = new System.Random(seed);
        }

        /// Place all nodes from a GraphSpec with non-overlap. Returns positions by spec node id.
        public Dictionary<int, Vector2> Place(GraphSpec spec)
        {
            var pos = new Dictionary<int, Vector2>(spec.nodes.Count);
            if (spec.nodes.Count == 0) return pos;

            // Build adjacency for ordering (simple undirected graph)
            var adj = new List<List<int>>(spec.nodes.Count);
            for (int i = 0; i < spec.nodes.Count; i++) adj.Add(new List<int>());
            foreach (var e in spec.edges)
            {
                if (!adj[e.u].Contains(e.v)) adj[e.u].Add(e.v);
                if (!adj[e.v].Contains(e.u)) adj[e.v].Add(e.u);
            }

            // BFS order starting at node 0 (assumes connected; if not, we’ll start new components later)
            var order = new List<int>();
            var seen = new HashSet<int>();
            var q = new Queue<int>();

            void EnqueueComponent(int start)
            {
                q.Enqueue(start);
                seen.Add(start);
                while (q.Count > 0)
                {
                    int u = q.Dequeue();
                    order.Add(u);
                    foreach (var v in adj[u])
                        if (seen.Add(v)) q.Enqueue(v);
                }
            }

            // cover all components
            for (int i = 0; i < spec.nodes.Count; i++)
                if (!seen.Contains(i)) EnqueueComponent(i);

            // Place the first node of each component near origin or offset cluster
            var componentOffset = Vector2.zero;
            int placedCountInComponent = 0;

            var placed = new HashSet<int>();
            var componentStartIndices = new HashSet<int>();
            componentStartIndices.Add(order.Count > 0 ? order[0] : -1);

            // detect component starts by seen set during BFS above
            // (simple heuristic: a node with no placed neighbors is treated as a new component)
            foreach (int u in order)
            {
                // Collect already placed neighbors
                var neighbors = adj[u].FindAll(v => placed.Contains(v));

                if (neighbors.Count == 0 && placed.Count > 0)
                {
                    // Likely a new component; nudge its cluster away from existing ones
                    componentOffset += new Vector2(1000f, 0f); // coarse separation between disconnected components
                    placedCountInComponent = 0;
                }

                var su = spec.nodes[u];
                if (placed.Count == 0 || neighbors.Count == 0)
                {
                    // First in component: place at offset (or origin)
                    pos[u] = componentOffset;
                    placed.Add(u);
                    placedCountInComponent++;
                    continue;
                }

                // Reference anchor = average of placed neighbor positions (centroid of already-placed neighbors)
                Vector2 anchor = Vector2.zero;
                float minRequiredD = 0f;
                foreach (var v in neighbors)
                {
                    anchor += pos[v];
                    float need = spec.nodes[v].radius + su.radius + gap;
                    if (need > minRequiredD) minRequiredD = need;
                }
                anchor /= neighbors.Count;

                // Try to find a valid angle & ring around the anchor that does not overlap any placed island
                bool placedOk = TryPlaceAroundAnchor(spec, u, anchor, minRequiredD, pos, out Vector2 finalPos);
                if (!placedOk)
                {
                    // Fallback: push further along a random direction until it fits
                    finalPos = FallbackPlace(spec, u, anchor, minRequiredD, pos);
                }

                pos[u] = finalPos;
                placed.Add(u);
                placedCountInComponent++;
            }

            return pos;
        }

        bool TryPlaceAroundAnchor(GraphSpec spec, int nodeId, Vector2 anchor, float baseDist, Dictionary<int, Vector2> pos, out Vector2 chosen)
        {
            chosen = default;
            var rSelf = spec.nodes[nodeId].radius;

            float golden = Mathf.Deg2Rad * 137.50776f;
            float startAngle = (float)(rng.NextDouble() * Mathf.PI * 2f);

            for (int ring = 0; ring < Mathf.Max(1, ringTries); ring++)
            {
                float R = baseDist + ring * ringStep;

                for (int k = 0; k < Mathf.Max(1, angleSamples); k++)
                {
                    float theta = useGoldenAngle
                        ? startAngle + k * golden
                        : startAngle + k * (2f * Mathf.PI / Mathf.Max(1, angleSamples));

                    var dir = new Vector2(Mathf.Cos(theta), Mathf.Sin(theta));
                    var candidate = anchor + dir * R;

                    if (Fits(spec, nodeId, candidate, pos))
                    {
                        chosen = candidate;
                        return true;
                    }
                }
            }
            return false;
        }

        Vector2 FallbackPlace(GraphSpec spec, int nodeId, Vector2 anchor, float baseDist, Dictionary<int, Vector2> pos)
        {
            // March outward along a random direction until it fits (bounded tries)
            var dirAngle = (float)(rng.NextDouble() * Mathf.PI * 2f);
            var dir = new Vector2(Mathf.Cos(dirAngle), Mathf.Sin(dirAngle));
            float R = baseDist;
            for (int t = 0; t < 256; t++)
            {
                var candidate = anchor + dir * R;
                if (Fits(spec, nodeId, candidate, pos)) return candidate;
                R += ringStep;
            }
            // Last resort: place very far
            return anchor + dir * (baseDist + 1000f);
        }

        bool Fits(GraphSpec spec, int nodeId, Vector2 candidate, Dictionary<int, Vector2> pos)
        {
            float rSelf = spec.nodes[nodeId].radius;
            foreach (var kv in pos)
            {
                int otherId = kv.Key;
                var p = kv.Value;
                float rOther = spec.nodes[otherId].radius;
                float min = rSelf + rOther + gap;
                if ((candidate - p).sqrMagnitude < min * min) return false;
            }
            return true;
        }

        /// Compute a world-space bounds rectangle that encloses all placed islands
        /// (circle envelopes) plus padding.
        public static Rect ComputeBounds(GraphSpec spec, IDictionary<int, Vector2> positions, float padding = 5f)
        {
            if (spec.nodes.Count == 0 || positions == null || positions.Count == 0)
                return new Rect(-25, -25, 50, 50);

            float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
            float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;

            foreach (var n in spec.nodes)
            {
                if (!positions.TryGetValue(n.id, out var p)) continue;
                float r = n.radius;
                minX = Mathf.Min(minX, p.x - r);
                maxX = Mathf.Max(maxX, p.x + r);
                minY = Mathf.Min(minY, p.y - r);
                maxY = Mathf.Max(maxY, p.y + r);
            }

            minX -= padding; minY -= padding;
            maxX += padding; maxY += padding;

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}
