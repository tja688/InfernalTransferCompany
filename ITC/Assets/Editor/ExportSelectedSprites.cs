using UnityEngine;
using UnityEditor;
using System.IO;

public static class ExportSelectedSprites
{
    [MenuItem("Assets/Export/Export Selected Sprites to PNG", true)]
    static bool Validate()
    {
        foreach (var obj in Selection.objects)
            if (obj is Sprite) return true;
        return false;
    }

    [MenuItem("Assets/Export/Export Selected Sprites to PNG")]
    static void ExportSprites()
    {
        var objs = Selection.objects;
        if (objs == null || objs.Length == 0) return;

        string baseDir = "Assets/ExportedSprites";
        if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

        int count = 0;
        foreach (var obj in objs)
        {
            if (!(obj is Sprite sprite)) continue;

            // 确保源贴图可读
            var path = AssetDatabase.GetAssetPath(sprite.texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (!importer.isReadable) { importer.isReadable = true; changed = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
                if (changed) { importer.SaveAndReimport(); }
            }

            var r = sprite.textureRect;
            var src = sprite.texture;

            var tex = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
            var pixels = src.GetPixels(
                Mathf.FloorToInt(r.x),
                Mathf.FloorToInt(r.y),
                Mathf.FloorToInt(r.width),
                Mathf.FloorToInt(r.height)
            );
            tex.SetPixels(pixels);
            tex.Apply();

            var png = tex.EncodeToPNG();
            string file = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(baseDir, sprite.name + ".png"));
            File.WriteAllBytes(file, png);
            count++;
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Export Sprites", $"导出完成：{count} 张 PNG 到 {baseDir}", "OK");
    }
}
