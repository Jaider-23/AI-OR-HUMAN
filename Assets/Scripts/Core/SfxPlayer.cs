using System.Collections.Generic;
using UnityEngine;

public enum Sfx
{
    Click,
    Scan,
    Correct,
    Wrong,
    Collect,
    Alert,
    Type,
    Door,
    Step
}

/// <summary>
/// Efectos de sonido sintetizados en tiempo de ejecución (éxito, error, alerta, escaneo...)
/// para cumplir el GDD 19 sin depender de archivos de audio externos.
/// </summary>
public class SfxPlayer : MonoBehaviour
{
    const int SampleRate = 44100;

    static SfxPlayer instance;
    readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();
    AudioSource source;

    public static void Play(Sfx sfx)
    {
        EnsureInstance();
        if (instance.clips.TryGetValue(sfx, out var clip)) instance.source.PlayOneShot(clip);
    }

    static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("[SfxPlayer]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SfxPlayer>();
    }

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.volume = 0.35f;
        clips[Sfx.Click] = Tone("click", new[] { 880f }, 0.05f, 0.6f);
        clips[Sfx.Type] = Tone("type", new[] { 1400f }, 0.015f, 0.25f);
        clips[Sfx.Scan] = Sweep("scan", 400f, 1600f, 0.35f);
        clips[Sfx.Correct] = Tone("correct", new[] { 660f, 880f, 1320f }, 0.09f, 0.8f);
        clips[Sfx.Wrong] = Tone("wrong", new[] { 220f, 160f }, 0.16f, 0.9f, square: true);
        clips[Sfx.Collect] = Tone("collect", new[] { 988f, 1319f }, 0.07f, 0.7f);
        clips[Sfx.Alert] = Tone("alert", new[] { 520f, 390f, 520f, 390f }, 0.1f, 0.6f, square: true);
        clips[Sfx.Door] = Whoosh("door", 0.28f, 0.35f);
        clips[Sfx.Step] = Tone("step", new[] { 150f }, 0.035f, 0.18f);
    }

    /// <summary>Ruido filtrado con envolvente (puerta neumática). Semilla fija: siempre suena igual.</summary>
    static AudioClip Whoosh(string name, float seconds, float gain)
    {
        int count = Mathf.CeilToInt(seconds * SampleRate);
        var data = new float[count];
        var random = new System.Random(7);
        float smooth = 0f;
        for (int i = 0; i < count; i++)
        {
            float k = (float)i / count;
            float white = (float)(random.NextDouble() * 2.0 - 1.0);
            smooth = Mathf.Lerp(smooth, white, Mathf.Lerp(0.05f, 0.25f, k));
            data[i] = smooth * Mathf.Sin(Mathf.PI * k) * gain;
        }
        return Build(name, data);
    }

    static AudioClip Tone(string name, float[] notes, float noteSeconds, float gain, bool square = false)
    {
        int perNote = Mathf.CeilToInt(noteSeconds * SampleRate);
        var data = new float[perNote * notes.Length];
        for (int n = 0; n < notes.Length; n++)
        {
            for (int i = 0; i < perNote; i++)
            {
                float t = (float)i / SampleRate;
                float wave = Mathf.Sin(2f * Mathf.PI * notes[n] * t);
                if (square) wave = Mathf.Sign(wave) * 0.5f;
                float envelope = 1f - (float)i / perNote;
                data[n * perNote + i] = wave * envelope * gain;
            }
        }
        return Build(name, data);
    }

    static AudioClip Sweep(string name, float from, float to, float seconds)
    {
        int count = Mathf.CeilToInt(seconds * SampleRate);
        var data = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float k = (float)i / count;
            phase += 2f * Mathf.PI * Mathf.Lerp(from, to, k) / SampleRate;
            data[i] = Mathf.Sin(phase) * Mathf.Sin(Mathf.PI * k) * 0.4f;
        }
        return Build(name, data);
    }

    static AudioClip Build(string name, float[] data)
    {
        var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
