using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Rejilla de navegación para las pruebas: marca como libre cada punto donde cabe el collider de los
/// pies de Alex (consulta real a Physics2D) y busca caminos con BFS de 8 vecinos sin cortar esquinas.
/// </summary>
public sealed class NavGrid
{
    readonly float step;
    readonly Vector2 origin;
    readonly int width;
    readonly int height;
    readonly bool[] free;

    public NavGrid(Collider2D feet, Bounds area, float step)
    {
        this.step = step;
        origin = area.min;
        width = Mathf.CeilToInt(area.size.x / step);
        height = Mathf.CeilToInt(area.size.y / step);
        free = new bool[width * height];
        var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true };
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(feet.gameObject.layer));
        var hits = new Collider2D[8];
        var box = feet as BoxCollider2D;
        var size = box != null ? box.size + Vector2.one * (2f * box.edgeRadius) : (Vector2)feet.bounds.size;
        var offset = feet.offset;
        for (int j = 0; j < height; j++)
            for (int i = 0; i < width; i++)
            {
                int n = Physics2D.OverlapBox(Position(i, j) + offset, size + Vector2.one * 0.04f, 0f, filter, hits);
                free[j * width + i] = hits.Take(n).All(h => h == feet);
            }
    }

    public Vector2 Position(int i, int j) => origin + new Vector2((i + 0.5f) * step, (j + 0.5f) * step);

    Vector2Int CellOf(Vector2 p) => new Vector2Int(Mathf.FloorToInt((p.x - origin.x) / step), Mathf.FloorToInt((p.y - origin.y) / step));

    bool Free(int i, int j) => i >= 0 && j >= 0 && i < width && j < height && free[j * width + i];

    /// <summary>Camino (puntos de paso) desde start hasta la casilla libre más cercana a target dentro de radius.</summary>
    public List<Vector2> FindPath(Vector2 start, Vector2 target, float radius, System.Func<Vector2, bool> accept = null)
    {
        var from = NearestFree(CellOf(start));
        if (from == null) return null;
        var previous = new Dictionary<Vector2Int, Vector2Int>();
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(from.Value);
        previous[from.Value] = from.Value;
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            var here = Position(c.x, c.y);
            if (Vector2.Distance(here, target) <= radius && (accept == null || accept(here))) return Rebuild(previous, c);
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var n = new Vector2Int(c.x + dx, c.y + dy);
                    if (previous.ContainsKey(n) || !Free(n.x, n.y)) continue;
                    if (dx != 0 && dy != 0 && (!Free(c.x + dx, c.y) || !Free(c.x, c.y + dy))) continue;
                    previous[n] = c;
                    queue.Enqueue(n);
                }
        }
        return null;
    }

    Vector2Int? NearestFree(Vector2Int c)
    {
        for (int r = 0; r < 6; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                    if (Free(c.x + dx, c.y + dy)) return new Vector2Int(c.x + dx, c.y + dy);
        return null;
    }

    List<Vector2> Rebuild(Dictionary<Vector2Int, Vector2Int> previous, Vector2Int end)
    {
        var cells = new List<Vector2Int> { end };
        while (previous[cells[cells.Count - 1]] != cells[cells.Count - 1]) cells.Add(previous[cells[cells.Count - 1]]);
        cells.Reverse();
        // Se conservan solo los puntos donde cambia la dirección.
        var points = new List<Vector2>();
        for (int k = 0; k < cells.Count; k++)
        {
            bool turn = k == 0 || k == cells.Count - 1 || cells[k] - cells[k - 1] != cells[k + 1] - cells[k];
            if (turn) points.Add(Position(cells[k].x, cells[k].y));
        }
        return points;
    }
}
