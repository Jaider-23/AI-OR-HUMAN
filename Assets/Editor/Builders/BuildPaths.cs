using System.IO;
using UnityEditor;

/// <summary>Rutas de assets usadas por los constructores del proyecto.</summary>
public static class BuildPaths
{
    public const string Sprites = "Assets/Sprites";
    public const string Animation = "Assets/Animation";
    public const string Fonts = "Assets/Fonts";
    public const string GameScene = "Assets/Scenes/GameScene.unity";
    public const string MenuScene = "Assets/Scenes/MainMenuScene.unity";

    public const string RunDown = Sprites + "/run_down_40x40.png";
    public const string RunUp = Sprites + "/run_up_40x40.png";
    public const string RunLeft = Sprites + "/run_left_40x40.png";
    public const string RunRight = Sprites + "/run_right_40x40.png";
    public const string HoloF0 = Sprites + "/panel_holo_on_f0.png";
    public const string HoloF1 = Sprites + "/panel_holo_on_f1.png";
    public const string Interior = Sprites + "/pixel-cyberpunk-interior.png";
    public const string Aria = Sprites + "/aria.jpg";
    public const string LeakedImage = Sprites + "/Gemini_Generated_Image_nzmmxnzmmxnzmmxn.jpg";
    public const string Logo = Sprites + "/logo.jpeg";
    public const string MenuBackground = Sprites + "/fondo menu.jpeg";
    public const string CreditsBackground = Sprites + "/fondo creditos.jpeg";

    public const string TmpFonts = "Assets/TextMesh Pro/Resources/Fonts & Materials";
    public const string OrbitronBoldTtf = TmpFonts + "/Orbitron-Bold.ttf";
    public const string OrbitronDynamic = Fonts + "/Orbitron-Bold Dynamic SDF.asset";
    public const string BodyFont = TmpFonts + "/LiberationSans SDF.asset";

    /// <summary>Crea la carpeta (y sus padres) si no existe.</summary>
    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
