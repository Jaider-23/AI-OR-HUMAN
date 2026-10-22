using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Transición entre escenas: fundido a negro con una línea de escaneo cian (GDD 19).</summary>
public class SceneFader : MonoBehaviour
{
    const float FadeOutSeconds = 0.28f;
    const float FadeInSeconds = 0.35f;

    static SceneFader instance;

    CanvasGroup group;
    RectTransform scanLine;
    bool busy;

    public static void LoadScene(string sceneName)
    {
        EnsureInstance();
        if (instance.busy) return;
        instance.StartCoroutine(instance.Run(sceneName));
    }

    static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("[SceneFader]", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        DontDestroyOnLoad(go);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        instance = go.AddComponent<SceneFader>();
        instance.group = go.GetComponent<CanvasGroup>();
        instance.group.alpha = 0f;
        instance.group.blocksRaycasts = false;

        var black = new GameObject("Black", typeof(RectTransform), typeof(Image));
        var blackRect = (RectTransform)black.transform;
        blackRect.SetParent(go.transform, false);
        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        black.GetComponent<Image>().color = new Color(0.01f, 0.01f, 0.03f, 1f);

        var line = new GameObject("ScanLine", typeof(RectTransform), typeof(Image));
        instance.scanLine = (RectTransform)line.transform;
        instance.scanLine.SetParent(go.transform, false);
        instance.scanLine.anchorMin = new Vector2(0f, 0.5f);
        instance.scanLine.anchorMax = new Vector2(1f, 0.5f);
        instance.scanLine.sizeDelta = new Vector2(0f, 4f);
        line.GetComponent<Image>().color = new Color(0.13f, 0.89f, 0.94f, 0.85f);
    }

    IEnumerator Run(string sceneName)
    {
        busy = true;
        group.blocksRaycasts = true;
        yield return Fade(0f, 1f, FadeOutSeconds);
        Time.timeScale = 1f;
        var load = SceneManager.LoadSceneAsync(sceneName);
        while (!load.isDone) yield return null;
        yield return Fade(1f, 0f, FadeInSeconds);
        group.blocksRaycasts = false;
        busy = false;
    }

    IEnumerator Fade(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float k = t / seconds;
            group.alpha = Mathf.Lerp(from, to, k);
            scanLine.anchorMin = new Vector2(0f, k);
            scanLine.anchorMax = new Vector2(1f, k);
            yield return null;
        }
        group.alpha = to;
    }
}
