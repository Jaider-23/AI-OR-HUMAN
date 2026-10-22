using UnityEngine;

/// <summary>
/// Detalles visuales de Alex: sombra a los pies, polvo y sonido de pasos al correr y una
/// respiración de 1 píxel en reposo. La parte visual vive en un hijo para no mover la física.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerVisuals : MonoBehaviour
{
    const float PixelSize = 1f / 16f;

    public Transform visual;
    public ParticleSystem dust;
    [Tooltip("Distancia recorrida entre dos pasos (coincide con el ciclo de carrera).")]
    public float stepDistance = 1.2f;
    public float breathePeriod = 1.4f;

    Rigidbody2D body;
    PlayerController controller;
    Vector2 lastPhysicsPosition;
    Vector3 visualBase;
    float travelled;

    bool IsMoving => controller != null && controller.IsMoving;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        controller = GetComponent<PlayerController>();
        lastPhysicsPosition = body.position;
        if (visual != null) visualBase = visual.localPosition;
    }

    /// <summary>
    /// La distancia se mide en los pasos de física: body.position solo cambia ahí, así que medirla
    /// por fotograma daba cero en la mayoría de fotogramas y los pasos nunca sonaban.
    /// </summary>
    void FixedUpdate()
    {
        Vector2 position = body.position;
        float moved = (position - lastPhysicsPosition).magnitude;
        lastPhysicsPosition = position;
        if (!IsMoving)
        {
            travelled = stepDistance * 0.5f;
            return;
        }
        travelled += moved;
        if (travelled < stepDistance) return;
        travelled = 0f;
        if (dust != null) dust.Emit(2);
        SfxPlayer.Play(Sfx.Step);
    }

    void LateUpdate()
    {
        if (visual == null) return;
        bool inhale = !IsMoving && Mathf.Repeat(Time.time, breathePeriod) > breathePeriod * 0.5f;
        visual.localPosition = visualBase + (inhale ? Vector3.up * PixelSize : Vector3.zero);
    }
}
