using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Lanza la prueba automática de juego en el Editor (ver PlaytestDriver).</summary>
public static class PlaytestLauncher
{
    [MenuItem("AI OR HUMAN/Prueba automática de juego", priority = 40)]
    public static string Run()
    {
        if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        if (!EditorSceneGuard.ConfirmLeaveOpenScenes()) return EditorSceneGuard.Cancelled;
        EditorSceneManager.OpenScene(BuildPaths.GameScene);
        SessionState.SetBool(PlaytestDriver.SessionKey, true);
        EditorApplication.isPlaying = true;
        return "playtest iniciado";
    }

    public static string Stop()
    {
        EditorApplication.isPlaying = false;
        return "detenido";
    }
}
