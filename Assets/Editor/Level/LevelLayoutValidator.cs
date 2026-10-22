using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Reglas que debe cumplir el mapa para que no haya zonas inaccesibles, pasillos estrechos
/// ni salas sin puerta. Las mismas reglas se usaron al diseñarlo; la prueba de EditMode
/// vuelve a comprobarlas cada vez que alguien edita lab_layout.json.
/// </summary>
public static class LevelLayoutValidator
{
    public const int MinSpan = 3;
    static readonly int[] TerminalsPerZone = { 0, 2, 2, 2, 2, 1, 1, 1 };
    static readonly Vector2Int[] Dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

    public static readonly string[] FloorDecor =
        { "desk", "chair", "server_rack", "counter", "cabinet", "pod", "sofa", "bed", "vending", "table", "console", "crate", "lamp", "beanbag" };

    public static readonly string[] WallDecor = { "wall_screen", "neon_sign", "wall_panel", "window_panel" };

    public static List<string> Validate(LevelLayout map)
    {
        var errors = new List<string>();
        CheckCells(map, errors);
        CheckZones(map, errors);
        CheckDoors(map, errors);
        var blocked = CheckObjects(map, errors);
        CheckConnectivity(map, blocked, errors);
        return errors.Distinct().Take(40).ToList();
    }

    static void CheckCells(LevelLayout map, List<string> errors)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                char c = map.CharAt(x, y);
                if ("#.D 1234567".IndexOf(c) < 0) errors.Add($"carácter inválido '{c}' en ({x},{y})");
                if (!map.IsWalkable(x, y)) continue;
                if (x == 0 || y == 0 || x == map.Width - 1 || y == map.Height - 1)
                    errors.Add($"casilla transitable en el borde del mapa ({x},{y})");
                foreach (var d in Dirs)
                {
                    var n = map.KindAt(x + d.x, y + d.y);
                    if (n == CellKind.Void) errors.Add($"({x},{y}) toca el exterior; ciérrala con '#'");
                    int zone = map.ZoneAt(x, y);
                    if (zone > 0 && n == CellKind.Hub) errors.Add($"la sala {zone} toca el hub sin puerta en ({x},{y})");
                    int other = map.ZoneAt(x + d.x, y + d.y);
                    if (zone > 0 && other > 0 && other != zone) errors.Add($"la sala {zone} toca la sala {other} en ({x},{y})");
                }
                if (map.KindAt(x, y) != CellKind.Door
                    && Span(map, x, y, 1, 0, map.IsWalkable) < MinSpan && Span(map, x, y, 0, 1, map.IsWalkable) < MinSpan)
                    errors.Add($"paso estrecho en ({x},{y}): menos de {MinSpan} casillas de ancho");
            }
    }

    static void CheckZones(LevelLayout map, List<string> errors)
    {
        for (int zone = 1; zone <= 7; zone++)
        {
            var cells = AllCells(map).Where(p => map.ZoneAt(p.x, p.y) == zone).ToList();
            if (cells.Count == 0) { errors.Add($"falta la sala {zone}"); continue; }
            var reach = Flood(map, cells[0], p => map.ZoneAt(p.x, p.y) == zone);
            if (reach.Count != cells.Count) errors.Add($"la sala {zone} está partida en varias zonas");
            int w = cells.Max(p => p.x) - cells.Min(p => p.x) + 1;
            int h = cells.Max(p => p.y) - cells.Min(p => p.y) + 1;
            if (w < 7 || h < 5) errors.Add($"la sala {zone} mide {w}x{h}; mínimo 7x5");
        }
    }

    static void CheckDoors(LevelLayout map, List<string> errors)
    {
        var doors = AllCells(map).Where(p => map.KindAt(p.x, p.y) == CellKind.Door).ToList();
        bool IsDoor(int x, int y) => map.KindAt(x, y) == CellKind.Door;
        foreach (var p in doors)
        {
            int run = Mathf.Max(Span(map, p.x, p.y, 1, 0, IsDoor), Span(map, p.x, p.y, 0, 1, IsDoor));
            if (run < MinSpan) errors.Add($"puerta de menos de {MinSpan} casillas en ({p.x},{p.y})");
        }
        var connected = new HashSet<int>();
        var seen = new HashSet<Vector2Int>();
        foreach (var p in doors)
        {
            if (seen.Contains(p)) continue;
            var group = Flood(map, p, q => map.KindAt(q.x, q.y) == CellKind.Door);
            seen.UnionWith(group);
            bool hub = group.Any(q => Dirs.Any(d => map.KindAt(q.x + d.x, q.y + d.y) == CellKind.Hub));
            if (!hub) continue;
            foreach (var q in group)
                foreach (var d in Dirs)
                {
                    int zone = map.ZoneAt(q.x + d.x, q.y + d.y);
                    if (zone > 0) connected.Add(zone);
                }
        }
        for (int zone = 1; zone <= 7; zone++)
            if (!connected.Contains(zone)) errors.Add($"la sala {zone} no tiene una puerta (D) hacia el hub");
    }

    /// <summary>Comprueba objetos y devuelve las casillas que ocupan (obstáculos).</summary>
    static HashSet<Vector2Int> CheckObjects(LevelLayout map, List<string> errors)
    {
        var data = map.Data;
        var blocked = new HashSet<Vector2Int>();
        if (data.spawn == null || map.KindAt(data.spawn.x, data.spawn.y) != CellKind.Hub)
            errors.Add("el punto de inicio debe estar en el hub");
        var ada = data.ada;
        if (ada == null || map.KindAt(ada.x, ada.y) != CellKind.Hub || map.KindAt(ada.x + 1, ada.y) != CellKind.Hub)
            errors.Add("la consola de ADA necesita dos casillas del hub");
        else
        {
            if (!map.IsWalkable(ada.x, ada.y + 1) || !map.IsWalkable(ada.x + 1, ada.y + 1))
                errors.Add("la consola de ADA necesita suelo libre al sur");
            blocked.Add(new Vector2Int(ada.x, ada.y));
            blocked.Add(new Vector2Int(ada.x + 1, ada.y));
        }

        var counts = new int[8];
        foreach (var t in data.terminals ?? new LayoutTerminal[0])
        {
            if (t.zone < 1 || t.zone > 7) { errors.Add($"terminal con sala inválida {t.zone}"); continue; }
            counts[t.zone]++;
            if (map.ZoneAt(t.x, t.y) != t.zone) errors.Add($"la terminal de la sala {t.zone} en ({t.x},{t.y}) no está dentro de su sala");
            if (map.KindAt(t.x, t.y - 1) != CellKind.Wall) errors.Add($"la terminal ({t.x},{t.y}) debe apoyarse en la pared norte");
            if (!map.IsWalkable(t.x, t.y + 1)) errors.Add($"la terminal ({t.x},{t.y}) necesita suelo libre al sur");
            if (!blocked.Add(new Vector2Int(t.x, t.y))) errors.Add($"la terminal ({t.x},{t.y}) se superpone con otro objeto");
        }
        for (int zone = 1; zone <= 7; zone++)
            if (counts[zone] != TerminalsPerZone[zone])
                errors.Add($"la sala {zone} necesita {TerminalsPerZone[zone]} terminal(es) y tiene {counts[zone]}");

        foreach (var f in data.fragments ?? new LayoutPoint[0])
        {
            var kind = map.KindAt(f.x, f.y);
            if (!map.IsWalkable(f.x, f.y) || kind == CellKind.Door) errors.Add($"fragmento ({f.x},{f.y}) fuera del suelo");
            if (blocked.Contains(new Vector2Int(f.x, f.y))) errors.Add($"fragmento ({f.x},{f.y}) encima de un objeto");
        }
        foreach (var d in data.decor ?? new LayoutDecor[0])
        {
            if (FloorDecor.Contains(d.kind))
            {
                if (!map.IsWalkable(d.x, d.y) || map.KindAt(d.x, d.y) == CellKind.Door)
                    errors.Add($"decorado '{d.kind}' en ({d.x},{d.y}) fuera del suelo o en una puerta");
                else blocked.Add(new Vector2Int(d.x, d.y));
            }
            else if (!WallDecor.Contains(d.kind)) errors.Add($"tipo de decorado desconocido '{d.kind}'");
        }
        return blocked;
    }

    static void CheckConnectivity(LevelLayout map, HashSet<Vector2Int> blocked, List<string> errors)
    {
        var data = map.Data;
        if (data.spawn == null || map.KindAt(data.spawn.x, data.spawn.y) != CellKind.Hub) return;
        var start = new Vector2Int(data.spawn.x, data.spawn.y);
        var plain = Flood(map, start, p => map.IsWalkable(p.x, p.y));
        int walkable = AllCells(map).Count(p => map.IsWalkable(p.x, p.y));
        if (plain.Count != walkable) errors.Add($"{walkable - plain.Count} casillas transitables son inalcanzables desde el inicio");

        var withObjects = Flood(map, start, p => map.IsWalkable(p.x, p.y) && !blocked.Contains(p));
        var required = new List<(Vector2Int cell, string what)>();
        required.AddRange(AllCells(map).Where(p => map.KindAt(p.x, p.y) == CellKind.Door).Select(p => (p, "puerta")));
        required.AddRange((data.fragments ?? new LayoutPoint[0]).Select(f => (new Vector2Int(f.x, f.y), "fragmento")));
        required.AddRange((data.terminals ?? new LayoutTerminal[0]).Select(t => (new Vector2Int(t.x, t.y + 1), $"frente de la terminal ({t.x},{t.y})")));
        if (data.ada != null)
        {
            required.Add((new Vector2Int(data.ada.x, data.ada.y + 1), "frente de ADA"));
            required.Add((new Vector2Int(data.ada.x + 1, data.ada.y + 1), "frente de ADA"));
        }
        foreach (var (cell, what) in required)
            if (!withObjects.Contains(cell)) errors.Add($"{what} en ({cell.x},{cell.y}) queda inalcanzable con los objetos colocados");
    }

    static IEnumerable<Vector2Int> AllCells(LevelLayout map)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                yield return new Vector2Int(x, y);
    }

    public static int Span(LevelLayout map, int x, int y, int dx, int dy, System.Func<int, int, bool> ok)
    {
        int n = 1;
        for (int s = 1; ok(x + dx * s, y + dy * s); s++) n++;
        for (int s = 1; ok(x - dx * s, y - dy * s); s++) n++;
        return n;
    }

    static HashSet<Vector2Int> Flood(LevelLayout map, Vector2Int start, System.Func<Vector2Int, bool> passable)
    {
        var seen = new HashSet<Vector2Int> { start };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            foreach (var d in Dirs)
            {
                var n = p + d;
                if (!map.InBounds(n.x, n.y) || seen.Contains(n) || !passable(n)) continue;
                seen.Add(n);
                queue.Enqueue(n);
            }
        }
        return seen;
    }
}
