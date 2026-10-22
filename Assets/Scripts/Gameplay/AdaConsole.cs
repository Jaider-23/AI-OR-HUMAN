using UnityEngine;

/// <summary>Consola holográfica de ADA: al usarla, ADA comenta el caso actual.</summary>
public class AdaConsole : Interactable
{
    public override string PromptText => "Hablar con ADA";

    public override void Interact(PlayerInteractor interactor)
    {
        SfxPlayer.Play(Sfx.Click);
        if (GameManager.Instance != null) GameManager.Instance.TalkToAda();
    }
}
