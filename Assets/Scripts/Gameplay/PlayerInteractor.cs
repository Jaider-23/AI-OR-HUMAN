using System;
using UnityEngine;

/// <summary>Detecta el interactuable más cercano y lo activa con E / Espacio / Enter.</summary>
[DefaultExecutionOrder(-100)]
public class PlayerInteractor : MonoBehaviour
{
    /// <summary>Se dispara cuando cambia el objeto enfocado (null si no hay ninguno).</summary>
    public static event Action<Interactable> FocusChanged;

    Interactable focused;

    void Update()
    {
        var nearest = InputGate.IsLocked ? null : FindNearest();
        if (nearest != focused)
        {
            focused = nearest;
            FocusChanged?.Invoke(focused);
        }

        if (focused != null && !InputGate.JustUnlocked && InteractPressed()) focused.Interact(this);
    }

    void OnDisable()
    {
        focused = null;
        FocusChanged?.Invoke(null);
    }

    static bool InteractPressed() =>
        Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);

    Interactable FindNearest()
    {
        Interactable best = null;
        float bestDistance = float.MaxValue;
        Vector2 position = transform.position;
        // Bucle por índice: un foreach sobre IReadOnlyList reserva memoria en cada fotograma.
        var active = Interactable.Active;
        for (int i = 0; i < active.Count; i++)
        {
            var candidate = active[i];
            if (candidate == null || !candidate.CanInteract || !candidate.CanInteractFrom(position)) continue;
            float distance = Vector2.Distance(position, candidate.transform.position);
            if (distance <= candidate.interactRadius && distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }
        return best;
    }
}
