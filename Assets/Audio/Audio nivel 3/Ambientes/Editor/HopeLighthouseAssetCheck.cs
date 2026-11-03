using UnityEditor;
using UnityEngine;
using System.IO;
public static class HopeLighthouseAssetCheck
{
    [InitializeOnLoadMethod]
    static void Preparar() { EditorApplication.delayCall += Revisar; }
    [MenuItem("Hope/Audio/Verificar configuracion del faro")]
    static void Revisar()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Revisar; return; }
        var s = AssetDatabase.LoadAssetAtPath<HopeLighthouseAudioSettings>("Assets/Audio/Audio nivel 3/Ambientes/Resources/HopeLighthouseAudioSettings.asset");
        if (s == null) return;
        bool ok = s.ambientes != null && s.guerra != null && s.salidaAmbientes != null && s.salidaGuerra != null;
        ok &= s.ambientes != null && s.ambientes.viento != null && s.ambientes.presion != null;
        ok &= s.guerra != null && s.guerra.motor != null && s.guerra.eco != null;
        if (s.salidaAmbientes != null) ok &= s.salidaAmbientes.audioMixer.GetFloat("ParedExteriorDb", out _);
        if (s.salidaGuerra != null) ok &= s.salidaGuerra.audioMixer.GetFloat("ParedExteriorDb", out _);
        int pares = 0;
        if (s.versionesInterior != null)
            foreach (var par in s.versionesInterior)
            {
                ok &= par.exterior != null && par.interior != null;
                if (par.exterior == null || par.interior == null) continue;
                ok &= par.exterior.samples == par.interior.samples && par.exterior.frequency == par.interior.frequency;
                pares++;
            }
        ok &= pares == 7;
        Directory.CreateDirectory("Temp");
        File.WriteAllText("Temp/HopeLighthouseAudioAssetValidation.json", "{\"correcto\":" + (ok ? "true" : "false") + ",\"paresInterior\":" + pares + "}");
        if (!ok) Debug.LogError("Hope: revisar referencias o submixers del exterior del faro. Ver Temp/HopeLighthouseAudioAssetValidation.json.");
    }
}
