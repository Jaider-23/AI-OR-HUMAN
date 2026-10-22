using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Herramienta de diagnóstico: renderiza el nivel completo a PNG y vuelca un informe con
/// los límites del tilemap y la posición de los objetos. Útil para revisar el mapa sin abrir el Editor.
/// </summary>
public static class SceneSnapshot
{
    const int Pixels = 1600;

    /// <summary>Entrada batch: -executeMethod SceneSnapshot.Run -snapshotOut &lt;carpeta&gt;</summary>
    public static void Run()
    {
        string outDir = ArgValue("-snapshotOut") ?? "Temp/Snapshots";
        Directory.CreateDirectory(outDir);
        Capture(BuildPaths.GameScene, Path.Combine(outDir, "game_level.png"), Path.Combine(outDir, "game_level.txt"));
        CaptureUI(BuildPaths.MenuScene, Path.Combine(outDir, "menu.png"), null);
        CaptureUI(BuildPaths.GameScene, Path.Combine(outDir, "game_ui.png"), null);
    }

    /// <summary>
    /// Renderiza los Canvas de una escena (pasándolos temporalmente a Screen Space - Camera).
    /// <paramref name="activate"/> permite mostrar un panel concreto antes de capturar. No guarda la escena.
    /// </summary>
    public static void CaptureUI(string scenePath, string pngPath, string activate)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        if (!string.IsNullOrEmpty(activate))
        {
            foreach (string target in activate.Split(';'))
            {
                foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                {
                    if (t.gameObject.scene.IsValid() && HierarchyPath(t).EndsWith(target)) t.gameObject.SetActive(true);
                }
            }
        }

        var go = new GameObject("SnapshotUICamera");
        var cam = go.AddComponent<Camera>();
        var mainCam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = mainCam != null ? mainCam.orthographicSize : 5f;
        go.transform.position = mainCam != null ? mainCam.transform.position : new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        const int width = 1920, height = 1080;
        cam.aspect = (float)width / height;
        foreach (var canvas in Object.FindObjectsOfType<Canvas>())
        {
            if (!canvas.isRootCanvas) continue;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 1000;
        }
        Canvas.ForceUpdateCanvases();

        var rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(pngPath, tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
    }

    /// <summary>Capturas de revisión en ClaudeBridge/shots (sin guardar cambios en las escenas).</summary>
    public static string Shots()
    {
        if (!EditorSceneGuard.ConfirmLeaveOpenScenes()) return EditorSceneGuard.Cancelled;
        string dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "ClaudeBridge", "shots"));
        Directory.CreateDirectory(dir);
        Capture(BuildPaths.GameScene, System.IO.Path.Combine(dir, "level.png"), System.IO.Path.Combine(dir, "level.txt"));
        CaptureUI(BuildPaths.MenuScene, System.IO.Path.Combine(dir, "menu.png"), null);
        CaptureUI(BuildPaths.MenuScene, System.IO.Path.Combine(dir, "menu_stats.png"), "StatsPanel");
        CaptureUI(BuildPaths.MenuScene, System.IO.Path.Combine(dir, "menu_settings.png"), "SettingsPanel");
        CaptureUI(BuildPaths.GameScene, System.IO.Path.Combine(dir, "game_hud.png"), "GameUI/HUD/Prompt;GameUI/Dialogue/Box");
        CaptureUI(BuildPaths.GameScene, System.IO.Path.Combine(dir, "game_investigation.png"), "GameUI/Investigation/Root");
        CaptureUI(BuildPaths.GameScene, System.IO.Path.Combine(dir, "game_feedback.png"), "GameUI/Investigation/Root;Feedback/Root");
        EditorSceneManager.OpenScene(BuildPaths.GameScene);
        return dir;
    }

    public static void Capture(string scenePath, string pngPath, string reportPath)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var report = new StringBuilder();
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        foreach (var tilemap in Object.FindObjectsOfType<Tilemap>())
        {
            tilemap.CompressBounds();
            var b = tilemap.localBounds;
            var world = new Bounds(tilemap.transform.TransformPoint(b.center), b.size);
            report.AppendLine($"TILEMAP {tilemap.name} cellBounds={tilemap.cellBounds} world={world.min}..{world.max}");
            if (hasBounds) bounds.Encapsulate(world);
            else { bounds = world; hasBounds = true; }
        }
        foreach (var renderer in Object.FindObjectsOfType<SpriteRenderer>())
        {
            report.AppendLine($"SPRITE {HierarchyPath(renderer.transform)} pos={renderer.transform.position} sprite={(renderer.sprite ? renderer.sprite.name : "-")} order={renderer.sortingOrder} layer={renderer.sortingLayerName}");
        }
        foreach (var cam in Object.FindObjectsOfType<Camera>())
        {
            report.AppendLine($"CAMERA {cam.name} pos={cam.transform.position} ortho={cam.orthographicSize}");
        }
        report.AppendLine($"BOUNDS {bounds.min}..{bounds.max}");
        File.WriteAllText(reportPath, report.ToString());

        RenderBounds(bounds, pngPath);
        Debug.Log($"[SceneSnapshot] {scene.name} -> {pngPath}");
    }

    static void RenderBounds(Bounds bounds, string pngPath)
    {
        var go = new GameObject("SnapshotCamera");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        float aspect = Mathf.Max(0.5f, bounds.size.x / Mathf.Max(1f, bounds.size.y));
        int width = Pixels;
        int height = Mathf.RoundToInt(Pixels / aspect);
        cam.orthographicSize = bounds.extents.y + 0.5f;
        cam.aspect = (float)width / height;
        go.transform.position = new Vector3(bounds.center.x, bounds.center.y, -20f);

        var rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(pngPath, tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
    }

    static string HierarchyPath(Transform t) => t.parent == null ? t.name : HierarchyPath(t.parent) + "/" + t.name;

    public static string ArgValue(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name) return args[i + 1];
        }
        return null;
    }
}
