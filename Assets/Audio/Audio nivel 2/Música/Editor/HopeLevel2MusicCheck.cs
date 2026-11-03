using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class HopeLevel2MusicCheck
{
    const string Key = "Hope.Level2MusicCheck";
    static double inicio;
    static AudioSource[] fuentes;
    static bool salto, pausa, reanudado, envolvio;
    static float escala;
    static int deriva;
    static float pico;
    [Serializable] public class Informe { public bool correcto; public string error; public int desfaseMaximoMuestras; public int margenLecturaMuestras; public float picoDeSenal; public bool cierreLoop; public bool pausaYReanudacion; }
    static HopeLevel2MusicCheck() { EditorApplication.playModeStateChanged += Cambio; }
    [MenuItem("Hope/Audio/Probar mÃƒÂºsica nivel 2")]
    static void Ejecutar()
    {
        if (EditorSceneManager.GetActiveScene().name != "Nivel_2" || EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Cambio(PlayModeStateChange estado)
    {
        if (estado == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false)) EditorApplication.delayCall += Iniciar;
        if (estado == PlayModeStateChange.ExitingPlayMode) { EditorApplication.update -= Actualizar; SessionState.SetBool(Key, false); }
    }
    static void Iniciar()
    {
        try
        {
            var musica = UnityEngine.Object.FindObjectsOfType<HopeLevel2Music>();
            if (musica.Length != 1) throw new Exception("Se esperaba una ÃƒÂºnica mÃƒÂºsica de Nivel 2.");
            fuentes = musica[0].GetComponentsInChildren<AudioSource>().OrderBy(a => a.name).ToArray();
            if (fuentes.Length != 2 || fuentes.Any(a => a.clip == null || a.outputAudioMixerGroup == null || !a.loop || a.pitch != 1)) throw new Exception("Faltan clips o ruteo de mÃƒÂºsica.");
            if (fuentes[0].clip.samples != 2457600 || fuentes[1].clip.samples != 2457600) throw new Exception("DuraciÃƒÂ³n importada distinta al loop de 51.2 s.");
            if (UnityEngine.Object.FindObjectsOfType<HopeLevelMusic>().Length != 0) throw new Exception("Hay mÃƒÂºsica antigua duplicada.");
            escala = Time.timeScale; inicio = EditorApplication.timeSinceStartup;
            salto = pausa = reanudado = envolvio = false; deriva = 0; pico = 0;
            EditorApplication.update += Actualizar;
        }
        catch (Exception ex) { Finalizar(ex.Message); }
    }
    static void Actualizar()
    {
        try
        {
            double t = EditorApplication.timeSinceStartup - inicio;
            if (t < .7) return;
            double antes = AudioSettings.dspTime;
            int muestra0 = fuentes[0].timeSamples, muestra1 = fuentes[1].timeSamples;
            double despues = AudioSettings.dspTime;
            // Ignore reads straddling an audio-thread buffer boundary.
            if (antes == despues && !(pausa && !reanudado))
            {
                int diferencia = Math.Abs(muestra0 - muestra1);
                diferencia = Math.Min(diferencia, fuentes[0].clip.samples - diferencia);
                deriva = Math.Max(deriva, diferencia);
            }
            foreach (var a in fuentes)
            {
                float[] data = new float[256]; a.GetOutputData(data, 0);
                pico = Mathf.Max(pico, data.Max(x => Mathf.Abs(x)));
            }
            if (t > 2 && !salto)
            {
                int fin = fuentes[0].clip.samples - 48000;
                fuentes[0].Stop(); fuentes[1].Stop();
                fuentes[0].timeSamples = fin; fuentes[1].timeSamples = fin;
                double cuando = AudioSettings.dspTime + .1;
                fuentes[0].PlayScheduled(cuando); fuentes[1].PlayScheduled(cuando); salto = true;
            }
            if (t > 3.5 && t < 5) envolvio |= fuentes.All(a => a.isPlaying && a.timeSamples < 240000);
            if (t > 5 && !pausa) { Time.timeScale = 0; pausa = true; }
            if (t > 5.5 && !reanudado) { Time.timeScale = escala; reanudado = true; }
            if (t > 8)
            {
                // timeSamples queries cross native voice processing boundaries;
                // one DSP block of readback jitter is not sustained playback drift.
                AudioSettings.GetDSPBufferSize(out int bloque, out int cantidad);
                if (!envolvio || !fuentes.All(a => a.isPlaying) || deriva > bloque || pico <= .00001f) throw new Exception("Revisar sincronÃƒÂ­a, seÃƒÂ±al o cierre del loop.");
                Finalizar(null);
            }
        }
        catch (Exception ex) { Finalizar(ex.Message); }
    }
    static void Finalizar(string error)
    {
        EditorApplication.update -= Actualizar; SessionState.SetBool(Key, false); Time.timeScale = escala;
        AudioSettings.GetDSPBufferSize(out int margen, out int cantidadBuffers);
        File.WriteAllText("Temp/HopeLevel2MusicValidation.json", JsonUtility.ToJson(new Informe { correcto = error == null, error = error, desfaseMaximoMuestras = deriva, margenLecturaMuestras = margen, picoDeSenal = pico, cierreLoop = envolvio, pausaYReanudacion = reanudado }, true));
        Debug.Log("Hope: prueba de mÃƒÂºsica N2 terminada: " + (error ?? "correcta") + ". Informe: Temp/HopeLevel2MusicValidation.json");
        EditorApplication.isPlaying = false;
    }
}
