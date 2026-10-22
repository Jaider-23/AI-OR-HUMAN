using System.Collections;
using UnityEngine;

/// <summary>Aparición suave de un panel (fundido + ligera escala) cada vez que se activa.</summary>
[RequireComponent(typeof(RectTransform))]
public class PanelAppear : MonoBehaviour
{
    public float duration = 0.18f;
    public float startScale = 0.95f;

    CanvasGroup group;
    Coroutine routine;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Play());
    }

    void OnDisable()
    {
        group.alpha = 1f;
        transform.localScale = Vector3.one;
    }

    IEnumerator Play()
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            float eased = 1f - (1f - k) * (1f - k);
            group.alpha = eased;
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, eased);
            yield return null;
        }
        group.alpha = 1f;
        transform.localScale = Vector3.one;
        routine = null;
    }
}
