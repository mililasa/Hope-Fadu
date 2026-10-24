using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class HopeLevel2PlatformAudioCheck
{
    const string Key = "Hope.PlatformAudioCheck";
    static double inicio;
    static HopeLevel2PlatformAudio[] plataformas;
    static PlayerController2D hope;
    static Rigidbody2D cuerpo;
    static Vector3 posicion;
    static bool controlActivo, simulaba;
    static int indiceAnterior = -1;
    static Behaviour movimientoActual;
    static bool movimientoHabilitado;
    static List<Registro> registros;
    [Serializable] public class Registro
    {
        public string plataforma;
        public float velocidadMaxima, volumenMaximo, picoAudio, volumenDetenida = -1, volumenLejana = -1;
        public int inversiones;
        public bool correcto;
    }
    [Serializable] public class Informe { public bool correcto; public string error; public Registro[] plataformas; }
    static HopeLevel2PlatformAudioCheck()
    {
        EditorApplication.playModeStateChanged += Cambio;
    }
    [MenuItem("Hope/Audio/Probar plataformas nivel 2")]
    static void Ejecutar()
    {
        if (EditorSceneManager.GetActiveScene().name != "Nivel_2" || EditorApplication.isPlayingOrWillChangePlaymode)
        { Debug.LogWarning("Hope: abrir Nivel_2 fuera de Play para probar plataformas."); return; }
        SessionState.SetBool(Key, true);
        EditorApplication.isPlaying = true;
    }
    static void Cambio(PlayModeStateChange estado)
    {
        if (estado == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false))
        {
            EditorApplication.delayCall += Iniciar;
        }
        if (estado == PlayModeStateChange.ExitingPlayMode) { EditorApplication.update -= Actualizar; SessionState.SetBool(Key, false); }
    }
    static void Iniciar()
    {
        try
        {
            plataformas = UnityEngine.Object.FindObjectsOfType<HopeLevel2PlatformAudio>().OrderBy(p => p.transform.position.x).ToArray();
            hope = UnityEngine.Object.FindObjectOfType<PlayerController2D>();
            if (plataformas.Length != 5 || hope == null) throw new Exception("Se esperaban cinco plataformas y Hope.");
            cuerpo = hope.GetComponent<Rigidbody2D>(); posicion = hope.transform.position;
            controlActivo = hope.enabled; simulaba = cuerpo.simulated;
            hope.enabled = false; cuerpo.simulated = false;
            registros = plataformas.Select(p => new Registro { plataforma = p.name }).ToList();
            indiceAnterior = -1; inicio = EditorApplication.timeSinceStartup;
            EditorApplication.update += Actualizar;
            Debug.Log("Hope: prueba de plataformas en Play, 50 segundos. Solo cambios temporales; las escenas no se guardan.");
        }
        catch (Exception ex) { Finalizar(ex.Message); }
    }
    static void Actualizar()
    {
        if (!EditorApplication.isPlaying) return;
        try
        {
            double elapsed = EditorApplication.timeSinceStartup - inicio;
            int indice = (int)(elapsed / 10);
            if (indice >= plataformas.Length) { Finalizar(null); return; }
            int audibles = plataformas.Count(item => item.GetComponentsInChildren<AudioSource>().Any(a => a.name.StartsWith("M - Plataforma -") && a.isPlaying && a.volume > .001f));
            if (audibles > 1) throw new Exception("Se superponen plataformas audibles.");
            foreach (var item in plataformas)
                if (item.ErrorRitmico > 1.0 / 48000) throw new Exception("Disparo fuera de la rejilla musical.");
            var p = plataformas[indice]; var r = registros[indice];
            if (indice != indiceAnterior)
            {
                if (movimientoActual != null) movimientoActual.enabled = movimientoHabilitado;
                movimientoActual = (Behaviour)p.GetComponent<PlataformaMovil>() ?? p.GetComponent<PlataformaMovilHorizontal>();
                movimientoHabilitado = movimientoActual.enabled; indiceAnterior = indice;
            }
            double fase = elapsed % 10;
            movimientoActual.enabled = fase < 8 && movimientoHabilitado;
            hope.transform.position = p.transform.position + (fase < 9 ? Vector3.up : Vector3.right * 100);
            var sources = p.GetComponentsInChildren<AudioSource>().Where(a => a.name.StartsWith("M - Plataforma -")).ToArray();
            var loops = sources.Where(a => !a.name.EndsWith("Inversion")).ToArray();
            if (sources.Any(a => a.loop)) throw new Exception("Una capa quedó en loop: " + p.name);
            if (loops.Length != 3) throw new Exception("Faltan capas en " + p.name);
            float volumen = loops.Max(a => a.volume);
            if (fase > .5 && fase < 8)
            {
                r.velocidadMaxima = Mathf.Max(r.velocidadMaxima, p.VelocidadActual);
                r.volumenMaximo = Mathf.Max(r.volumenMaximo, volumen);
                float[] muestras = new float[256]; loops[0].GetOutputData(muestras, 0);
                r.picoAudio = Mathf.Max(r.picoAudio, muestras.Max(v => Mathf.Abs(v)));
            }
            if (fase > 8.8 && fase < 9) r.volumenDetenida = volumen;
            if (fase > 9.8) r.volumenLejana = volumen;
            r.inversiones = p.Inversiones;
        }
        catch (Exception ex) { Finalizar(ex.Message); }
    }
    static void Finalizar(string error)
    {
        EditorApplication.update -= Actualizar; SessionState.SetBool(Key, false);
        if (movimientoActual != null) movimientoActual.enabled = movimientoHabilitado;
        if (hope != null) { hope.transform.position = posicion; hope.enabled = controlActivo; }
        if (cuerpo != null) cuerpo.simulated = simulaba;
        var datos = registros == null ? new Registro[0] : registros.ToArray();
        foreach (var r in datos) r.correcto = r.velocidadMaxima > .1f && r.volumenMaximo > .2f && r.inversiones > 0 && r.volumenDetenida >= 0 && r.volumenDetenida < .005f && r.volumenLejana >= 0 && r.volumenLejana < .005f;
        bool ok = error == null && datos.Length == 5 && datos.All(r => r.correcto);
        File.WriteAllText("Temp/HopePlatformAudioValidation.json", JsonUtility.ToJson(new Informe { correcto = ok, error = error, plataformas = datos }, true));
        Debug.Log("Hope: prueba de plataformas terminada. Resultado: " + ok + ". Informe en Temp/HopePlatformAudioValidation.json");
        EditorApplication.isPlaying = false;
    }
}
