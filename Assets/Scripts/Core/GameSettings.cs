using UnityEngine;

/// <summary>Configuración del jugador persistida en PlayerPrefs (menú CONFIGURACIÓN).</summary>
public static class GameSettings
{
    const string VolumeKey = "aih_volume";
    const string TextSpeedKey = "aih_text_speed";
    const float DefaultVolume = 0.8f;
    const float DefaultTextSpeed = 45f;

    public static float Volume => PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);

    /// <summary>Caracteres por segundo del efecto máquina de escribir.</summary>
    public static float TextSpeed => PlayerPrefs.GetFloat(TextSpeedKey, DefaultTextSpeed);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyOnStartup() => AudioListener.volume = Volume;

    public static void SetVolume(float value)
    {
        float clamped = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, clamped);
        AudioListener.volume = clamped;
    }

    public static void SetTextSpeed(float charsPerSecond)
    {
        PlayerPrefs.SetFloat(TextSpeedKey, Mathf.Clamp(charsPerSecond, 10f, 200f));
    }

    public static void SetFullscreen(bool fullscreen) => Screen.fullScreen = fullscreen;

    /// <summary>Guarda en disco una sola vez (no en cada movimiento del slider: en WebGL es costoso).</summary>
    public static void Persist() => PlayerPrefs.Save();
}
