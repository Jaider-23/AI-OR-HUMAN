using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Entradas para ejecutar en modo batch (unity run -- -executeMethod BatchEntry.X).
/// Si algo falla, Unity sale con código 1 para que el error no pase desapercibido.
/// </summary>
public static class BatchEntry
{
    public static void Build() => Run(ProjectBuilder.BuildAll);

    public static void RebuildInterfaces() => Run(ProjectBuilder.RebuildInterfaces);

    public static void Audit() => Run(CollisionAudit.Run);

    public static void Shots() => Run(SceneSnapshot.Shots);

    public static void BuildWindows() => Run(ProjectBuilder.BuildWindows);

    public static void BuildWebGL() => Run(ProjectBuilder.BuildWebGL);

    /// <summary>Compila las versiones de Windows y Web en una sola ejecución de Unity.</summary>
    public static void BuildPlayers() => Run(() => $"{ProjectBuilder.BuildWindows()} | {ProjectBuilder.BuildWebGL()}");

    /// <summary>Construye el proyecto, audita colisiones y genera capturas en una sola ejecución.</summary>
    public static void BuildAuditShots() => Run(() =>
        $"{ProjectBuilder.BuildAll()} | {CollisionAudit.Run()} | {SceneSnapshot.Shots()}");

    static void Run(Func<string> action)
    {
        try
        {
            string result = action();
            Debug.Log($"[BatchEntry] OK {result}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[BatchEntry] FAILED {e}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            throw;
        }
    }
}
