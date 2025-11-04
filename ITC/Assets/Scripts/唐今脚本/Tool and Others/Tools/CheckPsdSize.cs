// Assets/Editor/CheckPsdSize.cs
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Reflection;

public static class CheckPsdSize
{
    [MenuItem("Tools/Textures/Check Selected PSD Sizes")]
    static void CheckSelected()
    {
        foreach (var obj in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            var importer = AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogWarning($"No importer: {path}");
                continue;
            }

            // 导入后的尺寸（用于对比）
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var imported = tex ? $"{tex.width}x{tex.height}" : "(not imported)";

            int srcW = -1, srcH = -1;

            // 1) 普通 TextureImporter 直接用官方 API
            if (importer is TextureImporter ti)
            {
                ti.GetSourceTextureWidthAndHeight(out srcW, out srcH);
            }
            else
            {
                // 2) 尝试 PSDImporter（Package: com.unity.2d.psdimporter）
                //    先反射看看有没有类似 GetTextureActualWidthAndHeight 的方法（不同版本命名略有差异）
                var t = importer.GetType(); // UnityEditor.U2D.PSD.PSDImporter
                var m = t.GetMethod("GetTextureActualWidthAndHeight",
                                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (m != null)
                {
                    object[] args = new object[] { 0, 0 };
                    // 有的版本签名是(out int w, out int h)，如果签名不匹配就走文件头解析
                    try
                    {
                        var parms = m.GetParameters();
                        if (parms.Length == 2 && parms[0].IsOut && parms[1].IsOut)
                        {
                            object[] outArgs = new object[] { 0, 0 };
                            m.Invoke(importer, outArgs);
                            srcW = (int)outArgs[0];
                            srcH = (int)outArgs[1];
                        }
                    }
                    catch { /* ignore and fallback */ }
                }

                // 3) 仍然没拿到 → 直接读 PSD 文件头（最通用）
                if (srcW <= 0 || srcH <= 0)
                {
                    try
                    {
                        using (var fs = File.OpenRead(path))
                        using (var br = new BinaryReader(fs))
                        {
                            // '8BPS'
                            var sig = br.ReadBytes(4);
                            // 版本
                            ushort ver = ReadBEU16(br);
                            // reserved 6 bytes
                            br.ReadBytes(6);
                            // channels
                            ushort channels = ReadBEU16(br);
                            // height/width（大端）
                            uint h = ReadBEU32(br);
                            uint w = ReadBEU32(br);
                            // depth / color mode
                            ushort depth = ReadBEU16(br);
                            ushort color = ReadBEU16(br);
                            srcW = (int)w;
                            srcH = (int)h;
                        }
                    }
                    catch
                    {
                        // 兜底：至少别报“Not a texture”
                    }
                }
            }

            string source = (srcW > 0 && srcH > 0) ? $"{srcW}x{srcH}" : "(source size unknown)";
            Debug.Log($"{path}\n  Source: {source}\n  Imported: {imported}");
        }
    }

    static ushort ReadBEU16(BinaryReader br)
    {
        var b = br.ReadBytes(2);
        return (ushort)((b[0] << 8) | b[1]);
    }
    static uint ReadBEU32(BinaryReader br)
    {
        var b = br.ReadBytes(4);
        return (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
    }
}
