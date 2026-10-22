using UnityEngine;

/// <summary>
/// Flotación vertical ajustada a la rejilla de píxeles (1/16 de unidad) para que el pixel art
/// no tiemble con la cámara pixel-perfect.
/// </summary>
public class HoverBob : MonoBehaviour
{
    const float PixelSize = 1f / 16f;

    public float amplitudePixels = 2f;
    public float speed = 2f;

    Vector3 basePosition;
    float phase;

    void Awake()
    {
        basePosition = transform.localPosition;
        phase = Mathf.Abs(transform.position.x * 0.7f + transform.position.y * 1.3f);
    }

    void Update()
    {
        float offset = Mathf.Round(Mathf.Sin(Time.time * speed + phase) * amplitudePixels) * PixelSize;
        transform.localPosition = basePosition + Vector3.up * offset;
    }
}
