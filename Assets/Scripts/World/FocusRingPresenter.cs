using UnityEngine;

/// <summary>Muestra un anillo pulsante bajo el objeto con el que Alex puede interactuar.</summary>
public class FocusRingPresenter : MonoBehaviour
{
    public SpriteRenderer ring;
    public Color color = new Color(0.13f, 0.89f, 0.94f, 1f);
    public float pulseSpeed = 5f;

    Transform target;
    Vector2 offset;

    void OnEnable()
    {
        PlayerInteractor.FocusChanged += OnFocusChanged;
        if (ring != null) ring.enabled = false;
    }

    void OnDisable() => PlayerInteractor.FocusChanged -= OnFocusChanged;

    void OnFocusChanged(Interactable interactable)
    {
        target = interactable != null ? interactable.transform : null;
        offset = interactable != null ? interactable.focusOffset : Vector2.zero;
        if (ring != null) ring.enabled = target != null;
    }

    void LateUpdate()
    {
        if (ring == null || target == null) return;
        ring.transform.position = (Vector2)target.position + offset;
        float k = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
        ring.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.45f, 1f, k));
    }
}
