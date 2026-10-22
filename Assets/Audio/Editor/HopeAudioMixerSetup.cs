#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

[InitializeOnLoad]
internal static class HopeAudioMixerSetup
{
    const string Raiz = "Assets/Audio/";
    static readonly (string categoria, string carpeta, string grupo)[] Buses =
    {
        ("Ambientes", "Ambientes", "Ambientes"),
        ("Foley", "Foley", "Foley"),
        ("Mecanicas", "Mec" + (char)0x00e1 + "nicas", "Mec" + (char)0x00e1 + "nicas"),
        ("Musica", "M" + (char)0x00fa + "sica", "M" + (char)0x00fa + "sica")
    };

    static HopeAudioMixerSetup()
    {
        EditorApplication.delayCall += ConfigurarAlImportar;
    }

    static void ConfigurarAlImportar()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
        try { Configurar(); }
        catch (Exception e) { Debug.LogError("Hope Audio Mixer: " + e); }
    }

    [MenuItem("Hope/Audio/Configurar mezcladores")]
    public static void Configurar()
    {
        const string generalPath = Raiz + "Mixer.mixer";
        AudioMixer general = AssetDatabase.LoadAssetAtPath<AudioMixer>(generalPath);
        if (general == null) throw new Exception("No se pudo abrir " + generalPath);

        foreach (var bus in Buses)
        {
            string grupoPath = "Master/" + bus.grupo;
            AudioMixerGroup destino = BuscarGrupo(general, grupoPath);
            if (destino == null)
            {
                object controlador = Controlador(generalPath);
                Type tipo = controlador.GetType();
                object creado = tipo.GetMethod("CreateNewGroup", BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(controlador, new object[] { bus.grupo, true });
                object master = tipo.GetProperty("masterGroup", BindingFlags.Public | BindingFlags.Instance).GetValue(controlador);
                tipo.GetMethod("AddChildToParent", BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(controlador, new[] { creado, master });
                EditorUtility.SetDirty((UnityEngine.Object)creado);
                EditorUtility.SetDirty(general);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                general = AssetDatabase.LoadAssetAtPath<AudioMixer>(generalPath);
                destino = BuscarGrupo(general, grupoPath);
                if (destino == null) throw new Exception("No se pudo crear el grupo " + grupoPath);
            }

            string carpeta = Raiz + bus.carpeta + "/Resources/";
            string ruta = carpeta + "HopeMixer" + bus.categoria + ".mixer";
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            Directory.CreateDirectory(Path.Combine(projectRoot, carpeta.Replace('/', Path.DirectorySeparatorChar)));
            AudioMixer sub = AssetDatabase.LoadAssetAtPath<AudioMixer>(ruta);
            if (sub == null)
            {
                Type tipo = typeof(Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController", true);
                MethodInfo crear = tipo.GetMethod("CreateMixerControllerAtPath", BindingFlags.Public | BindingFlags.Static);
                crear.Invoke(null, new object[] { ruta });
                AssetDatabase.Refresh();
                sub = AssetDatabase.LoadAssetAtPath<AudioMixer>(ruta);
                if (sub == null) throw new Exception("No se pudo crear " + ruta);
            }
            sub.outputAudioMixerGroup = destino;
            EditorUtility.SetDirty(sub);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Hope Audio Mixer: cuatro submixers conectados al Mixer general. Los AudioSource se asignan por categorÃ­a al crearse.");
    }

    static AudioMixerGroup BuscarGrupo(AudioMixer mixer, string ruta)
    {
        AudioMixerGroup[] grupos = mixer.FindMatchingGroups(ruta);
        return grupos != null && grupos.Length == 1 ? grupos[0] : null;
    }

    static object Controlador(string ruta)
    {
        Type tipo = typeof(Editor).Assembly.GetType("UnityEditor.Audio.AudioMixerController", true);
        UnityEngine.Object[] objetos = AssetDatabase.LoadAllAssetsAtPath(ruta);
        foreach (UnityEngine.Object objeto in objetos)
            if (tipo.IsInstanceOfType(objeto)) return objeto;
        throw new Exception("No se encontrÃ³ el controlador del mixer " + ruta);
    }
}
#endif