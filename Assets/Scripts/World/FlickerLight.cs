using UnityEngine;

public enum LightMode
{
    Breathe,
    Pulse,
    Flicker,
    Blink
}

/// <summary>
/// Anima la intensidad (alfa) de un brillo o neón: respiración suave, pulso, parpadeo de neón
/// defectuoso o encendido/apagado tipo LED. Cada instancia se desfasa según su posición.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FlickerLight : MonoBehaviour
{
    public LightMode mode = LightMode.Breathe;
    [Range(0f, 1f)] public float baseAlpha = 0.6f;
    [Range(0f, 1f)] public float amplitude = 0.25f;
    public float speed = 1.5f;

    SpriteRenderer sprite;
    float phase;
    float nextToggle;
    bool blinkOn = true;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        var p = transform.position;
        phase = Mathf.Abs(p.x * 1.37f + p.y * 2.71f) % (Mathf.PI * 2f);
    }

    void Update()
    {
        float alpha = baseAlpha;
        float t = Time.time * speed + phase;
        switch (mode)
        {
            case LightMode.Breathe:
                alpha += Mathf.Sin(t) * amplitude * 0.5f;
                break;
            case LightMode.Pulse:
                alpha += (Mathf.Sin(t) * 0.5f + 0.5f) * amplitude;
                break;
            case LightMode.Flicker:
                float noise = Mathf.PerlinNoise(t, phase);
                alpha = noise < 0.18f ? baseAlpha * 0.25f : baseAlpha + (noise - 0.5f) * amplitude;
                break;
            case LightMode.Blink:
                if (Time.time >= nextToggle)
                {
                    blinkOn = !blinkOn;
                    nextToggle = Time.time + (blinkOn ? Random.Range(0.4f, 2.2f) : Random.Range(0.08f, 0.35f)) / Mathf.Max(0.1f, speed);
                }
                alpha = blinkOn ? baseAlpha : baseAlpha * 0.1f;
                break;
        }
        var c = sprite.color;
        c.a = Mathf.Clamp01(alpha);
        sprite.color = c;
    }
}
