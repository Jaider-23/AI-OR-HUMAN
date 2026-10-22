using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD siempre visible (GDD 18.3): caso, nivel, puntuación, reputación, fragmentos,
/// objetivo actual y aviso de interacción.
/// </summary>
public class HUDController : MonoBehaviour
{
    public TextMeshProUGUI caseText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI fragmentsText;
    public TextMeshProUGUI reputationText;
    public Image reputationFill;
    public TextMeshProUGUI objectiveText;
    public GameObject promptRoot;
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI floatingText;

    [Header("Autoguardado")]
    [Tooltip("Aviso breve cuando el juego guarda la partida. Opcional: si falta, no se avisa.")]
    public TextMeshProUGUI autosaveText;

    [Header("Colores de reputación")]
    public Color goodColor = new Color(0.27f, 0.95f, 0.55f);
    public Color midColor = new Color(1f, 0.75f, 0.2f);
    public Color badColor = new Color(1f, 0.3f, 0.42f);

    int totalCases = 1;
    int totalFragments;
    Coroutine floating;
    Coroutine autosaveNotice;
    Vector2 floatingBase;
    float shownScore;
    float shownReputation;
    bool primed;

    void OnEnable()
    {
        GameSession.Changed += Refresh;
        PlayerInteractor.FocusChanged += OnFocusChanged;
        AutoSave.Saved += OnAutoSaved;
        Refresh();
        OnFocusChanged(null);
        if (floatingText != null)
        {
            floatingBase = floatingText.rectTransform.anchoredPosition;
            floatingText.gameObject.SetActive(false);
        }
        if (autosaveText != null) autosaveText.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        GameSession.Changed -= Refresh;
        PlayerInteractor.FocusChanged -= OnFocusChanged;
        AutoSave.Saved -= OnAutoSaved;
    }

    public void Configure(int caseCount, int fragmentCount)
    {
        totalCases = Mathf.Max(1, caseCount);
        totalFragments = fragmentCount;
        Refresh();
    }

    public void SetLevel(int level, int maxLevel) => levelText.text = $"NIVEL {level}/{maxLevel}";

    public void SetObjective(string objective) => objectiveText.text = objective;

    public void Refresh()
    {
        int shown = Mathf.Min(GameSession.CaseIndex + 1, totalCases);
        caseText.text = $"CASO {shown:D2}/{totalCases:D2}";
        fragmentsText.text = $"DATOS {GameSession.Fragments}/{totalFragments}";
        if (!primed)
        {
            shownScore = GameSession.Score;
            shownReputation = GameSession.Reputation;
            primed = true;
        }
        ApplyAnimatedStats();
    }

    /// <summary>Puntos y reputación "cuentan" hasta su nuevo valor en lugar de saltar.</summary>
    void Update()
    {
        float score = GameSession.Score, reputation = GameSession.Reputation;
        if (Mathf.Approximately(shownScore, score) && Mathf.Approximately(shownReputation, reputation)) return;
        shownScore = Approach(shownScore, score);
        shownReputation = Approach(shownReputation, reputation);
        ApplyAnimatedStats();
    }

    static float Approach(float from, float to)
    {
        float step = Mathf.Max(1f, Mathf.Abs(to - from) * 5f) * Time.unscaledDeltaTime;
        return Mathf.MoveTowards(from, to, step);
    }

    void ApplyAnimatedStats()
    {
        scoreText.text = $"PUNTOS {Mathf.RoundToInt(shownScore):D4}";
        int reputation = Mathf.RoundToInt(shownReputation);
        reputationText.text = $"REPUTACIÓN DIGITAL {reputation}%";
        reputationFill.fillAmount = shownReputation / 100f;
        // El ancho del relleno refleja el porcentaje (funciona con o sin sprite).
        reputationFill.rectTransform.anchorMax = new Vector2(shownReputation / 100f, 1f);
        reputationFill.color = reputation >= 70 ? goodColor : reputation >= 40 ? midColor : badColor;
    }

    void OnFocusChanged(Interactable target)
    {
        if (promptRoot == null) return;
        promptRoot.SetActive(target != null);
        if (target != null) promptText.text = $"[E]  {target.PromptText}";
    }

    /// <summary>Avisa de que la partida quedó guardada, sin interrumpir el juego.</summary>
    void OnAutoSaved()
    {
        if (autosaveText == null || !isActiveAndEnabled) return;
        if (autosaveNotice != null) StopCoroutine(autosaveNotice);
        autosaveNotice = StartCoroutine(AutosaveNoticeRoutine());
    }

    IEnumerator AutosaveNoticeRoutine()
    {
        const float hold = 1f;
        const float fade = 0.7f;
        autosaveText.text = "PARTIDA GUARDADA";
        autosaveText.gameObject.SetActive(true);
        for (float t = 0f; t < hold + fade; t += Time.unscaledDeltaTime)
        {
            float alpha = t < hold ? 1f : 1f - (t - hold) / fade;
            autosaveText.color = new Color(goodColor.r, goodColor.g, goodColor.b, alpha);
            yield return null;
        }
        autosaveText.gameObject.SetActive(false);
        autosaveNotice = null;
    }

    /// <summary>Muestra un texto flotante breve, por ejemplo "+150".</summary>
    public void Flash(string message, Color color)
    {
        if (floatingText == null) return;
        if (floating != null) StopCoroutine(floating);
        floating = StartCoroutine(FlashRoutine(message, color));
    }

    IEnumerator FlashRoutine(string message, Color color)
    {
        floatingText.gameObject.SetActive(true);
        floatingText.text = message;
        var rect = floatingText.rectTransform;
        Vector2 start = floatingBase;
        const float duration = 1.4f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            rect.anchoredPosition = start + Vector2.up * (60f * k);
            floatingText.color = new Color(color.r, color.g, color.b, 1f - k * k);
            yield return null;
        }
        rect.anchoredPosition = start;
        floatingText.gameObject.SetActive(false);
    }
}
