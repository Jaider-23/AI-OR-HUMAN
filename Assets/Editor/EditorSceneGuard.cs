using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Las herramientas del menú "AI OR HUMAN" abren escenas en modo Single: antes de hacerlo se pregunta
/// si guardar los cambios pendientes, para no descartar en silencio el trabajo del equipo.
/// </summary>
public static class EditorSceneGuard
{
    /// <summary>true si se puede continuar (en modo batch, o si el usuario guardó/descartó los cambios).</summary>
    public static bool ConfirmLeaveOpenScenes() =>
        Application.isBatchMode || EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

    public const string Cancelled = "cancelado: hay cambios sin guardar en la escena abierta";
}
