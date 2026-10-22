using UnityEngine;
using UnityEngine.UI;

/// <summary>Pausa con Escape: continuar o volver al menú principal.</summary>
public class PauseMenu : MonoBehaviour
{
    public GameObject root;
    public Button resumeButton;
    public Button menuButton;

    void Awake()
    {
        if (root != null) root.SetActive(false);
        resumeButton.onClick.AddListener(Resume);
        menuButton.onClick.AddListener(GoToMenu);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (root.activeSelf) Resume();
        else if (!InputGate.IsLocked) Pause();
    }

    void Pause()
    {
        root.SetActive(true);
        InputGate.Lock(this);
        Time.timeScale = 0f;
        SfxPlayer.Play(Sfx.Click);
    }

    void Resume()
    {
        root.SetActive(false);
        InputGate.Unlock(this);
        Time.timeScale = 1f;
        SfxPlayer.Play(Sfx.Click);
    }

    void GoToMenu()
    {
        Time.timeScale = 1f;
        InputGate.Clear();
        SceneFader.LoadScene(EndScreen.MenuScene);
    }
}
