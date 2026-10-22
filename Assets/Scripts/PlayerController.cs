using UnityEngine;

/// <summary>
/// Movimiento top-down de Alex con animaciones en 4 direcciones.
/// El Animator usa MoveX/MoveY (dirección), Speed (0 = idle) y LastX/LastY (dirección de reposo).
/// El pivote del sprite está en los pies, así que la posición del objeto es el punto de apoyo.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    static readonly int MoveXId = Animator.StringToHash("MoveX");
    static readonly int MoveYId = Animator.StringToHash("MoveY");
    static readonly int SpeedId = Animator.StringToHash("Speed");
    static readonly int LastXId = Animator.StringToHash("LastX");
    static readonly int LastYId = Animator.StringToHash("LastY");

#if UNITY_EDITOR
    /// <summary>Solo pruebas automáticas: si tiene valor, sustituye a la lectura del teclado.</summary>
    public static Vector2? ScriptedInput;
#endif

    public float moveSpeed = 5f;

    Rigidbody2D rb;
    Animator animator;
    Vector2 moveInput;
    Vector2 lastDirection = Vector2.down;

    public Vector2 FacingDirection => lastDirection;

    /// <summary>Intención de moverse este fotograma (aunque una pared frene el desplazamiento).</summary>
    public bool IsMoving => moveInput.sqrMagnitude > 0.0001f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        moveInput = InputGate.IsLocked ? Vector2.zero : ReadInput();
        if (moveInput != Vector2.zero) lastDirection = DominantAxis(moveInput);
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    static Vector2 ReadInput()
    {
#if UNITY_EDITOR
        if (ScriptedInput.HasValue) return Vector2.ClampMagnitude(ScriptedInput.Value, 1f);
#endif
        var raw = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        return raw.sqrMagnitude > 1f ? raw.normalized : raw;
    }

    /// <summary>Las hojas de sprites solo tienen 4 direcciones: se elige el eje dominante.</summary>
    static Vector2 DominantAxis(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return new Vector2(Mathf.Sign(v.x), 0f);
        return new Vector2(0f, Mathf.Sign(v.y));
    }

    void UpdateAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.SetFloat(MoveXId, lastDirection.x);
        animator.SetFloat(MoveYId, lastDirection.y);
        animator.SetFloat(LastXId, lastDirection.x);
        animator.SetFloat(LastYId, lastDirection.y);
        animator.SetFloat(SpeedId, moveInput.sqrMagnitude);
    }
}
