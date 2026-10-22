using System;
using UnityEngine;

/// <summary>
/// Autoguardado de la partida en curso. Lo añade <see cref="GameManager"/> a su propio objeto,
/// así que funciona sin tocar la escena.
///
/// Escribe un punto de control cuando el mundo está en un estado coherente:
/// al cerrar un caso, al recoger un fragmento, cada <see cref="PeriodicSeconds"/> de juego libre,
/// al pausar, al perder el foco, al salir del juego y al volver al menú.
///
/// Nunca guarda en modo entrenamiento (no cuenta para nada) ni con un caso o un diálogo a medias:
/// un guardado a mitad de caso dejaría los puntos aplicados y el caso sin cerrar. Mientras haya
/// algo abierto la petición queda pendiente y se escribe en cuanto el panel se cierra.
/// </summary>
[DisallowMultipleComponent]
public class AutoSave : MonoBehaviour
{
    /// <summary>Repaso periódico: recoge el paseo de Alex aunque no cambie la puntuación.</summary>
    public const float PeriodicSeconds = 20f;

    /// <summary>Dos escrituras seguidas no aportan nada y en WebGL cada una cuesta un volcado a disco.</summary>
    const float MinSecondsBetweenWrites = 1f;

    /// <summary>Se dispara después de escribir el guardado (el HUD lo usa para avisar al jugador).</summary>
    public static event Action Saved;

    GameManager manager;
    SaveData lastWritten;
    bool pending;
    float nextPeriodic;
    float lastWriteTime = float.NegativeInfinity;

    /// <summary>Empieza a vigilar la partida de este <see cref="GameManager"/>.</summary>
    public void Begin(GameManager owner)
    {
        manager = owner;
        lastWritten = null;
        nextPeriodic = Time.unscaledTime + PeriodicSeconds;
        // Deja constancia de la partida en cuanto termine la introducción.
        pending = true;
    }

    /// <summary>Pide un punto de control (lo escribe en cuanto el estado sea coherente).</summary>
    public void Request() => pending = true;

    /// <summary>
    /// Deja de vigilar la partida: ni el repaso periódico ni el cierre de la escena volverán
    /// a escribir. Lo usan las pruebas automáticas para no tocar la partida del jugador.
    /// </summary>
    public void Stop()
    {
        manager = null;
        pending = false;
    }

    void Update()
    {
        if (Time.unscaledTime >= nextPeriodic)
        {
            nextPeriodic = Time.unscaledTime + PeriodicSeconds;
            pending = true;
        }
        if (pending) TryWrite(false);
    }

    // El tiempo real: durante la pausa Time.timeScale es 0 pero el autoguardado sigue vivo.
    void OnApplicationPause(bool paused)
    {
        if (paused) TryWrite(true);
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused) TryWrite(true);
    }

    void OnApplicationQuit() => TryWrite(true);

    /// <summary>Al descargarse la escena (volver al menú) se guarda el último estado.</summary>
    void OnDestroy() => TryWrite(true, false);

    /// <param name="force">Salir del juego o de la escena: ignora el intervalo mínimo.</param>
    /// <param name="notify">False al destruirse la escena: el HUD ya no está para avisar.</param>
    bool TryWrite(bool force, bool notify = true)
    {
        if (manager == null || GameSession.TrainingMode)
        {
            pending = false;
            return false;
        }
        // Caso, diálogo o final en marcha: estado a medias, se reintenta al cerrarse.
        if (!manager.CanCheckpoint) return false;
        if (!force && Time.unscaledTime - lastWriteTime < MinSecondsBetweenWrites) return false;

        var data = manager.CaptureSave();
        pending = false;
        // Partida terminada o sin casos: no hay nada que retomar.
        if (data.totalCases <= 0 || data.caseIndex >= data.totalCases) return false;
        if (SaveData.SameProgress(data, lastWritten)) return false;

        SaveSystem.Write(data);
        lastWritten = data;
        lastWriteTime = Time.unscaledTime;
        if (notify) Saved?.Invoke();
        return true;
    }
}
