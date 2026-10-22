using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Prueba de extremo a extremo de las colisiones: Alex camina (física real, sin teletransporte)
/// hasta cada terminal, cada fragmento y la consola de ADA. Si queda atascado más de
/// <see cref="StuckSeconds"/> en un tramo libre según la rejilla, hay una pared invisible o un enganche.
/// </summary>
public class WalkingTests
{
    const float GridStep = 0.25f;
    const float StuckSeconds = 2.5f;
    const float WaypointTolerance = 0.14f;

    struct Target
    {
        public string Name;
        public Vector2 Position;
        public float Radius;
        public System.Func<Vector2, bool> Accept;
        /// <summary>Para fragmentos: el objetivo se cumple al recogerlo (no por una distancia exacta).</summary>
        public System.Func<bool> Collected;
    }

    const float CollectWaitSeconds = 1f;

    [UnityTest, Timeout(900000)]
    public IEnumerator AlexWalksToEveryTerminalFragmentAndAda()
    {
        GameSessionBootstrap.Started = false;
        // El recorrido completo no debe sobrescribir ni borrar la partida guardada del jugador.
        SaveSystem.Enabled = false;
        yield return SceneManager.LoadSceneAsync("GameScene");
        yield return null;
        var gm = GameManager.Instance;
        Assert.IsNotNull(gm, "GameManager no encontrado");
        yield return CloseDialogue(gm.dialogue);

        var player = Object.FindObjectOfType<PlayerController>();
        var body = player.GetComponent<Rigidbody2D>();
        var feet = player.GetComponents<Collider2D>().First(c => !c.isTrigger);
        var walls = GameObject.Find("WallColliders").GetComponent<CompositeCollider2D>();
        Assert.Greater(walls.pathCount, 0, "Las paredes no tienen geometría de colisión");
        var grid = new NavGrid(feet, walls.bounds, GridStep);

        Time.timeScale = 2f;
        try
        {
            foreach (var target in Targets(gm))
            {
                var path = grid.FindPath(body.position, target.Position, target.Radius * 0.7f, target.Accept);
                Assert.IsNotNull(path, $"No existe camino libre hasta {target.Name}");
                yield return Follow(body, path, target.Name, gm.dialogue);
                if (target.Collected != null)
                {
                    for (float t = 0f; !target.Collected() && t < CollectWaitSeconds; t += Time.unscaledDeltaTime) yield return null;
                    Assert.IsTrue(target.Collected(), $"Alex pasó por {target.Name} pero no lo recogió (quedó en {body.position})");
                    continue;
                }
                Assert.LessOrEqual(Vector2.Distance(body.position, target.Position), target.Radius,
                                   $"Alex no llegó a {target.Name} (quedó en {body.position})");
                Assert.IsTrue(target.Accept == null || target.Accept(body.position), $"{target.Name} no se puede usar desde {body.position}");
            }
        }
        finally
        {
            PlayerController.ScriptedInput = null;
            Time.timeScale = 1f;
            SaveSystem.Enabled = true;
        }
        Assert.AreEqual(gm.fragments.Length, GameSession.Fragments, "No se recogieron todos los fragmentos caminando");
    }

    static IEnumerable<Target> Targets(GameManager gm)
    {
        foreach (var terminal in gm.terminals.Where(t => t != null && t.gameObject.activeInHierarchy).OrderBy(t => t.caseIndex))
            yield return new Target { Name = terminal.name, Position = terminal.transform.position, Radius = terminal.interactRadius, Accept = terminal.CanInteractFrom };
        foreach (var fragment in gm.fragments.Where(f => f != null))
        {
            var trigger = fragment.GetComponent<Collider2D>();
            yield return new Target
            {
                Name = fragment.name, Position = fragment.transform.position, Radius = 0.3f,
                Collected = () => !trigger.enabled || !fragment.gameObject.activeInHierarchy
            };
        }
        var ada = Object.FindObjectOfType<AdaConsole>();
        if (ada != null) yield return new Target { Name = ada.name, Position = ada.transform.position, Radius = ada.interactRadius, Accept = ada.CanInteractFrom };
    }

    static IEnumerator Follow(Rigidbody2D body, List<Vector2> path, string what, DialogueController dialogue)
    {
        foreach (var waypoint in path)
        {
            float stuck = 0f;
            Vector2 last = body.position;
            while ((waypoint - body.position).magnitude > WaypointTolerance)
            {
                if (dialogue.IsOpen)
                {
                    PlayerController.ScriptedInput = Vector2.zero;
                    yield return CloseDialogue(dialogue);
                }
                PlayerController.ScriptedInput = (waypoint - body.position).normalized;
                yield return new WaitForFixedUpdate();
                if ((body.position - last).sqrMagnitude < 0.000004f)
                {
                    stuck += Time.fixedDeltaTime;
                    if (stuck > StuckSeconds)
                        Assert.Fail($"Alex quedó atascado en {body.position} yendo a {what} (punto {waypoint})");
                }
                else
                {
                    stuck = 0f;
                    last = body.position;
                }
            }
        }
        PlayerController.ScriptedInput = Vector2.zero;
        yield return null;
    }

    static IEnumerator CloseDialogue(DialogueController dialogue)
    {
        for (int guard = 0; dialogue.IsOpen && guard < 60; guard++)
        {
            dialogue.Advance();
            yield return new WaitForSecondsRealtime(0.05f);
        }
    }
}
