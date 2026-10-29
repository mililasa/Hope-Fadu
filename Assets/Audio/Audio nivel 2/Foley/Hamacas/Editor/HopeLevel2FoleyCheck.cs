using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class HopeLevel2FoleyCheck
{
    const string Key = "Hope.FoleyN2Check";
    static double inicio;
    static PlayerController2D hope;
    static Rigidbody2D cuerpo;
    static HopeLevel2SwingAudio hamacas;
    static AudioSource ambiente, capa;
    static Vector3 posicion;
    static bool control, simula, primero, segundo, continuidad;
    static float cursor, escala;
    static HopeLevel2FoleyCheck() { EditorApplication.playModeStateChanged += Cambio; }
    [MenuItem("Hope/Audio/Probar hamacas y capa nivel 2")]
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
            hope = UnityEngine.Object.FindObjectOfType<PlayerController2D>(); hamacas = hope.GetComponent<HopeLevel2SwingAudio>();
            ambiente = hope.GetComponentsInChildren<AudioSource>().Single(a => a.name == "F - Hamacas nivel 2");
            capa = hope.GetComponentsInChildren<AudioSource>().Single(a => a.name == "F - Capa doble salto nivel 2");
            if (hope.GetComponent<HopeJumpLanding>() == null || capa.outputAudioMixerGroup == null) throw new Exception("Falta reproductor o salida de capa.");
            cuerpo = hope.GetComponent<Rigidbody2D>(); posicion = hope.transform.position; control = hope.enabled; simula = cuerpo.simulated; escala = Time.timeScale;
            hope.enabled = false; cuerpo.simulated = false; primero = segundo = continuidad = false; cursor = 0;
            inicio = EditorApplication.timeSinceStartup; EditorApplication.update += Actualizar;
        }
        catch (Exception ex) { Finalizar(ex.Message); }
    }
    static void Saltar(bool doble)
    {
        typeof(PlayerController2D).GetMethod("IniciarSalto", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hope, new object[] { hope.alturaSalto, false, doble });
    }
    static void Actualizar()
    {
        try
        {
            double t = EditorApplication.timeSinceStartup - inicio;
            Bounds z = hamacas.ZonaConjunto(); float margen = Mathf.Abs(hope.moveSpeed);
            if (t < 1.2) hope.transform.position = new Vector3(z.min.x - margen * .5f, z.center.y, 0);
            else if (t < 4.2) hope.transform.position = new Vector3(Mathf.Lerp(z.min.x, z.max.x, (float)((t - 1.2) / 3)), z.center.y + Mathf.Sin((float)t * 7), 0);
            else hope.transform.position = new Vector3(z.max.x + margen * 2, z.center.y, 0);
            if (t > .8 && !primero)
            {
                if (!ambiente.isPlaying || ambiente.volume <= .01f) throw new Exception("Hamacas no anticipan la llegada.");
                Saltar(false); if (capa.isPlaying) throw new Exception("Capa suena en primer salto."); primero = true;
            }
            if (t > 1.4 && !segundo)
            {
                Saltar(true); if (!capa.isPlaying || capa.volume <= 0) throw new Exception("No suena capa en doble salto."); segundo = true;
            }
            if (t > 1 && t < 4.1)
            {
                if (!ambiente.isPlaying || ambiente.time + .03f < cursor) throw new Exception("Hamacas se interrumpen o reinician.");
                cursor = ambiente.time; continuidad = true;
            }
            if (t > 5.8)
            {
                if (ambiente.volume > .001f) throw new Exception("Hamacas no terminan el fade al salir.");
                if (!primero || !segundo || !continuidad) throw new Exception("Prueba incompleta.");
                Finalizar(null);
            }
        }
        catch (Exception ex) { Finalizar(ex.Message); }
    }
    static void Finalizar(string error)
    {
        EditorApplication.update -= Actualizar; SessionState.SetBool(Key, false); Time.timeScale = escala;
        if (hope != null) { hope.transform.position = posicion; hope.enabled = control; }
        if (cuerpo != null) cuerpo.simulated = simula;
        File.WriteAllText("Temp/HopeLevel2FoleyValidation.json", "{\"correcto\":" + (error == null ? "true" : "false") + ",\"detalle\":\"" + (error ?? "Hamacas anticipadas y continuas entre cuatro apoyos, fade al salir; capa solo en doble salto, controlador conectado.").Replace("\"", "'") + "\"}");
        Debug.Log("Hope: prueba Foley N2: " + (error ?? "correcta")); EditorApplication.isPlaying = false;
    }
}
