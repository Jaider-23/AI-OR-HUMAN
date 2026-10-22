using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base de todo objeto con el que Alex puede interactuar con la tecla E.
/// Se registra en una lista estática para que <see cref="PlayerInteractor"/> encuentre el más cercano.
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    static readonly List<Interactable> active = new List<Interactable>();

    public static IReadOnlyList<Interactable> Active => active;

    [Tooltip("Distancia máxima (en unidades) para poder interactuar.")]
    public float interactRadius = 1.6f;

    [Tooltip("Desplazamiento del anillo de foco respecto al objeto (p. ej. delante de una pantalla de pared).")]
    public Vector2 focusOffset;

    /// <summary>Texto que aparece en el HUD, por ejemplo "Analizar terminal".</summary>
    public abstract string PromptText { get; }

    public virtual bool CanInteract => isActiveAndEnabled;

    /// <summary>Permite restringir desde dónde se puede usar (p. ej. una pantalla de pared solo por delante).</summary>
    public virtual bool CanInteractFrom(Vector2 playerPosition) => true;

    public abstract void Interact(PlayerInteractor interactor);

    protected virtual void OnEnable() => active.Add(this);

    protected virtual void OnDisable() => active.Remove(this);
}
