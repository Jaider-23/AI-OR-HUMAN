using UnityEngine;

/// <summary>
/// Puerta corrediza automática: sus dos hojas se recogen hacia las jambas cuando Alex se acerca
/// (se encogen en lugar de atravesar la pared). Nunca bloquea el paso: no tiene collider y se abre
/// antes de que el jugador llegue, así que no actúa como pared invisible ni visible.
/// </summary>
public class AutoDoor : MonoBehaviour
{
    /// <summary>Ancho mínimo de una hoja abierta: 4 px, igual que los bordes 9-slice (si no, se deforman).</summary>
    const float MinLeaf = 0.25f;
    const float PixelSize = 1f / 16f;

    public SpriteRenderer leafA;
    public SpriteRenderer leafB;
    [Tooltip("true = puerta en pared horizontal (las hojas se recogen hacia este/oeste).")]
    public bool horizontal = true;
    [Tooltip("Longitud total del hueco de la puerta en unidades.")]
    public float span = 3f;
    public float openRadius = 2.8f;
    public float openSpeed = 4f;
    public SpriteRenderer statusLight;
    public Color closedColor = new Color(1f, 0.3f, 0.42f);
    public Color openColor = new Color(0.27f, 0.95f, 0.55f);

    Transform player;
    float openness;
    bool wasOpening;

    public float Openness => openness;

    void Start()
    {
        var controller = FindObjectOfType<PlayerController>();
        if (controller != null) player = controller.transform;
        Apply(0f);
    }

    void Update()
    {
        bool opening = player != null && Vector2.Distance(player.position, transform.position) <= openRadius;
        if (opening && !wasOpening) SfxPlayer.Play(Sfx.Door);
        wasOpening = opening;
        float target = opening ? 1f : 0f;
        if (Mathf.Approximately(openness, target)) return;
        openness = Mathf.MoveTowards(openness, target, openSpeed * Time.deltaTime);
        Apply(openness * openness * (3f - 2f * openness));
    }

    void Apply(float eased)
    {
        float half = span * 0.5f;
        // Longitud ajustada a píxeles enteros para que el pixel art no "tiemble" al abrirse.
        float length = Mathf.Round(Mathf.Lerp(half, MinLeaf, eased) / PixelSize) * PixelSize;
        Place(leafA, -half + length * 0.5f, length);
        Place(leafB, half - length * 0.5f, length);
        if (statusLight != null) statusLight.color = Color.Lerp(closedColor, openColor, eased);
    }

    void Place(SpriteRenderer leaf, float along, float length)
    {
        if (leaf == null) return;
        var size = leaf.size;
        if (horizontal)
        {
            leaf.size = new Vector2(length, size.y);
            leaf.transform.localPosition = new Vector3(along, leaf.transform.localPosition.y, 0f);
        }
        else
        {
            leaf.size = new Vector2(size.x, length);
            leaf.transform.localPosition = new Vector3(leaf.transform.localPosition.x, along, 0f);
        }
    }
}
