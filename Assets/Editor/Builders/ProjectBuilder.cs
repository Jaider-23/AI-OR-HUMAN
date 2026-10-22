using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Punto de entrada de la construcción del proyecto. Menú "AI OR HUMAN" en el Editor
/// o en batch con -executeMethod ProjectBuilder.BuildAll.
/// </summary>
public static class ProjectBuilder
{
    const string Menu = "AI OR HUMAN/";
    const string BuildsFolder = "Builds";

    [MenuItem(Menu + "Construir todo (animaciones, casos, escenas)", priority = 0)]
    public static string BuildAll()
    {
        if (!EditorSceneGuard.ConfirmLeaveOpenScenes()) return EditorSceneGuard.Cancelled;
        ConfigureImports();
        SpritePivots.Apply();
        AnimationBuilder.BuildAll();
        var cases = CaseBankBuilder.BuildAll();
        AssignEvidence();
        BuildGameScene(cases);
        BuildMenuScene(cases);
        ConfigurePlayer();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(BuildPaths.MenuScene);
        Debug.Log("[ProjectBuilder] Construcción completa.");
        return $"cases={cases.Length}";
    }

    /// <summary>
    /// Regenera solo la interfaz (menú principal y HUD) sin tocar arte, casos ni mapa:
    /// es la forma rápida de que aparezcan los elementos nuevos, como el botón CONTINUAR
    /// y el aviso de autoguardado.
    /// </summary>
    [MenuItem(Menu + "Reconstruir interfaz (menú y HUD)", priority = 1)]
    public static string RebuildInterfaces()
    {
        if (!EditorSceneGuard.ConfirmLeaveOpenScenes()) return EditorSceneGuard.Cancelled;
        var cases = CaseBankBuilder.LoadExisting();
        if (cases.Length == 0) cases = CaseBankBuilder.BuildAll();

        var game = EditorSceneManager.OpenScene(BuildPaths.GameScene, OpenSceneMode.Single);
        var ui = GameUIBuilder.Build();
        var manager = Object.FindObjectOfType<GameManager>();
        if (manager != null)
        {
            // El mundo (terminales y fragmentos) no se toca: solo se vuelven a enlazar los paneles.
            manager.hud = ui.Hud;
            manager.dialogue = ui.Dialogue;
            manager.investigation = ui.Investigation;
            manager.endScreen = ui.End;
            EditorUtility.SetDirty(manager);
        }
        EditorSceneManager.MarkSceneDirty(game);
        EditorSceneManager.SaveScene(game);

        BuildMenuScene(cases);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(BuildPaths.MenuScene);
        Debug.Log("[ProjectBuilder] Interfaz reconstruida (menú principal y HUD).");
        return $"interfaz reconstruida, cases={cases.Length}";
    }

    static void ConfigureImports()
    {
        foreach (var path in new[] { BuildPaths.Aria, BuildPaths.LeakedImage })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null || importer.textureType == TextureImporterType.Sprite) continue;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    static void AssignEvidence()
    {
        SetEvidence("C-301", BuildPaths.Aria);
        SetEvidence("C-302", BuildPaths.LeakedImage);
    }

    static void SetEvidence(string caseId, string spritePath)
    {
        var data = AssetDatabase.LoadAssetAtPath<CaseData>($"{CaseBankBuilder.CasesFolder}/{caseId}.asset");
        if (data == null) return;
        data.evidenceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        EditorUtility.SetDirty(data);
    }

    static void BuildGameScene(CaseData[] cases)
    {
        var scene = EditorSceneManager.OpenScene(BuildPaths.GameScene, OpenSceneMode.Single);
        var (terminals, fragments) = LabWorldBuilder.Build(cases);
        var ui = GameUIBuilder.Build();

        var manager = Object.FindObjectOfType<GameManager>();
        if (manager == null) manager = new GameObject("GameManager").AddComponent<GameManager>();
        manager.cases = cases.OrderBy(c => c.level).ToArray();
        manager.terminals = terminals;
        manager.fragments = fragments;
        manager.hud = ui.Hud;
        manager.dialogue = ui.Dialogue;
        manager.investigation = ui.Investigation;
        manager.endScreen = ui.End;
        EditorUtility.SetDirty(manager);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void BuildMenuScene(CaseData[] cases)
    {
        var scene = EditorSceneManager.OpenScene(BuildPaths.MenuScene, OpenSceneMode.Single);
        MenuBuilder.Build(cases.OrderBy(c => c.level).ToArray());
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void ConfigurePlayer()
    {
        PlayerSettings.companyName = "Digital Truth Agency";
        PlayerSettings.productName = "AI OR HUMAN";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(BuildPaths.MenuScene, true),
            new EditorBuildSettingsScene(BuildPaths.GameScene, true)
        };
    }

    [MenuItem(Menu + "Compilar para Windows", priority = 20)]
    public static string BuildWindows() =>
        Build(BuildTarget.StandaloneWindows64, Path.Combine(BuildsFolder, "Windows", "AI OR HUMAN.exe"));

    [MenuItem(Menu + "Compilar para Web (WebGL)", priority = 21)]
    public static string BuildWebGL() => Build(BuildTarget.WebGL, Path.Combine(BuildsFolder, "WebGL"));

    static string Build(BuildTarget target, string location)
    {
        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = location,
            target = target,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        string message = $"{target}: {summary.result} errors={summary.totalErrors} size={summary.totalSize / (1024 * 1024)}MB -> {location}";
        Debug.Log($"[ProjectBuilder] {message}");
        if (summary.result != BuildResult.Succeeded && Application.isBatchMode) EditorApplication.Exit(1);
        return message;
    }
}
