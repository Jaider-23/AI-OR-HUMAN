using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bloquea el control del jugador mientras haya paneles abiertos (investigación, diálogo, pausa).
/// Cada panel usa su propia clave para no desbloquear el de otro.
/// </summary>
public static class InputGate
{
    static readonly HashSet<object> locks = new HashSet<object>();
    static int lastUnlockFrame = -1;

    public static bool IsLocked => locks.Count > 0;

    /// <summary>
    /// True en el mismo frame en que se cerró un panel: la tecla que lo cerró
    /// no debe volver a activar una interacción.
    /// </summary>
    public static bool JustUnlocked => lastUnlockFrame == Time.frameCount;

    public static void Lock(object owner) => locks.Add(owner);

    public static void Unlock(object owner)
    {
        if (locks.Remove(owner)) lastUnlockFrame = Time.frameCount;
    }

    public static void Clear() => locks.Clear();
}
