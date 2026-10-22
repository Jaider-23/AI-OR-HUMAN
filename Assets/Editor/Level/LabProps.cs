using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Objetos del laboratorio: terminales de casos (pantallas de pared), estación de ADA, fragmentos
/// y decorado. Cada mueble de suelo usa como collider la huella real de su base (catálogo) y solo se
/// coloca si no bloquea puertas, terminales, ADA, fragmentos ni el inicio (<see cref="Occupancy"/>).
/// </summary>
public static class LabProps
{
    const float Px = 1f / 16f;
    /// <summary>
    /// Pantallas y decorado de pared se ordenan por profundidad con Alex (orden 0, punto = pivote en la base
    /// de la pared): delante de la pared él se dibuja encima; detrás de ella (en la sala de arriba), debajo.
    /// </summary>
    const int WallDecorOrder = 0;
    const int TerminalOrder = 0;
    const float HoloMarkerHeight = 2.3f;
    /// <summary>Base del poste de la farola (sprite 50): 4 px de ancho (x 3-6) y 3 px de profundidad.</summary>
    static readonly Vector2 LampBaseSize = new Vector2(4f / 16f, 3f / 16f);

    /// <summary>Desplazamientos que se prueban si un mueble no cabe en la casilla pedida por el mapa.</summary>
    static readonly int[] WallNudges = { 0, -1, 1 };
    static readonly Vector2Int[] FloorNudges =
    {
        Vector2Int.zero, Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down
    };

    public sealed class Result
    {
        public CaseTerminal[] Terminals;
        public DataFragment[] Fragments;
        public int DecorPlaced;
        public int DecorSkipped;
    }

    public static Result Build(LevelLayout map, CaseData[] cases, ArtLibrary art, PropCatalog catalog, Transform root)
    {
        var data = map.Data;
        var occupancy = new Occupancy(map);
        foreach (var t in data.terminals)
        {
            occupancy.Reserve(new Vector2Int(t.x, t.y));
            occupancy.Reserve(new Vector2Int(t.x, t.y + 1));
        }
        occupancy.Block(new Vector2Int(data.ada.x, data.ada.y));
        occupancy.Block(new Vector2Int(data.ada.x + 1, data.ada.y));
        occupancy.Reserve(new Vector2Int(data.ada.x, data.ada.y + 1));
        occupancy.Reserve(new Vector2Int(data.ada.x + 1, data.ada.y + 1));
        foreach (var f in data.fragments) occupancy.Reserve(new Vector2Int(f.x, f.y));

        var result = new Result
        {
            Terminals = BuildTerminals(map, cases, art, catalog, occupancy, Child(root, "Terminals")),
            Fragments = BuildFragments(map, art, catalog, Child(root, "Fragments")),
        };
        BuildAdaStation(map, art, catalog, Child(root, "ADA"));

        var decor = Child(root, "Decor");
        foreach (var d in data.decor ?? new LayoutDecor[0])
        {
            if (PlaceDecor(map, d.kind, d.x, d.y, art, catalog, occupancy, decor))
            {
                result.DecorPlaced++;
                continue;
            }
            result.DecorSkipped++;
            Debug.LogWarning($"[LabProps] Decorado '{d.kind}' en ({d.x},{d.y}) descartado: no cabe sin bloquear un paso, una puerta o algo reservado");
        }
        result.DecorPlaced += AutoDressWalls(map, art, catalog, occupancy, decor);
        return result;
    }

    static Transform Child(Transform parent, string name)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    static SpriteRenderer SpriteChild(Transform parent, string name, Sprite sprite, Vector2 local, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = local;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        return renderer;
    }

    // ------------------------------------------------------------------ terminales
    static CaseTerminal[] BuildTerminals(LevelLayout map, CaseData[] cases, ArtLibrary art, PropCatalog catalog,
                                         Occupancy occupancy, Transform parent)
    {
        var ordered = cases.Where(c => c != null).OrderBy(c => c.level).ToList();
        var free = map.Data.terminals.OrderBy(t => t.zone).ThenBy(t => t.x).ToList();
        var holoController = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimationBuilder.HoloController);
        var holoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BuildPaths.HoloF0);
        var result = new List<CaseTerminal>();
        for (int i = 0; i < ordered.Count; i++)
        {
            var data = ordered[i];
            // Estricto: si falta una terminal en la sala del caso, el juego no se podría terminar.
            var slot = free.FirstOrDefault(s => s.zone == data.level)
                ?? throw new System.InvalidOperationException(
                    $"lab_layout.json no tiene una terminal libre en la sala {data.level} para el caso {data.caseId}");
            free.Remove(slot);

            // La pantalla cuelga de la cara de la pared que está al norte de la casilla de la terminal.
            var go = new GameObject($"Terminal_{data.caseId}");
            go.transform.SetParent(parent, false);
            go.transform.position = map.ToWorldBase(slot.x, slot.y - 1);
            var screen = SpriteChild(go.transform, "Screen", catalog.SpriteOf(data.level == 7 ? 2 : 3), new Vector2(0f, Px), TerminalOrder);
            var glow = LabAmbience.Glow(go.transform, "Glow", art, Theme.Cyan, 0.5f, 1.3f, -6);
            glow.transform.localPosition = new Vector3(0f, 0.95f, 0f);

            var holo = SpriteChild(go.transform, "Holo", holoSprite, new Vector2(0f, HoloMarkerHeight), 20);
            holo.gameObject.AddComponent<Animator>().runtimeAnimatorController = holoController;
            holo.gameObject.SetActive(false);

            var terminal = go.AddComponent<CaseTerminal>();
            terminal.caseIndex = i;
            terminal.body = screen;
            terminal.glow = glow;
            terminal.activeMarker = holo.transform;
            terminal.interactRadius = 1.6f;
            terminal.frontOnly = true;
            terminal.focusOffset = new Vector2(0f, -0.35f);
            occupancy.UseWall(new[] { new Vector2Int(slot.x - 1, slot.y - 1), new Vector2Int(slot.x, slot.y - 1), new Vector2Int(slot.x + 1, slot.y - 1) });
            result.Add(terminal);
        }
        return result.ToArray();
    }

    // ------------------------------------------------------------------ ADA
    static void BuildAdaStation(LevelLayout map, ArtLibrary art, PropCatalog catalog, Transform parent)
    {
        var ada = map.Data.ada;
        var basePosition = map.ToWorldBase(ada.x, ada.y) + Vector2.right * 0.5f;
        var station = new GameObject("ADA_Console");
        station.transform.SetParent(parent, false);
        station.transform.position = basePosition;
        station.AddComponent<SortingGroup>();
        SpriteChild(station.transform, "Table", catalog.SpriteOf(6), Vector2.zero, 0);
        catalog.AddFootprint(station, 6);
        SpriteChild(station.transform, "Monitor", catalog.SpriteOf(21), new Vector2(0f, 9 * Px), 1);
        SpriteChild(station.transform, "Keyboard", catalog.SpriteOf(28), new Vector2(-0.45f, 8 * Px), 2);
        var console = station.AddComponent<AdaConsole>();
        console.interactRadius = 1.8f;

        var hologram = SpriteChild(parent, "ADA_Hologram", art.AdaHologram, basePosition + new Vector2(0f, 1.45f), 25);
        hologram.gameObject.AddComponent<HoverBob>().amplitudePixels = 2f;
        var flicker = hologram.gameObject.AddComponent<FlickerLight>();
        flicker.mode = LightMode.Flicker;
        flicker.baseAlpha = 0.9f;
        flicker.amplitude = 0.2f;
        var halo = LabAmbience.Glow(parent, "ADA_Halo", art, Theme.Cyan, 0.35f, 2.6f, 24);
        halo.transform.position = basePosition + new Vector2(0f, 2.1f);
        var floor = LabAmbience.Glow(parent, "ADA_FloorLight", art, Theme.Cyan, 0.22f, 2.4f, -16);
        floor.transform.position = basePosition + new Vector2(0f, 0.2f);
    }

    // ------------------------------------------------------------------ fragmentos
    static DataFragment[] BuildFragments(LevelLayout map, ArtLibrary art, PropCatalog catalog, Transform parent)
    {
        var result = new List<DataFragment>();
        var fragments = map.Data.fragments;
        for (int i = 0; i < fragments.Length; i++)
        {
            var go = new GameObject($"DataFragment_{i + 1}");
            go.transform.SetParent(parent, false);
            go.transform.position = map.ToWorldCenter(fragments[i].x, fragments[i].y);
            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.42f;
            var shadow = SpriteChild(go.transform, "Shadow", art.BlobShadow, new Vector2(0f, -0.3f), -6);
            shadow.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            var visual = SpriteChild(go.transform, "Visual", catalog.SpriteOf(1), new Vector2(0f, -4 * Px), 0);
            var glow = LabAmbience.Glow(go.transform, "Glow", art, Theme.Cyan, 0.45f, 0.9f, 1);
            var fragment = go.AddComponent<DataFragment>();
            fragment.visual = visual.transform;
            fragment.glow = glow;
            result.Add(fragment);
        }
        return result.ToArray();
    }

    // ------------------------------------------------------------------ decorado
    static bool PlaceDecor(LevelLayout map, string kind, int x, int y, ArtLibrary art, PropCatalog catalog,
                           Occupancy occupancy, Transform parent)
    {
        if (!PropCatalog.KindSprites.TryGetValue(kind, out var candidates)) return false;
        int start = ((x * 31 + y * 17) & 0x7fffffff) % candidates.Length;
        var rotated = candidates.Skip(start).Concat(candidates.Take(start)).ToList();
        if (catalog.IsWallKind(kind))
        {
            int row = map.KindAt(x, y) == CellKind.Wall ? y : y - 1;
            // Si no cabe justo ahí, se prueba desplazándolo una casilla a cada lado de la pared.
            return WallNudges.Any(dx => rotated.Any(id => TryPlaceWall(map, id, x + dx, row, art, catalog, occupancy, parent)));
        }
        return FloorNudges.Any(d => rotated.Any(id => TryPlaceFloor(map, id, x + d.x, y + d.y, art, catalog, occupancy, parent)));
    }

    static bool TryPlaceWall(LevelLayout map, int id, int x, int y, ArtLibrary art, PropCatalog catalog,
                             Occupancy occupancy, Transform parent)
    {
        var sprite = catalog.SpriteOf(id);
        if (sprite == null) return false;
        int width = catalog.WidthInCells(id);
        int x0 = x - (width - 1) / 2;
        var cells = Enumerable.Range(x0, width).Select(cx => new Vector2Int(cx, y)).ToList();
        if (!cells.All(occupancy.IsWallFree)) return false;
        // Piezas altas solo donde por encima no hay otra sala (para no "invadir" su suelo).
        if (sprite.rect.height > 22 && cells.Any(c => map.IsWalkable(c.x, c.y - 1))) return false;

        float centerX = (map.ToCell(x0, y).x + map.ToCell(x0 + width - 1, y).x + 1) * 0.5f;
        var position = new Vector2(centerX, map.ToCell(x, y).y + Px);
        var renderer = SpriteChild(parent, $"Wall_{id}", sprite, position, WallDecorOrder);
        var glowColor = LuminousColor(id);
        if (glowColor.HasValue)
        {
            var glow = LabAmbience.Glow(renderer.transform, "Glow", art, glowColor.Value, 0.28f, Mathf.Max(1.2f, width * 0.8f), -7);
            glow.transform.localPosition = new Vector3(0f, sprite.rect.height * 0.5f * Px, 0f);
            var flicker = glow.gameObject.AddComponent<FlickerLight>();
            flicker.mode = id >= 51 && id <= 54 ? LightMode.Flicker : LightMode.Breathe;
            flicker.baseAlpha = 0.28f;
        }
        occupancy.UseWall(cells);
        return true;
    }

    static Color? LuminousColor(int id)
    {
        switch (id)
        {
            case 7: case 18: case 19: case 36: return Theme.Magenta;
            case 11: case 50: case 51: case 52: case 53: case 54: case 72: return Theme.Orange;
            case 4: case 9: case 12: case 15: case 26: case 27: case 71: return Theme.Cyan;
            default: return null;
        }
    }

    static bool TryPlaceFloor(LevelLayout map, int id, int x, int y, ArtLibrary art, PropCatalog catalog,
                              Occupancy occupancy, Transform parent)
    {
        var sprite = catalog.SpriteOf(id);
        if (sprite == null) return false;
        int width = catalog.WidthInCells(id);
        var basePosition = map.ToWorldBase(x, y) + (width % 2 == 0 ? Vector2.right * 0.5f : Vector2.zero);
        var footprint = catalog.FootprintRect(id) ?? new Rect(-0.4f, 0f, 0.8f, 0.5f);
        var world = new Rect(basePosition + footprint.position, footprint.size);
        var cells = CellsOverlapping(map, world, x, y);
        if (cells.Count == 0 || !occupancy.TryOccupy(cells)) return false;

        // El origen del objeto va en la fila donde la base toca el suelo: así se ordena con Alex por donde
        // realmente choca (en muebles con patas o puertas abiertas abajo, esa fila está unos píxeles más arriba).
        // El SortingGroup hace que el mueble y lo que lleva encima (monitor, LED...) se ordenen como uno solo.
        float originY = catalog.BaseRow(id) * Px;
        var go = new GameObject($"Prop_{id}");
        go.transform.SetParent(parent, false);
        go.transform.position = basePosition + Vector2.up * originY;
        go.AddComponent<SortingGroup>();
        SpriteChild(go.transform, "Sprite", sprite, Vector2.down * originY, 0);
        catalog.AddFootprint(go, id, originY);
        if (PropCatalog.DeskLike.Contains(id)) AddDeskItems(go.transform, id, sprite, catalog, x + y, originY);
        if (id == PropCatalog.RackA || id == PropCatalog.RackB) AddRackLeds(go.transform, art, id, originY);
        if (id == 50)
        {
            // Farola de suelo: el catálogo la registra como lámpara de pared (sin huella), pero de pie en el
            // suelo debe chocar por su base estrecha para no poder atravesarla.
            var post = go.AddComponent<BoxCollider2D>();
            post.size = new Vector2(LampBaseSize.x, LampBaseSize.y);
            post.offset = new Vector2(0f, LampBaseSize.y * 0.5f);
            // El brillo de la lámpara va fuera del grupo para iluminar por encima de quien pase al lado.
            var lamp = LabAmbience.Glow(parent, "LampGlow", art, Theme.Orange, 0.35f, 1.6f, 22);
            lamp.transform.position = basePosition + Vector2.up * (sprite.rect.height * Px - 0.2f);
        }
        return true;
    }

    static List<Vector2Int> CellsOverlapping(LevelLayout map, Rect world, int ax, int ay)
    {
        var inner = new Rect(world.x + 0.02f, world.y + 0.02f, world.width - 0.04f, world.height - 0.04f);
        var cells = new List<Vector2Int>();
        for (int cy = ay - 3; cy <= ay + 3; cy++)
            for (int cx = ax - 5; cx <= ax + 5; cx++)
            {
                var c = map.ToCell(cx, cy);
                if (new Rect(c.x, c.y, 1f, 1f).Overlaps(inner)) cells.Add(new Vector2Int(cx, cy));
            }
        return cells;
    }

    static void AddDeskItems(Transform desk, int id, Sprite sprite, PropCatalog catalog, int seed, float originY)
    {
        var footprint = catalog.FootprintRect(id);
        float depthPx = footprint.HasValue ? footprint.Value.height * 16f : 8f;
        float topY = (sprite.rect.height - depthPx + 2) * Px - originY;
        bool wide = sprite.rect.width >= 32;
        // Mesas anchas: monitor holográfico; consolas estrechas: solo teclado (un monitor ancho no cabe).
        int main = wide ? 21 : 28;
        SpriteChild(desk, wide ? "Monitor" : "Keyboard", catalog.SpriteOf(main), new Vector2(wide ? 0f : -0.2f, topY), 1);
        int small = PropCatalog.DeskItems[(seed & 0x7fffffff) % PropCatalog.DeskItems.Length];
        if (small != main && small != 21) SpriteChild(desk, "Item", catalog.SpriteOf(small), new Vector2(sprite.rect.width * 0.28f * Px, topY - Px), 2);
    }

    static void AddRackLeds(Transform rack, ArtLibrary art, int id, float originY)
    {
        Color[] colors = id == PropCatalog.RackA
            ? new[] { Theme.Hex("46F28C"), Theme.Cyan, Theme.Hex("46F28C") }
            : new[] { Theme.Orange, Theme.Cyan, Theme.Hex("FF4D6A") };
        for (int i = 0; i < PropArt.RackLeds.Length; i++)
        {
            var led = PropArt.RackLeds[i];
            var glow = LabAmbience.Glow(rack, "Led", art, colors[i], 0.7f, 0.22f, 3);
            glow.transform.localPosition = new Vector3((led.x + 0.5f - PropArt.RackWidth * 0.5f) * Px, (led.y + 0.5f) * Px - originY, 0f);
            var blink = glow.gameObject.AddComponent<FlickerLight>();
            blink.mode = LightMode.Blink;
            blink.baseAlpha = 0.75f;
            blink.speed = 1f + i * 0.4f;
        }
    }

    /// <summary>Viste las paredes largas que quedaron desnudas con pantallas, neones y ventanas.</summary>
    static int AutoDressWalls(LevelLayout map, ArtLibrary art, PropCatalog catalog, Occupancy occupancy, Transform parent)
    {
        string[] rotation = { "wall_screen", "neon_sign", "window_panel", "wall_panel" };
        int placed = 0, turn = 0;
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                if ((x + y * 3) % 7 != 0 || !occupancy.IsWallFree(new Vector2Int(x, y))) continue;
                if (!occupancy.IsWallFree(new Vector2Int(x - 1, y)) || !occupancy.IsWallFree(new Vector2Int(x + 1, y))) continue;
                string kind = rotation[turn++ % rotation.Length];
                if (PlaceDecor(map, kind, x, y, art, catalog, occupancy, parent)) placed++;
            }
        return placed;
    }
}
