using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Hace "respirar" toda una capa de tiles (las líneas de neón de las paredes).</summary>
[RequireComponent(typeof(Tilemap))]
public class TilemapPulse : MonoBehaviour
{
    [Range(0f, 1f)] public float minIntensity = 0.55f;
    [Range(0f, 1f)] public float maxIntensity = 1f;
    public float speed = 1.2f;

    Tilemap tilemap;

    void Awake() => tilemap = GetComponent<Tilemap>();

    void Update()
    {
        float k = Mathf.Lerp(minIntensity, maxIntensity, Mathf.Sin(Time.time * speed) * 0.5f + 0.5f);
        tilemap.color = new Color(k, k, k, 1f);
    }
}
