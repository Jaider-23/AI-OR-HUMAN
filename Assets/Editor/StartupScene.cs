using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Evita la "pantalla azul": si Unity abre con una escena vacía (Untitled) se abre el menú principal,
/// y si se pulsa Play en una escena sin guardar el juego arranca desde el menú (una escena vacía solo
/// muestra el color de fondo de la cámara). No interfiere con las escenas del juego ni con las pruebas.
/// </summary>
[InitializeOnLoad]
public static class StartupScene
{
    static StartupScene()
    {
        EditorApplication.delayCall += OpenMenuIfUntitled;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    /// <summary>Escena vacía recién creada: sin ruta, sin nombre y sin cambios.</summary>
    static bool IsEmptyUntitled(Scene scene) =>
        string.IsNullOrEmpty(scene.path) && string.IsNullOrEmpty(scene.name) && !scene.isDirty;

    static void OpenMenuIfUntitled()
    {
        if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.sceneCount != 1 || !IsEmptyUntitled(SceneManager.GetActiveScene())) return;
        EditorSceneManager.OpenScene(BuildPaths.MenuScene);
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorSceneManager.playModeStartScene = null;
            return;
        }
        if (state != PlayModeStateChange.ExitingEditMode || Application.isBatchMode) return;
        // Solo la escena vacía "Untitled"; las pruebas usan escenas propias con nombre y no se tocan.
        if (IsEmptyUntitled(SceneManager.GetActiveScene()))
        {
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BuildPaths.MenuScene);
        }
    }

    /// <summary>Abre el menú principal y entra en modo Play (lo usa el puente con el Editor).</summary>
    public static string PlayFromMenu()
    {
        if (EditorApplication.isPlaying) return "ya en modo Play";
        EditorSceneManager.OpenScene(BuildPaths.MenuScene);
        EditorApplication.isPlaying = true;
        return "Play desde " + BuildPaths.MenuScene;
    }

    /// <summary>Guarda una captura de la vista Game en ClaudeBridge/play.png (solo en modo Play).</summary>
    public static string CaptureGameView()
    {
        if (!EditorApplication.isPlaying) return "no está en modo Play";
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ClaudeBridge", "play.png"));
        if (File.Exists(path)) File.Delete(path);
        ScreenCapture.CaptureScreenshot(path);
        return $"captura solicitada: {path} (escena activa: {SceneManager.GetActiveScene().name})";
    }

    public static string ActiveScene() =>
        $"escena={SceneManager.GetActiveScene().path} playing={EditorApplication.isPlaying} " +
        $"build=[{string.Join(", ", EditorBuildSettings.scenes.Select(s => s.path))}]";
}
