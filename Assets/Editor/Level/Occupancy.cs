using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Control de espacio al amueblar: qué casillas bloquea cada mueble y cuáles deben quedar libres
/// (puertas y sus accesos, frente de terminales y de ADA, fragmentos, inicio). Un mueble solo se
/// coloca si todo lo reservado sigue siendo alcanzable desde el punto de inicio.
/// </summary>
public sealed class Occupancy
{
    static readonly Vector2Int[] Dirs = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

    readonly LevelLayout map;
    readonly Vector2Int spawn;
    readonly HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
    readonly HashSet<Vector2Int> reserved = new HashSet<Vector2Int>();
    readonly HashSet<Vector2Int> wallUsed = new HashSet<Vector2Int>();

    public Occupancy(LevelLayout map)
    {
        this.map = map;
        spawn = new Vector2Int(map.Data.spawn.x, map.Data.spawn.y);
        Reserve(spawn);
        foreach (var d in Dirs) Reserve(spawn + d);
        foreach (var group in LabTilemaps.DoorGroups(map))
            foreach (var p in group)
            {
                Reserve(p);
                foreach (var d in Dirs)
                {
                    Reserve(p + d);
                    Reserve(p + d * 2);
                }
            }
    }

    public int BlockedCount => blocked.Count;

    public void Reserve(Vector2Int cell)
    {
        if (map.IsWalkable(cell.x, cell.y)) reserved.Add(cell);
    }

    public void Block(Vector2Int cell) => blocked.Add(cell);

    public bool IsWallFree(Vector2Int cell) => !wallUsed.Contains(cell) && WallAutotile.PieceAt(map, cell.x, cell.y) == WallPiece.Face;

    public void UseWall(IEnumerable<Vector2Int> cells)
    {
        foreach (var c in cells) wallUsed.Add(c);
    }

    /// <summary>Intenta ocupar las casillas; se deshace si rompe el acceso a algo reservado.</summary>
    public bool TryOccupy(IReadOnlyCollection<Vector2Int> cells)
    {
        foreach (var c in cells)
        {
            if (!map.IsWalkable(c.x, c.y) || map.KindAt(c.x, c.y) == CellKind.Door) return false;
            if (reserved.Contains(c) || blocked.Contains(c)) return false;
        }
        foreach (var c in cells) blocked.Add(c);
        if (ReservedReachable() && !CreatesSqueeze(cells)) return true;
        foreach (var c in cells) blocked.Remove(c);
        return false;
    }

    bool Free(Vector2Int c) => map.IsWalkable(c.x, c.y) && !blocked.Contains(c);

    bool ReservedReachable()
    {
        var seen = new HashSet<Vector2Int> { spawn };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(spawn);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            foreach (var d in Dirs)
            {
                var n = p + d;
                if (seen.Contains(n) || !Free(n)) continue;
                seen.Add(n);
                queue.Enqueue(n);
            }
        }
        return reserved.All(seen.Contains);
    }

    /// <summary>
    /// Rechaza el mueble si deja a su lado una casilla libre encajonada en los dos ejes (sin dos casillas libres
    /// seguidas ni en horizontal ni en vertical): un rincón donde Alex se atasca. Los pasillos de una casilla
    /// de ancho se permiten (los pies de Alex miden 0,6 unidades y caben).
    /// </summary>
    bool CreatesSqueeze(IReadOnlyCollection<Vector2Int> cells)
    {
        var around = cells.SelectMany(c => new[] { c + Vector2Int.left, c + Vector2Int.right, c + Vector2Int.up, c + Vector2Int.down })
                          .Where(Free).Distinct();
        foreach (var c in around)
        {
            int h = LevelLayoutValidator.Span(map, c.x, c.y, 1, 0, (x, y) => Free(new Vector2Int(x, y)));
            int v = LevelLayoutValidator.Span(map, c.x, c.y, 0, 1, (x, y) => Free(new Vector2Int(x, y)));
            // Una casilla libre encajonada en las dos direcciones es un rincón donde Alex se atasca.
            if (h < 2 && v < 2) return true;
        }
        return false;
    }
}
