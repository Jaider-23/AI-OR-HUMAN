using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct DialogueLine
{
    public Speaker speaker;
    [TextArea(2, 5)] public string text;

    public DialogueLine(Speaker speaker, string text)
    {
        this.speaker = speaker;
        this.text = text;
    }
}

/// <summary>
/// Panel de diálogo con retrato, nombre y efecto máquina de escribir.
/// Click / E / Espacio completa la línea o avanza a la siguiente.
/// </summary>
public class DialogueController : MonoBehaviour
{
    /// <summary>Tope de tiempo por fotograma al escribir (un tirón no revela media línea de golpe).</summary>
    const float MaxTypeStep = 0.05f;

    public GameObject root;
    public Image portrait;
    public GameObject phantomMask;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI continueHint;

    [Header("Retratos")]
    public Sprite adaPortrait;
    public Color adaColor = new Color(0.13f, 0.89f, 0.94f);
    public Color phantomColor = new Color(1f, 0.25f, 0.82f);
    public Color systemColor = new Color(1f, 0.6f, 0.24f);

    readonly Queue<DialogueLine> queue = new Queue<DialogueLine>();
    Action onFinished;
    Coroutine typing;
    bool lineComplete;
    float openedAt;

    public bool IsOpen => root != null && root.activeSelf;

    Coroutine glitch;
    Vector2 maskBase;

    void Awake()
    {
        if (root != null) root.SetActive(false);
        if (phantomMask != null) maskBase = ((RectTransform)phantomMask.transform).anchoredPosition;
    }

    /// <summary>Interferencias visuales cuando habla Phantom (desplazamiento y cambio de color).</summary>
    void SetGlitch(bool on)
    {
        if (glitch != null) StopCoroutine(glitch);
        glitch = null;
        if (phantomMask != null) ((RectTransform)phantomMask.transform).anchoredPosition = maskBase;
        if (on && phantomMask != null) glitch = StartCoroutine(Glitch());
    }

    IEnumerator Glitch()
    {
        var rect = (RectTransform)phantomMask.transform;
        while (true)
        {
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.25f, 0.9f));
            for (int i = 0; i < 3; i++)
            {
                rect.anchoredPosition = maskBase + new Vector2(UnityEngine.Random.Range(-6, 7), UnityEngine.Random.Range(-2, 3));
                nameText.color = i % 2 == 0 ? adaColor : phantomColor;
                yield return new WaitForSecondsRealtime(0.04f);
            }
            rect.anchoredPosition = maskBase;
            nameText.color = phantomColor;
        }
    }

    public void Show(IEnumerable<DialogueLine> lines, Action finished = null)
    {
        queue.Clear();
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line.text)) queue.Enqueue(line);
        }
        onFinished = finished;
        if (queue.Count == 0)
        {
            Close();
            return;
        }
        root.SetActive(true);
        InputGate.Lock(this);
        openedAt = Time.unscaledTime;
        NextLine();
    }

    public void Show(Speaker speaker, string text, Action finished = null) =>
        Show(new[] { new DialogueLine(speaker, text) }, finished);

    void Update()
    {
        if (!IsOpen || Time.unscaledTime - openedAt < 0.15f) return;
        bool advance = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E)
                       || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
        if (advance) Advance();
    }

    /// <summary>Completa la línea actual o pasa a la siguiente (equivale a pulsar E).</summary>
    public void Advance()
    {
        if (!IsOpen) return;
        if (!lineComplete) CompleteLine();
        else NextLine();
    }

    void NextLine()
    {
        if (queue.Count == 0)
        {
            Close();
            return;
        }
        var line = queue.Dequeue();
        ApplySpeaker(line.speaker);
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(TypeLine(line.text));
    }

    void ApplySpeaker(Speaker speaker)
    {
        bool isPhantom = speaker == Speaker.Phantom;
        Color color = speaker == Speaker.Ada ? adaColor : isPhantom ? phantomColor : systemColor;
        nameText.text = speaker == Speaker.Ada ? "ADA · ASISTENTE IA"
                      : isPhantom ? "PHANTOM" : "SISTEMA DTA";
        nameText.color = color;
        SetGlitch(isPhantom);
        portrait.sprite = adaPortrait;
        portrait.color = speaker == Speaker.Ada ? Color.white : new Color(0.05f, 0.05f, 0.1f, 1f);
        if (phantomMask != null) phantomMask.SetActive(speaker != Speaker.Ada);
        if (phantomMask != null)
        {
            var label = phantomMask.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = isPhantom ? "?" : "!";
                label.color = color;
            }
        }
        if (isPhantom) SfxPlayer.Play(Sfx.Alert);
    }

    IEnumerator TypeLine(string text)
    {
        lineComplete = false;
        continueHint.enabled = false;
        bodyText.text = text;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();
        int total = bodyText.textInfo.characterCount;
        float charsPerSecond = Mathf.Max(1f, GameSettings.TextSpeed);
        float shown = 0f;
        int lastSound = 0;
        // Se revelan caracteres según el tiempo transcurrido (no uno por fotograma), así la velocidad
        // del texto de CONFIGURACIÓN se respeta con cualquier tasa de fotogramas.
        while (shown < total)
        {
            shown += charsPerSecond * Mathf.Min(Time.unscaledDeltaTime, MaxTypeStep);
            int visible = Mathf.Min(total, Mathf.FloorToInt(shown));
            bodyText.maxVisibleCharacters = visible;
            if (visible / 3 != lastSound)
            {
                lastSound = visible / 3;
                SfxPlayer.Play(Sfx.Type);
            }
            yield return null;
        }
        CompleteLine();
    }

    void CompleteLine()
    {
        if (typing != null) StopCoroutine(typing);
        typing = null;
        bodyText.maxVisibleCharacters = int.MaxValue;
        lineComplete = true;
        continueHint.enabled = true;
    }

    void Close()
    {
        SetGlitch(false);
        if (root != null) root.SetActive(false);
        InputGate.Unlock(this);
        var callback = onFinished;
        onFinished = null;
        callback?.Invoke();
    }
}
