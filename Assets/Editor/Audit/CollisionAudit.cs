using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Auditoría objetiva de colisiones de GameScene:
/// 1) inventario de colliders, 2) mapa de posiciones alcanzables con el cuerpo real del jugador,
/// 3) interactuables inalcanzables, 4) colliders que no coinciden con píxeles visibles
/// ("paredes invisibles"), 5) imagen con todo superpuesto. Resultados en ClaudeBridge/audit.
/// </summary>
public static class CollisionAudit
{
    const float Step = 0.125f;
    const float RenderPixelsPerUnit = 40f;
    const float MinVisibleCoverage = 0.6f;

    static readonly Color BlockedColor = new Color(1f, 0.1f, 0.1f, 0.28f);
    static readonly Color UnreachableColor = new Color(0.2f, 0.5f, 1f, 0.45f);
    static readonly Color SolidOutline = new Color(1f, 0.15f, 0.15f, 1f);
    static readonly Color CompositeOutline = new Color(1f, 0.2f, 1f, 1f);
    static readonly Color TriggerOutline = new Color(0.2f, 1f, 0.3f, 1f);

    [MenuItem("AI OR HUMAN/Auditar colisiones", priority = 41)]
    public static string Run()
    {
        if (!EditorSceneGuard.ConfirmLeaveOpenScenes()) return EditorSceneGuard.Cancelled;
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ClaudeBridge", "audit"));
        Directory.CreateDirectory(dir);
        EditorSceneManager.OpenScene(BuildPaths.GameScene, OpenSceneMode.Single);
        foreach (var composite in Object.FindObjectsOfType<CompositeCollider2D>()) composite.GenerateGeometry();
        Physics2D.SyncTransforms();
        SpritePixelSampler.ClearCache();

        var player = Object.FindObjectOfType<PlayerController>();
        var probe = new PlayerProbe(player);
        var report = new StringBuilder();
        WritePlayerSetup(report, player, probe);
        WriteInventory(report);

        var bounds = LevelBounds();
        var grid = new WalkGrid(bounds, Step, probe);
        var reachable = grid.FloodFrom(player.transform.position);
        report.AppendLine($"GRID {grid.Width}x{grid.Height} step={Step} reachable={reachable.Count(r => r)} blocked={grid.BlockedCount}");

        int unreachableTargets = WriteInteractables(report, grid, reachable);
        int invisible = WriteCoverage(report);
        WriteFrontierBlockers(report, grid, reachable);

        var texture = LevelRenderer.Render(bounds, RenderPixelsPerUnit);
        var canvas = new TextureCanvas(texture, bounds.min, RenderPixelsPerUnit);
        DrawSamples(canvas, grid, reachable);
        DrawColliders(canvas);
        canvas.Apply();
        File.WriteAllBytes(Path.Combine(dir, "collision.png"), texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        File.WriteAllText(Path.Combine(dir, "report.txt"), report.ToString());
        return $"unreachableTargets={unreachableTargets} invisibleColliders={invisible} -> {dir}";
    }

    static Bounds LevelBounds()
    {
        var bounds = new Bounds();
        bool first = true;
        foreach (var tilemap in Object.FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>())
        {
            tilemap.CompressBounds();
            var b = tilemap.localBounds;
            var world = new Bounds(tilemap.transform.TransformPoint(b.center), b.size);
            if (first) { bounds = world; first = false; }
            else bounds.Encapsulate(world);
        }
        bounds.Expand(new Vector3(1f, 1f, 0f));
        return bounds;
    }

    static void WritePlayerSetup(StringBuilder report, PlayerController player, PlayerProbe probe)
    {
        var body = player.GetComponent<Rigidbody2D>();
        report.AppendLine($"PLAYER pos={player.transform.position} probe={probe} body={body.bodyType} gravity={body.gravityScale} " +
                          $"material={(probe.Material ? $"{probe.Material.name} f={probe.Material.friction}" : "none")} " +
                          $"detection={body.collisionDetectionMode} contactOffset={Physics2D.defaultContactOffset}");
        // El sprite vive en el hijo "Visual" (PlayerVisuals.visual); el objeto raíz solo tiene física.
        var visuals = player.GetComponent<PlayerVisuals>();
        var renderer = visuals != null && visuals.visual != null ? visuals.visual.GetComponent<SpriteRenderer>() : null;
        if (renderer == null || renderer.sprite == null)
        {
            report.AppendLine("PLAYER sprite=MISSING (no hay SpriteRenderer en PlayerVisuals.visual)");
            return;
        }
        report.AppendLine($"PLAYER sprite={renderer.sprite.name} pivot(px)={renderer.sprite.pivot} rect={renderer.sprite.rect} sortPoint={renderer.spriteSortPoint}");
    }

    static void WriteInventory(StringBuilder report)
    {
        foreach (var c in Object.FindObjectsOfType<Collider2D>().OrderBy(c => c.transform.position.y))
        {
            string rb = c.attachedRigidbody ? c.attachedRigidbody.bodyType.ToString() : "none";
            report.AppendLine($"COLLIDER {HierarchyPath(c.transform)} type={c.GetType().Name} trigger={c.isTrigger} " +
                              $"enabled={c.enabled && c.gameObject.activeInHierarchy} layer={LayerMask.LayerToName(c.gameObject.layer)} " +
                              $"composite={c.usedByComposite} rb={rb} bounds={Fmt(c.bounds.min)}..{Fmt(c.bounds.max)}");
            if (c is CompositeCollider2D cc) report.AppendLine($"    geometry={cc.geometryType} paths={cc.pathCount} points={cc.pointCount}");
        }
    }

    static int WriteInteractables(StringBuilder report, WalkGrid grid, bool[] reachable)
    {
        int unreachable = 0;
        foreach (var target in Object.FindObjectsOfType<Interactable>())
        {
            float best = grid.MinReachableDistance(reachable, target.transform.position, target.CanInteractFrom);
            bool ok = best <= target.interactRadius;
            if (!ok) unreachable++;
            report.AppendLine($"TARGET {(ok ? "OK " : "NO ")} {target.name} radius={target.interactRadius} closest={best:F2}");
        }
        foreach (var fragment in Object.FindObjectsOfType<DataFragment>())
        {
            float best = grid.MinReachableDistance(reachable, fragment.transform.position);
            bool ok = best <= 0.6f;
            if (!ok) unreachable++;
            report.AppendLine($"FRAGMENT {(ok ? "OK " : "NO ")} {fragment.name} closest={best:F2}");
        }
        return unreachable;
    }

    /// <summary>Porcentaje del área de cada collider sólido de un objeto con sprite que cae sobre píxeles visibles.</summary>
    static int WriteCoverage(StringBuilder report)
    {
        int flagged = 0;
        foreach (var c in Object.FindObjectsOfType<Collider2D>())
        {
            if (c.isTrigger || c.usedByComposite || c is CompositeCollider2D || !c.enabled) continue;
            if (c.GetComponent<PlayerController>() != null) continue;
            // Sprites visibles del objeto (o de sus hijos), sin contar brillos aditivos ni sombras.
            var renderers = c.GetComponentsInChildren<SpriteRenderer>()
                             .Where(r => r.sprite != null && r.sortingOrder > -6
                                         && (r.sharedMaterial == null || !r.sharedMaterial.shader.name.Contains("Additive")))
                             .ToArray();
            if (renderers.Length == 0)
            {
                report.AppendLine($"INVISIBLE? {c.name} has a solid collider without a visible sprite");
                flagged++;
                continue;
            }
            int inside = 0, opaque = 0;
            var b = c.bounds;
            for (float x = b.min.x + 0.03f; x < b.max.x; x += 0.06f)
                for (float y = b.min.y + 0.03f; y < b.max.y; y += 0.06f)
                {
                    var p = new Vector2(x, y);
                    if (!c.OverlapPoint(p)) continue;
                    inside++;
                    if (renderers.Any(r => SpritePixelSampler.IsOpaqueAt(r, p))) opaque++;
                }
            float coverage = inside == 0 ? 1f : (float)opaque / inside;
            bool bad = coverage < MinVisibleCoverage;
            if (bad) flagged++;
            report.AppendLine($"COVERAGE {(bad ? "LOW" : "ok ")} {c.name} {coverage:P0} of collider over visible pixels");
        }
        return flagged;
    }

    static void WriteFrontierBlockers(StringBuilder report, WalkGrid grid, bool[] reachable)
    {
        var counts = grid.FrontierBlockers(reachable);
        foreach (var pair in counts.OrderByDescending(p => p.Value))
        {
            report.AppendLine($"BUMP {pair.Value,5} contacts with {pair.Key}");
        }
    }

    static void DrawSamples(TextureCanvas canvas, WalkGrid grid, bool[] reachable)
    {
        for (int i = 0; i < grid.Width; i++)
            for (int j = 0; j < grid.Height; j++)
            {
                int index = grid.Index(i, j);
                if (i % 2 != 0 || j % 2 != 0) continue;
                if (grid.Blocked[index]) canvas.Dot(grid.Position(i, j), 1, BlockedColor);
                else if (!reachable[index]) canvas.Dot(grid.Position(i, j), 1, UnreachableColor);
            }
    }

    static void DrawColliders(TextureCanvas canvas)
    {
        foreach (var c in Object.FindObjectsOfType<Collider2D>())
        {
            if (!c.enabled || (c.usedByComposite && !(c is CompositeCollider2D))) continue;
            var color = c.isTrigger ? TriggerOutline : c is CompositeCollider2D ? CompositeOutline : SolidOutline;
            foreach (var outline in ColliderShapes.Outlines(c)) canvas.Polygon(outline, color, 2);
        }
    }

    static string Fmt(Vector3 v) => $"({v.x:F2},{v.y:F2})";

    static string HierarchyPath(Transform t) => t.parent == null ? t.name : HierarchyPath(t.parent) + "/" + t.name;
}
