using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Grupo de botones donde solo uno queda seleccionado (veredicto, confianza, compartir).</summary>
[Serializable]
public class ChoiceGroup
{
    public Button[] buttons = Array.Empty<Button>();
    public Color normalColor = new Color(0.08f, 0.13f, 0.22f, 0.95f);
    public Color selectedColor = new Color(0.13f, 0.89f, 0.94f, 1f);

    public int Selected { get; private set; } = -1;
    public bool HasSelection => Selected >= 0;

    public void Bind(Action<int> onSelected)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.RemoveAllListeners();
            buttons[i].onClick.AddListener(() =>
            {
                Select(index);
                onSelected?.Invoke(index);
            });
        }
    }

    public void Select(int index)
    {
        Selected = index;
        for (int i = 0; i < buttons.Length; i++)
        {
            var image = buttons[i].targetGraphic as Image;
            if (image != null) image.color = i == index ? selectedColor : normalColor;
        }
        SfxPlayer.Play(Sfx.Click);
    }

    public void Clear()
    {
        Selected = -1;
        foreach (var button in buttons)
        {
            var image = button.targetGraphic as Image;
            if (image != null) image.color = normalColor;
        }
    }

    public void SetInteractable(bool value)
    {
        foreach (var button in buttons) button.interactable = value;
    }
}
