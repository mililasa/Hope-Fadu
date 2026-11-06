using System.IO;
using UnityEditor;
using UnityEngine;

// Al pasar el target a WebGL, Unity reimporta el audio en paralelo y deja
// clips sin archivo. FMOD entonces dice "File not found". Este editor no
// acepta fijar AAC a mano: con el target en WebGL, el import ya lo codifica.
// Se cambia el hash del import y se reimporta de a uno para no pisar FSBTool.
[InitializeOnLoad]
public static class HopeWebGlAudioRepair
{
    const string FlagPath = "Library/HopeWebGlAudioRepair.done";
    const string Mark = "hope-webgl-1";

    static HopeWebGlAudioRepair()
    {
        EditorApplication.delayCall += RunWhenReady;
    }

    [MenuItem("Hope/Reimportar audio WebGL")]
    public static void ReimportFromMenu()
    {
        if (File.Exists(FlagPath))
            File.Delete(FlagPath);
        Repair(true);
    }

    static void RunWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunWhenReady;
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Repair(false);
    }

    static void Repair(bool force)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            return;
        if (!force && File.Exists(FlagPath))
            return;

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
        int repaired = 0;
        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Audio WebGL", Path.GetFileName(path), guids.Length == 0 ? 1f : (float)i / guids.Length))
                    return;

                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                    continue;

                if (!force && importer.userData == Mark)
                {
                    repaired++;
                    continue;
                }

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.CompressedInMemory;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                settings.preloadAudioData = true;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                if (settings.sampleRateOverride < 8000 || settings.sampleRateOverride > 192000)
                    settings.sampleRateOverride = 48000;

                importer.defaultSampleSettings = settings;
                importer.userData = Mark;
                importer.SaveAndReimport();
                repaired++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (repaired == 0)
            return;

        File.WriteAllText(FlagPath, "reimported " + repaired);
        Debug.Log("Hope: se reimportaron " + repaired + " clips para WebGL. Volvé a hacer el build antes de subirlo.");
    }
}
