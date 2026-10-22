using UnityEngine;

public enum TerminalState
{
    Locked,
    Active,
    Solved
}

/// <summary>
/// Terminal del laboratorio de la DTA que contiene un caso. Solo la terminal activa
/// abre la investigación; las demás muestran un mensaje de ADA.
/// El brillo de pantalla indica el estado: cian pulsante (activa), tenue (bloqueada), verde (resuelta).
/// </summary>
public class CaseTerminal : Interactable
{
    const float PixelSize = 1f / 16f;
    static readonly Color LockedTint = new Color(0.45f, 0.5f, 0.6f, 1f);
    static readonly Color SolvedTint = new Color(0.55f, 1f, 0.75f, 1f);
    static readonly Color ActiveTint = new Color(0.55f, 1f, 1f, 1f);
    static readonly Color ActiveGlow = new Color(0.13f, 0.89f, 0.94f, 1f);
    static readonly Color LockedGlow = new Color(0.25f, 0.35f, 0.6f, 0.18f);
    static readonly Color SolvedGlow = new Color(0.27f, 0.95f, 0.55f, 0.35f);

    [Tooltip("Índice del caso en la lista del GameManager.")]
    public int caseIndex;
    public SpriteRenderer body;
    [Tooltip("Brillo aditivo de la pantalla.")]
    public SpriteRenderer glow;
    [Tooltip("Holograma animado que flota sobre la terminal activa.")]
    public Transform activeMarker;
    public float markerBobPixels = 2f;
    public float markerBobSpeed = 3f;
    [Tooltip("Pantalla montada en la pared: solo se usa desde delante (al sur), nunca a través del muro.")]
    public bool frontOnly = true;

    TerminalState state = TerminalState.Locked;
    Vector3 markerBasePosition;

    public TerminalState State => state;

    public override string PromptText
    {
        get
        {
            switch (state)
            {
                case TerminalState.Active: return "Analizar evidencia";
                case TerminalState.Solved: return "Caso cerrado";
                default: return "Terminal bloqueada";
            }
        }
    }

    void Awake()
    {
        if (activeMarker != null) markerBasePosition = activeMarker.localPosition;
    }

    void Update()
    {
        if (state != TerminalState.Active) return;
        float pulse = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
        if (body != null) body.color = Color.Lerp(Color.white, ActiveTint, pulse);
        if (glow != null) glow.color = new Color(ActiveGlow.r, ActiveGlow.g, ActiveGlow.b, Mathf.Lerp(0.45f, 0.85f, pulse));
        if (activeMarker == null || !activeMarker.gameObject.activeSelf) return;
        float offset = Mathf.Round(Mathf.Sin(Time.time * markerBobSpeed) * markerBobPixels) * PixelSize;
        activeMarker.localPosition = markerBasePosition + Vector3.up * offset;
    }

    public void SetState(TerminalState newState)
    {
        state = newState;
        if (body != null)
        {
            body.color = newState == TerminalState.Locked ? LockedTint
                       : newState == TerminalState.Solved ? SolvedTint
                       : Color.white;
        }
        if (glow != null)
        {
            glow.color = newState == TerminalState.Locked ? LockedGlow
                       : newState == TerminalState.Solved ? SolvedGlow
                       : ActiveGlow;
        }
        if (activeMarker != null) activeMarker.gameObject.SetActive(newState == TerminalState.Active);
    }

    public override bool CanInteractFrom(Vector2 playerPosition) =>
        !frontOnly || playerPosition.y <= transform.position.y + 0.05f;

    public override void Interact(PlayerInteractor interactor)
    {
        var manager = GameManager.Instance;
        if (manager == null) return;
        manager.OnTerminalUsed(this);
    }
}
