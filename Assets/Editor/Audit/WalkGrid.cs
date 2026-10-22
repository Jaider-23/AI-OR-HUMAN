using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Prueba de solapamiento con la forma exacta del collider del jugador.</summary>
public sealed class PlayerProbe
{
    readonly Collider2D shape;
    readonly HashSet<Collider2D> own;
    readonly ContactFilter2D filter;
    readonly Vector2 scale;
    readonly Collider2D[] hits = new Collider2D[16];

    public PlayerProbe(PlayerController player)
    {
        own = new HashSet<Collider2D>(player.GetComponents<Collider2D>());
        shape = own.FirstOrDefault(c => !c.isTrigger);
        scale = player.transform.lossyScale;
        filter = new ContactFilter2D { useTriggers = false, useLayerMask = true };
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(player.gameObject.layer));
    }

    public PhysicsMaterial2D Material => shape != null ? shape.sharedMaterial : null;

    /// <summary>Devuelve el primer collider ajeno que bloquea al jugador en esa posición (o null).</summary>
    public Collider2D BlockerAt(Vector2 position)
    {
        int count;
        switch (shape)
        {
            case BoxCollider2D box:
                count = Physics2D.OverlapBox(position + Vector2.Scale(box.offset, scale),
                    Vector2.Scale(box.size + Vector2.one * (2f * box.edgeRadius), scale), 0f, filter, hits);
                break;
            case CircleCollider2D circle:
                count = Physics2D.OverlapCircle(position + Vector2.Scale(circle.offset, scale),
                    circle.radius * Mathf.Max(scale.x, scale.y), filter, hits);
                break;
            case CapsuleCollider2D capsule:
                count = Physics2D.OverlapCapsule(position + Vector2.Scale(capsule.offset, scale),
                    Vector2.Scale(capsule.size, scale), capsule.direction, 0f, filter, hits);
                break;
            default:
                count = Physics2D.OverlapPoint(position, filter, hits);
                break;
        }
        for (int i = 0; i < count; i++)
        {
            if (!own.Contains(hits[i])) return hits[i];
        }
        return null;
    }

    public override string ToString()
    {
        switch (shape)
        {
            case BoxCollider2D b: return $"Box size={b.size} edge={b.edgeRadius} offset={b.offset}";
            case CircleCollider2D c: return $"Circle r={c.radius} offset={c.offset}";
            case CapsuleCollider2D k: return $"Capsule size={k.size} offset={k.offset}";
            default: return shape != null ? shape.GetType().Name : "none";
        }
    }
}

/// <summary>Rejilla de posiciones del jugador: bloqueadas / libres / alcanzables desde el inicio.</summary>
public sealed class WalkGrid
{
    public readonly int Width;
    public readonly int Height;
    public readonly bool[] Blocked;
    readonly string[] blockers;
    readonly Vector2 origin;
    readonly float step;

    public WalkGrid(Bounds bounds, float step, PlayerProbe probe)
    {
        this.step = step;
        origin = bounds.min;
        Width = Mathf.CeilToInt(bounds.size.x / step);
        Height = Mathf.CeilToInt(bounds.size.y / step);
        Blocked = new bool[Width * Height];
        blockers = new string[Width * Height];
        for (int i = 0; i < Width; i++)
            for (int j = 0; j < Height; j++)
            {
                var hit = probe.BlockerAt(Position(i, j));
                if (hit == null) continue;
                Blocked[Index(i, j)] = true;
                blockers[Index(i, j)] = hit.transform.parent != null ? $"{hit.transform.parent.name}/{hit.name}" : hit.name;
            }
    }

    public int BlockedCount => Blocked.Count(b => b);

    public int Index(int i, int j) => j * Width + i;

    public Vector2 Position(int i, int j) => origin + new Vector2(i * step, j * step);

    public bool[] FloodFrom(Vector2 start)
    {
        var reachable = new bool[Width * Height];
        int si = Mathf.Clamp(Mathf.RoundToInt((start.x - origin.x) / step), 0, Width - 1);
        int sj = Mathf.Clamp(Mathf.RoundToInt((start.y - origin.y) / step), 0, Height - 1);
        if (Blocked[Index(si, sj)]) return reachable;

        var queue = new Queue<(int, int)>();
        queue.Enqueue((si, sj));
        reachable[Index(si, sj)] = true;
        var dirs = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
        while (queue.Count > 0)
        {
            var (i, j) = queue.Dequeue();
            foreach (var (dx, dy) in dirs)
            {
                int ni = i + dx, nj = j + dy;
                if (ni < 0 || nj < 0 || ni >= Width || nj >= Height) continue;
                int n = Index(ni, nj);
                if (reachable[n] || Blocked[n]) continue;
                reachable[n] = true;
                queue.Enqueue((ni, nj));
            }
        }
        return reachable;
    }

    public float MinReachableDistance(bool[] reachable, Vector2 target, System.Func<Vector2, bool> accept = null)
    {
        float best = float.MaxValue;
        for (int i = 0; i < Width; i++)
            for (int j = 0; j < Height; j++)
            {
                if (!reachable[Index(i, j)]) continue;
                if (accept != null && !accept(Position(i, j))) continue;
                best = Mathf.Min(best, Vector2.Distance(Position(i, j), target));
            }
        return best;
    }

    /// <summary>Colliders contra los que el jugador choca realmente (celdas bloqueadas junto a zonas alcanzables).</summary>
    public Dictionary<string, int> FrontierBlockers(bool[] reachable)
    {
        var counts = new Dictionary<string, int>();
        for (int i = 1; i < Width - 1; i++)
            for (int j = 1; j < Height - 1; j++)
            {
                int index = Index(i, j);
                if (!Blocked[index]) continue;
                bool touches = reachable[Index(i + 1, j)] || reachable[Index(i - 1, j)]
                               || reachable[Index(i, j + 1)] || reachable[Index(i, j - 1)];
                if (!touches) continue;
                counts.TryGetValue(blockers[index], out int n);
                counts[blockers[index]] = n + 1;
            }
        return counts;
    }
}
