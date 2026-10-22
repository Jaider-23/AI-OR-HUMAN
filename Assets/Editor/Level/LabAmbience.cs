using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Ambientación del laboratorio: puertas automáticas, letreros de cada sala, luces (focos del hub,
/// tono de cada sala, marcos de puerta), dron de seguridad y partículas.
/// </summary>
public static class LabAmbience
{
    public static void BuildDoors(LevelLayout map, ArtLibrary art, Transform parent)
    {
        foreach (var group in LabTilemaps.DoorGroups(map))
        {
            bool horizontal = LabTilemaps.IsHorizontalDoor(map, group[0].x, group[0].y);
            var cells = group.Select(p => map.ToWorldCenter(p.x, p.y)).ToList();
            var center = new Vector2(cells.Average(c => c.x), cells.Average(c => c.y));
            int zone = LabTilemaps.ZoneOfDoor(map, group);
            var color = LabTilemaps.ZoneColor(zone);

            var door = new GameObject($"Door_Sala{zone}");
            door.transform.SetParent(parent, false);
            // Puerta horizontal: el pivote va en el borde inferior de la fila de la pared.
            door.transform.position = horizontal ? center + Vector2.down * 0.5f : center;
            var auto = door.AddComponent<AutoDoor>();
            auto.horizontal = horizontal;
            auto.span = group.Count;
            auto.leafA = Leaf(door.transform, "LeafA", art, horizontal);
            auto.leafB = Leaf(door.transform, "LeafB", art, horizontal);
            auto.statusLight = Glow(door.transform, "Status", art, Color.red, 0.55f, 0.5f, 25);
            auto.statusLight.transform.localPosition = horizontal ? new Vector3(0f, 0.85f, 0f) : new Vector3(0.6f, 0f, 0f);

            if (horizontal) BuildLintels(map, group, art, door.transform);

            float half = group.Count * 0.5f;
            foreach (float side in new[] { -half, half })
            {
                var jamb = Glow(door.transform, "JambLight", art, color, 0.5f, 0.9f, 6);
                jamb.transform.localPosition = horizontal ? new Vector3(side, 0.35f, 0f) : new Vector3(0f, side, 0f);
                var flicker = jamb.gameObject.AddComponent<FlickerLight>();
                flicker.mode = LightMode.Breathe;
                flicker.baseAlpha = 0.5f;
            }
            if (zone > 0) BuildSign(map, group, horizontal, zone, color, parent);
        }
    }

    /// <summary>
    /// Dintel sobre la puerta, ordenado por profundidad (pivote en el borde inferior de la fila de la pared,
    /// un poco por delante de las hojas): si Alex está delante de la puerta se dibuja encima del dintel;
    /// solo pasa "por debajo" mientras la cruza.
    /// </summary>
    static void BuildLintels(LevelLayout map, List<Vector2Int> group, ArtLibrary art, Transform door)
    {
        const float InFrontOfLeaves = 0.002f;
        foreach (var p in group)
        {
            var lintel = new GameObject("Lintel").AddComponent<SpriteRenderer>();
            lintel.transform.SetParent(door, false);
            lintel.transform.position = map.ToWorldBase(p.x, p.y) + Vector2.down * InFrontOfLeaves;
            lintel.sprite = art.LintelSprite;
            lintel.sortingOrder = 0;
            lintel.spriteSortPoint = SpriteSortPoint.Pivot;
        }
    }

    static SpriteRenderer Leaf(Transform parent, string name, ArtLibrary art, bool horizontal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = horizontal ? art.DoorPanelH : art.DoorPanelV;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = horizontal ? new Vector2(1.5f, 11f / 16f) : new Vector2(1f, 1.5f);
        renderer.sortingOrder = 0;
        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        return renderer;
    }

    public static SpriteRenderer Glow(Transform parent, string name, ArtLibrary art, Color color, float alpha, float scale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = art.Glow;
        renderer.sharedMaterial = art.Additive;
        renderer.color = new Color(color.r, color.g, color.b, alpha);
        renderer.sortingOrder = order;
        return renderer;
    }

    static void BuildSign(LevelLayout map, List<Vector2Int> group, bool horizontal, int zone, Color color, Transform parent)
    {
        var center = group[group.Count / 2];
        var world = map.ToWorldCenter(center.x, center.y);
        string label = $"NIVEL {zone} · {NarrativeScript.LevelName(zone).ToUpper()}";
        var sign = new GameObject($"Sign_Sala{zone}");
        sign.transform.SetParent(parent, false);
        sign.transform.position = horizontal ? world + Vector2.up * 0.95f : world + Vector2.up * 1.9f;

        var text = new GameObject("Text").AddComponent<TextMeshPro>();
        text.transform.SetParent(sign.transform, false);
        text.font = UIFactory.Heading;
        text.text = label;
        text.fontSize = 2.1f;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.rectTransform.sizeDelta = new Vector2(10f, 0.6f);
        text.sortingOrder = 46;
        var size = text.GetPreferredValues(label, 10f, 0.6f);

        var plate = new GameObject("Plate").AddComponent<SpriteRenderer>();
        plate.transform.SetParent(sign.transform, false);
        plate.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(ArtLibrary.Root + "/pixel.png");
        plate.color = new Color(0.02f, 0.03f, 0.06f, 0.82f);
        plate.drawMode = SpriteDrawMode.Simple;
        // pixel.png mide 2x2 px = 0.125 unidades: se escala al tamaño del texto con un margen.
        plate.transform.localScale = new Vector3((size.x + 0.4f) / 0.125f, (size.y + 0.15f) / 0.125f, 1f);
        plate.sortingOrder = 45;
    }

    /// <summary>Focos del hub (charcos de luz en el suelo) y un tono de color tenue en cada sala.</summary>
    public static void BuildLights(LevelLayout map, ArtLibrary art, Transform parent)
    {
        for (int y = 2; y < map.Height - 2; y += 5)
            for (int x = 2; x < map.Width - 2; x += 6)
            {
                if (!AllHub(map, x, y)) continue;
                var pool = Glow(parent, "CeilingLight", art, new Color(0.75f, 0.92f, 1f), 0.13f, 3.6f, -16);
                pool.transform.position = map.ToWorldCenter(x, y);
                var flicker = pool.gameObject.AddComponent<FlickerLight>();
                flicker.mode = (x + y) % 7 == 0 ? LightMode.Flicker : LightMode.Breathe;
                flicker.baseAlpha = 0.13f;
                flicker.amplitude = 0.05f;
            }

        for (int zone = 1; zone <= 7; zone++)
        {
            var cells = AllCells(map).Where(p => map.ZoneAt(p.x, p.y) == zone).ToList();
            if (cells.Count == 0) continue;
            var worlds = cells.Select(p => map.ToWorldCenter(p.x, p.y)).ToList();
            var center = new Vector2(worlds.Average(w => w.x), worlds.Average(w => w.y));
            float size = Mathf.Sqrt(cells.Count) * 0.9f;
            var tint = Glow(parent, $"Ambient_Sala{zone}", art, LabTilemaps.ZoneColor(zone), 0.12f, size, -16);
            tint.transform.position = center;
        }
    }

    static bool AllHub(LevelLayout map, int x, int y)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if (map.KindAt(x + dx, y + dy) != CellKind.Hub) return false;
        return true;
    }

    static IEnumerable<Vector2Int> AllCells(LevelLayout map)
    {
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                yield return new Vector2Int(x, y);
    }

    /// <summary>Dron que patrulla entre los accesos de las salas (vuela: sin colisión).</summary>
    public static void BuildDrone(LevelLayout map, ArtLibrary art, Transform parent)
    {
        var hubCenter = AllCells(map).Where(p => map.KindAt(p.x, p.y) == CellKind.Hub)
                                     .Select(p => map.ToWorldCenter(p.x, p.y)).ToList();
        if (hubCenter.Count == 0) return;
        var centroid = new Vector2(hubCenter.Average(p => p.x), hubCenter.Average(p => p.y));
        var points = new List<Vector2>();
        foreach (var group in LabTilemaps.DoorGroups(map))
        {
            var mid = group[group.Count / 2];
            foreach (var d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                var p = mid + d * 2;
                if (map.KindAt(p.x, p.y) == CellKind.Hub) { points.Add(map.ToWorldCenter(p.x, p.y)); break; }
            }
        }
        if (points.Count < 2) return;
        var loop = points.OrderBy(p => Mathf.Atan2(p.y - centroid.y, p.x - centroid.x)).ToArray();

        var drone = new GameObject("PatrolDrone");
        drone.transform.SetParent(parent, false);
        drone.transform.position = loop[0];
        var body = new GameObject("Body").AddComponent<SpriteRenderer>();
        body.transform.SetParent(drone.transform, false);
        body.transform.localPosition = new Vector3(0f, 1.25f, 0f);
        body.sprite = art.DroneA;
        body.sortingOrder = 30;
        body.gameObject.AddComponent<HoverBob>().amplitudePixels = 2f;
        var eye = Glow(body.transform, "EyeGlow", art, Theme.Cyan, 0.5f, 0.6f, 31);
        eye.transform.localPosition = new Vector3(0f, -0.03f, 0f);
        var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
        shadow.transform.SetParent(drone.transform, false);
        shadow.sprite = art.BlobShadow;
        shadow.sortingOrder = -6;
        var patrol = drone.AddComponent<PatrolDrone>();
        patrol.waypoints = loop;
        patrol.body = body;
        patrol.frameA = art.DroneA;
        patrol.frameB = art.DroneB;
    }

    public static void BuildParticles(LevelLayout map, ArtLibrary art, Transform parent)
    {
        var rect = map.WorldRect;
        Particles(parent, "AmbientMotes", art, rect.center, new Vector3(rect.width, rect.height, 1f), 7f,
                  new Vector2(5f, 9f), new Vector2(0.03f, 0.12f), new Vector2(0.06f, 0.1f), new Color(0.6f, 0.95f, 1f, 0.35f), 0f);
        ZoneParticles(map, art, parent, 7, "DataBits", 10f, new Color(1f, 0.3f, 0.45f, 0.8f), -0.15f);
        ZoneParticles(map, art, parent, 6, "Sparks", 3f, new Color(1f, 0.75f, 0.3f, 0.9f), 0.6f);
    }

    static void ZoneParticles(LevelLayout map, ArtLibrary art, Transform parent, int zone, string name, float rate, Color color, float gravity)
    {
        var cells = AllCells(map).Where(p => map.ZoneAt(p.x, p.y) == zone).Select(p => map.ToWorldCenter(p.x, p.y)).ToList();
        if (cells.Count == 0) return;
        var min = new Vector2(cells.Min(c => c.x), cells.Min(c => c.y));
        var max = new Vector2(cells.Max(c => c.x), cells.Max(c => c.y));
        Particles(parent, name, art, (min + max) * 0.5f, new Vector3(max.x - min.x + 1f, max.y - min.y + 1f, 1f), rate,
                  new Vector2(0.6f, 1.8f), new Vector2(0.2f, 0.8f), new Vector2(0.06f, 0.1f), color, gravity);
    }

    static void Particles(Transform parent, string name, ArtLibrary art, Vector2 center, Vector3 box, float rate,
                          Vector2 lifetime, Vector2 speed, Vector2 size, Color color, float gravity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.maxParticles = 120;
        var emission = ps.emission;
        emission.rateOverTime = rate;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = box;
        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = art.GlowParticles;
        renderer.sortingOrder = 35;
    }
}
