using System.IO;
using UnityEditor;
using UnityEngine;
using DiceBattle.UI;

namespace DiceBattle.EditorTools
{
    /// <summary>
    /// 유니티 내장 UI 스프라이트를 프로젝트 에셋으로 복사한다
    /// (<b>DiceBattle → 내장 UI 스프라이트 추출</b>).
    ///
    /// <b>왜 필요한가.</b> 내장 UI 스프라이트(UISprite, Background 등)는
    /// <c>unity_builtin_extra</c>에 들어 있어 <b>에디터에서만</b> 읽을 수 있다.
    /// 코드로 참조하면 에디터에서는 멀쩡하다가 빌드에서 null이 되어, 실기기에서만
    /// 모서리가 각지게 나오는 식으로 조용히 깨진다.
    ///
    /// 그래서 에디터에서 픽셀을 그대로 떠서 Assets 안에 PNG로 저장한다. 저장된 뒤로는
    /// 평범한 프로젝트 에셋이라 빌드에 정상적으로 들어간다.
    ///
    /// 눈대중으로 비슷한 그림을 만드는 것이 아니라 <b>9-슬라이스 경계와 PPU까지
    /// 원본 값을 그대로 옮기므로</b>, 인스펙터에서 맞춰 본 모양이 그대로 재현된다.
    ///
    /// 이 파일은 Editor 폴더에 있어 빌드에 포함되지 않는다.
    /// </summary>
    public static class BuiltinSpriteExtractor
    {
        /// <summary>내장 리소스 경로. 인스펙터의 스프라이트 선택 창에 뜨는 이름과 같다.</summary>
        private const string SourcePath = "UI/Skin/UISprite.psd";

        private const string OutputPath = "Assets/Arts/ui_rounded_panel.png";

        [MenuItem("DiceBattle/내장 UI 스프라이트 추출")]
        private static void Extract()
        {
            var source = AssetDatabase.GetBuiltinExtraResource<Sprite>(SourcePath);
            if (source == null)
            {
                EditorUtility.DisplayDialog("추출 실패",
                    $"내장 스프라이트를 찾지 못했습니다.\n{SourcePath}", "확인");
                return;
            }

            Texture2D copy = ReadPixels(source);
            if (copy == null)
            {
                EditorUtility.DisplayDialog("추출 실패",
                    "텍스처를 읽지 못했습니다.", "확인");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? "Assets");
            File.WriteAllBytes(OutputPath, copy.EncodeToPNG());
            Object.DestroyImmediate(copy);

            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);
            ApplyImportSettings(source);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OutputPath);
            string wired = AssignToSkin(sprite) ? "UiSkin에 자동 연결했습니다." : "UiSkin 에셋을 찾지 못해 연결은 직접 해 주세요.";

            EditorUtility.DisplayDialog("추출 완료",
                $"{OutputPath}\n\n" +
                $"크기 {source.rect.width}x{source.rect.height}\n" +
                $"9-슬라이스 {source.border}\n" +
                $"PPU {source.pixelsPerUnit}\n\n{wired}", "확인");

            Selection.activeObject = sprite;
        }

        /// <summary>
        /// 내장 텍스처는 읽기 전용이라 GetPixels를 바로 부를 수 없다.
        /// RenderTexture로 한 번 그린 뒤 화면에서 되읽는다.
        /// </summary>
        private static Texture2D ReadPixels(Sprite source)
        {
            Texture2D texture = source.texture;
            if (texture == null) return null;

            var rt = RenderTexture.GetTemporary(texture.width, texture.height, 0,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(texture, rt);
            RenderTexture.active = rt;

            var full = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            full.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            full.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            // 스프라이트가 텍스처 일부만 쓰는 경우를 대비해 실제 영역만 잘라 낸다.
            Rect r = source.rect;
            if (Mathf.Approximately(r.width, texture.width)
                && Mathf.Approximately(r.height, texture.height))
                return full;

            var cropped = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
            cropped.SetPixels(full.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height));
            cropped.Apply();
            Object.DestroyImmediate(full);
            return cropped;
        }

        /// <summary>
        /// 원본의 9-슬라이스 경계와 PPU를 그대로 옮긴다.
        /// 이 값이 틀리면 모서리가 늘어나 원본과 다른 모양이 된다.
        /// </summary>
        private static void ApplyImportSettings(Sprite source)
        {
            var importer = AssetImporter.GetAtPath(OutputPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = source.border;          // (좌, 하, 우, 상)
            importer.spritePixelsPerUnit = source.pixelsPerUnit;
            importer.mipmapEnabled = false;                 // UI라 밉맵이 필요 없다
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 64;                   // 원본이 32x32 남짓이다
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            importer.SaveAndReimport();
        }

        /// <summary>추출한 스프라이트를 UiSkin 에셋의 roundedPanel에 꽂는다.</summary>
        private static bool AssignToSkin(Sprite sprite)
        {
            if (sprite == null) return false;

            string[] guids = AssetDatabase.FindAssets("t:UiSkin");
            if (guids.Length == 0) return false;

            var skin = AssetDatabase.LoadAssetAtPath<UiSkin>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
            if (skin == null) return false;

            skin.roundedPanel = sprite;
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
