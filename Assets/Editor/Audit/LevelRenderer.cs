using UnityEngine;

/// <summary>Renderiza una zona del mundo a una Texture2D legible con una cámara ortográfica temporal.</summary>
public static class LevelRenderer
{
    public static Texture2D Render(Bounds bounds, float pixelsPerUnit)
    {
        int width = Mathf.CeilToInt(bounds.size.x * pixelsPerUnit);
        int height = Mathf.CeilToInt(bounds.size.y * pixelsPerUnit);
        var go = new GameObject("AuditCamera");
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = bounds.extents.y;
        cam.aspect = (float)width / height;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.05f);
        go.transform.position = new Vector3(bounds.center.x, bounds.center.y, -20f);

        var rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(go);
        return texture;
    }
}
