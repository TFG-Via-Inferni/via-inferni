using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HudPixelSpritePreset
{
    private const string SelectionMenuPath = "Tools/Via Inferni/Sprites/Apply HUD Pixel Preset to Selection";
    private const string FolderMenuPath = "Tools/Via Inferni/Sprites/Apply HUD Pixel Preset to Folder...";

    [MenuItem(SelectionMenuPath)]
    private static void ApplyPresetToSelection()
    {
        HashSet<string> texturePaths = CollectPngTexturePathsFromSelection();
        ApplyPreset(texturePaths);
    }

    [MenuItem(SelectionMenuPath, true)]
    private static bool ValidateApplyPresetToSelection()
    {
        return Selection.objects != null && Selection.objects.Length > 0;
    }

    [MenuItem(FolderMenuPath)]
    private static void ApplyPresetToFolder()
    {
        string absoluteFolderPath = EditorUtility.OpenFolderPanel("Select HUD texture folder", Application.dataPath, string.Empty);
        if (string.IsNullOrWhiteSpace(absoluteFolderPath))
        {
            return;
        }

        string relativeFolderPath = ToProjectRelativePath(absoluteFolderPath);
        if (string.IsNullOrEmpty(relativeFolderPath))
        {
            EditorUtility.DisplayDialog(
                "Invalid Folder",
                "The selected folder must be inside this Unity project's Assets folder.",
                "OK");
            return;
        }

        HashSet<string> texturePaths = CollectPngTexturePathsFromFolder(relativeFolderPath);
        ApplyPreset(texturePaths);
    }

    private static HashSet<string> CollectPngTexturePathsFromSelection()
    {
        HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (UnityEngine.Object selectedObject in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selectedObject);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            if (AssetDatabase.IsValidFolder(path))
            {
                foreach (string folderTexturePath in CollectPngTexturePathsFromFolder(path))
                {
                    result.Add(folderTexturePath);
                }

                continue;
            }

            if (IsPng(path))
            {
                result.Add(path);
            }
        }

        return result;
    }

    private static HashSet<string> CollectPngTexturePathsFromFolder(string folderPath)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (IsPng(path))
            {
                result.Add(path);
            }
        }

        return result;
    }

    private static void ApplyPreset(HashSet<string> texturePaths)
    {
        if (texturePaths == null || texturePaths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "No PNG textures found",
                "No PNG textures were found in the current selection.",
                "OK");
            return;
        }

        int changedCount = 0;
        List<string> sortedPaths = new List<string>(texturePaths);
        sortedPaths.Sort(StringComparer.OrdinalIgnoreCase);

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < sortedPaths.Count; i++)
            {
                string path = sortedPaths[i];
                EditorUtility.DisplayProgressBar("Applying HUD pixel preset", path, (float)(i + 1) / sortedPaths.Count);

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool changed = false;
                changed |= SetIfDifferent(() => importer.textureType, value => importer.textureType = value, TextureImporterType.Sprite);
                changed |= SetIfDifferent(() => importer.spritePixelsPerUnit, value => importer.spritePixelsPerUnit = value, 100f);
                changed |= SetIfDifferent(() => importer.spritePivot, value => importer.spritePivot = value, new Vector2(0.5f, 0.5f));
                changed |= SetIfDifferent(() => importer.wrapMode, value => importer.wrapMode = value, TextureWrapMode.Clamp);
                changed |= SetIfDifferent(() => importer.filterMode, value => importer.filterMode = value, FilterMode.Point);
                changed |= SetIfDifferent(() => importer.anisoLevel, value => importer.anisoLevel = value, 0);
                changed |= SetIfDifferent(() => importer.mipmapEnabled, value => importer.mipmapEnabled = value, false);
                changed |= SetIfDifferent(() => importer.alphaIsTransparency, value => importer.alphaIsTransparency = value, true);
                changed |= SetIfDifferent(() => importer.npotScale, value => importer.npotScale = value, TextureImporterNPOTScale.None);
                changed |= SetIfDifferent(() => importer.textureCompression, value => importer.textureCompression = value, TextureImporterCompression.Uncompressed);
                changed |= SetIfDifferent(() => importer.crunchedCompression, value => importer.crunchedCompression = value, false);

                // HUD icons should be imported as a single sprite.
                changed |= SetIfDifferent(() => importer.spriteImportMode, value => importer.spriteImportMode = value, SpriteImportMode.Single);

                if (!changed)
                {
                    continue;
                }

                changedCount++;
                importer.SaveAndReimport();
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "HUD Pixel Preset Applied",
            $"Processed {sortedPaths.Count} PNG texture(s). Updated {changedCount} texture importer(s).",
            "OK");
    }

    private static string ToProjectRelativePath(string absolutePath)
    {
        string normalizedAbsolutePath = absolutePath.Replace('\\', '/');
        string normalizedAssetsPath = Application.dataPath.Replace('\\', '/');

        if (!normalizedAbsolutePath.StartsWith(normalizedAssetsPath, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return "Assets" + normalizedAbsolutePath.Substring(normalizedAssetsPath.Length);
    }

    private static bool IsPng(string path)
    {
        return ".png".Equals(Path.GetExtension(path), StringComparison.OrdinalIgnoreCase);
    }

    private static bool SetIfDifferent<T>(Func<T> getter, Action<T> setter, T newValue)
    {
        T currentValue = getter();
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
        {
            return false;
        }

        setter(newValue);
        return true;
    }
}
