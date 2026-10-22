using System.Collections;
using UnityEngine;

/// <summary>
/// Fragmento de datos coleccionable repartido por el laboratorio (mecánica de recolección).
/// Se recoge al tocarlo y otorga puntos; reunirlos todos da una bonificación.
/// El trigger queda quieto; solo el hijo visual flota (en pasos de 1 píxel) y brilla.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DataFragment : MonoBehaviour
{
    public const int PointsPerFragment = 10;
    const float PixelSize = 1f / 16f;

    public Transform visual;
    public SpriteRenderer glow;
    public float bobPixels = 2f;
    public float bobSpeed = 2.5f;

    Vector3 visualBase;
    bool collected;

    /// <summary>Lo consulta el autoguardado para recordar qué fragmentos ya no están en el mapa.</summary>
    public bool Collected => collected;

    /// <summary>
    /// Retira un fragmento que ya se había recogido en la partida guardada:
    /// sin puntos, sin sonido y sin animación (los puntos se contaron en su día).
    /// </summary>
    public void MarkAlreadyCollected()
    {
        collected = true;
        var trigger = GetComponent<Collider2D>();
        if (trigger != null) trigger.enabled = false;
        gameObject.SetActive(false);
    }

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (visual != null) visualBase = visual.localPosition;
    }

    void Update()
    {
        if (collected || visual == null) return;
        float wave = Mathf.Sin(Time.time * bobSpeed + transform.position.x);
        visual.localPosition = visualBase + Vector3.up * (Mathf.Round(wave * bobPixels) * PixelSize);
        if (glow != null) glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, 0.45f + wave * 0.2f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || other.GetComponentInParent<PlayerController>() == null) return;
        collected = true;
        GetComponent<Collider2D>().enabled = false;
        SfxPlayer.Play(Sfx.Collect);
        GameSession.AddFragment();
        GameSession.AddPoints(PointsPerFragment);
        if (GameManager.Instance != null) GameManager.Instance.OnFragmentCollected();
        StartCoroutine(CollectEffect());
    }

    /// <summary>El fragmento sube y se desvanece mientras el brillo destella.</summary>
    IEnumerator CollectEffect()
    {
        const float duration = 0.35f;
        var sprite = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            if (visual != null) visual.localPosition = visualBase + Vector3.up * (Mathf.Round(k * 10f) * PixelSize);
            if (sprite != null) sprite.color = new Color(1f, 1f, 1f, 1f - k);
            if (glow != null) glow.color = new Color(glow.color.r, glow.color.g, glow.color.b, (1f - k) * 1.2f);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
