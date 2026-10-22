#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Prueba de juego automática (solo Editor). Se activa desde "AI OR HUMAN/Prueba automática":
/// recorre todos los casos usando herramientas, ADA, veredicto, confianza y compartir,
/// y guarda capturas en ClaudeBridge/playtest.
/// </summary>
public class PlaytestDriver : MonoBehaviour
{
    public const string SessionKey = "aih_playtest";
    const float Step = 0.6f;

    string outDir;
    int shot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!UnityEditor.SessionState.GetBool(SessionKey, false)) return;
        UnityEditor.SessionState.SetBool(SessionKey, false);
        new GameObject("[PlaytestDriver]").AddComponent<PlaytestDriver>();
    }

    IEnumerator Start()
    {
        // Recorre los 11 casos: sin esto sobrescribiría la partida guardada del jugador.
        SaveSystem.Enabled = false;
        outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ClaudeBridge", "playtest"));
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);
        Log("inicio");

        yield return new WaitForSeconds(1.5f);
        var gm = GameManager.Instance;
        if (gm == null) { Log("ERROR sin GameManager"); yield break; }
        yield return Shot("intro_dialogo");
        yield return SkipDialogue(gm.dialogue);

        var player = FindObjectOfType<PlayerController>();
        for (int i = 0; i < gm.cases.Length; i++)
        {
            var terminal = FindTerminal(gm, i);
            if (terminal == null) { Log($"ERROR sin terminal {i}"); yield break; }
            player.GetComponent<Rigidbody2D>().position = (Vector2)terminal.transform.position + Vector2.down * 0.8f;
            yield return new WaitForSeconds(Step);
            if (i == 0) yield return Shot("terminal_activa");

            terminal.Interact(player.GetComponent<PlayerInteractor>());
            yield return new WaitForSeconds(Step);
            if (i == 0 || i == 5) yield return Shot($"caso_{i + 1}_intro");
            yield return SkipDialogue(gm.dialogue);
            yield return SolveCase(gm, i);
            yield return new WaitForSeconds(Step);
        }

        yield return new WaitForSeconds(Step);
        yield return Shot("final_dialogo");
        yield return SkipDialogue(gm.dialogue);
        yield return new WaitForSeconds(Step);
        yield return Shot("final_resumen");
        Log($"FIN score={GameSession.Score} rep={GameSession.Reputation} fragments={GameSession.Fragments}");
        SaveSystem.Enabled = true;
    }

    IEnumerator SolveCase(GameManager gm, int index)
    {
        var panel = gm.investigation;
        var data = gm.cases[index];
        if (!panel.IsOpen) { Log($"ERROR panel no abierto en {data.caseId}"); yield break; }

        foreach (var button in panel.toolButtons)
        {
            button.onClick.Invoke();
            yield return new WaitForSeconds(0.75f);
        }
        panel.adaButton.onClick.Invoke();
        // El caso 4 se falla a propósito para comprobar penalizaciones.
        var verdict = index == 3 ? (Verdict)(((int)data.correctVerdict + 1) % 3) : data.correctVerdict;
        panel.verdictGroup.buttons[(int)verdict].onClick.Invoke();
        panel.confidenceGroup.buttons[index % 3].onClick.Invoke();
        if (data.canShare) panel.shareGroup.buttons[index == 6 ? 0 : 1].onClick.Invoke();
        yield return new WaitForSeconds(Step);
        if (index == 0 || index == 4 || index == 6) yield return Shot($"caso_{index + 1}_investigacion");

        panel.confirmButton.onClick.Invoke();
        yield return new WaitForSeconds(Step);
        if (index == 0 || index == 3 || index == 6) yield return Shot($"caso_{index + 1}_resultado");
        Log($"{data.caseId} score={GameSession.Score} rep={GameSession.Reputation}");
        panel.feedback.continueButton.onClick.Invoke();
    }

    static CaseTerminal FindTerminal(GameManager gm, int index)
    {
        foreach (var t in gm.terminals)
        {
            if (t != null && t.caseIndex == index) return t;
        }
        return null;
    }

    IEnumerator SkipDialogue(DialogueController dialogue)
    {
        for (int guard = 0; dialogue.IsOpen && guard < 40; guard++)
        {
            dialogue.Advance();
            yield return new WaitForSeconds(0.05f);
        }
    }

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        string path = Path.Combine(outDir, $"{++shot:D2}_{name}.png");
        ScreenCapture.CaptureScreenshot(path);
        yield return null;
    }

    void Log(string message)
    {
        Debug.Log($"[Playtest] {message}");
        File.AppendAllText(Path.Combine(outDir, "log.txt"), message + "\n");
    }
}
#endif
