using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
[InitializeOnLoad]
public static class HopeMusicTransitionCheck
{
    const string Key = "Hope.MusicTransitionCheck";
    static int etapa, identidad, muestra;
    static double inicio;
    static float escala;
    static AudioSource salida;
    static PlayerController2D jugador;
    static HopeMusicTransitionCheck() { EditorApplication.playModeStateChanged += Cambio; }
    [MenuItem("Hope/Audio/Probar transiciones 1-2-3")]
    static void Ejecutar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Cambio(PlayModeStateChange estado)
    {
        if (estado == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key, false)) EditorApplication.delayCall += Iniciar;
        if (estado == PlayModeStateChange.ExitingPlayMode) { EditorApplication.update -= Actualizar; SessionState.SetBool(Key, false); }
    }
    static void Iniciar()
    {
        escala = Time.timeScale; Time.timeScale = 1;
        etapa = 0; inicio = EditorApplication.timeSinceStartup;
        SceneManager.LoadScene("SampleScene"); EditorApplication.update += Actualizar;
    }
    static void Paso(int siguiente) { etapa = siguiente; inicio = EditorApplication.timeSinceStartup; }
    static void Requerir(bool condicion, string error) { if (!condicion) throw new Exception(error); }
    static void Actualizar()
    {
        try
        {
            double t = EditorApplication.timeSinceStartup - inicio;
            switch (etapa)
            {
                case 0:
                    if (t < 1) return;
                    jugador = UnityEngine.Object.FindObjectOfType<PlayerController2D>();
                    salida = jugador.GetComponent<HopeLevelMusic>().ObtenerSalidaParaTransicion();
                    Requerir(salida != null && salida.isPlaying, "Falta música de salida de Nivel 1.");
                    identidad = salida.GetInstanceID(); muestra = salida.timeSamples;
                    jugador.SendMessage("PasarANivel2"); Paso(1); break;
                case 1:
                    if (t < .7) return;
                    Requerir(SceneManager.GetActiveScene().name == "Nivel_2", "No cargó Nivel 2.");
                    Requerir(salida != null && salida.GetInstanceID() == identidad && salida.isPlaying && salida.timeSamples > muestra, "Se reinició o cortó la salida al entrar a Nivel 2.");
                    Requerir(HopeMusicTransition.GananciaFondo(SceneManager.GetActiveScene()) < .01f, "El fondo de Nivel 2 entra demasiado pronto.");
                    Paso(2); break;
                case 2:
                    if (t < 5.3) return;
                    Requerir(HopeMusicTransition.GananciaFondo(SceneManager.GetActiveScene()) > .99f && !salida.isPlaying, "No terminó el fundido hacia la música de Nivel 2.");
                    jugador = UnityEngine.Object.FindObjectOfType<PlayerController2D>(); jugador.enabled = false;
                    jugador.GetComponent<Rigidbody2D>().simulated = false;
                    var fin = GameObject.Find("NIVEL3").GetComponent<Collider2D>();
                    jugador.transform.position = fin.bounds.center + Vector3.left * 4;
                    Paso(3); break;
                case 3:
                    if (t < 2.6) return;
                    Requerir(salida.isPlaying && HopeMusicTransition.GananciaFondo(SceneManager.GetActiveScene()) < .01f, "No entra la salida al final de Nivel 2.");
                    muestra = salida.timeSamples;
                    jugador.SendMessage("PasarANivel3"); Paso(4); break;
                case 4:
                    if (t < .7) return;
                    Requerir(SceneManager.GetActiveScene().name == "Nivel_3", "No cargó Nivel 3.");
                    Requerir(salida != null && salida.GetInstanceID() == identidad && salida.isPlaying && salida.timeSamples > muestra, "Se cortó o reinició la música al entrar al faro.");
                    Requerir(HopeMusicTransition.GananciaFondo(SceneManager.GetActiveScene()) < .01f, "Se superpone el fondo al entrar al faro.");
                    Paso(5); break;
                case 5:
                    if (t < 5.3) return;
                    Requerir(!salida.isPlaying && HopeMusicTransition.GananciaFondo(SceneManager.GetActiveScene()) > .99f, "No termina el fundido de entrada a Nivel 3.");
                    Finalizar(null); break;
            }
        }
        catch (Exception ex) { Finalizar(ex.Message); }
    }
    static void Finalizar(string error)
    {
        EditorApplication.update -= Actualizar; SessionState.SetBool(Key, false); Time.timeScale = escala;
        File.WriteAllText("Temp/HopeMusicTransitionValidation.json", "{\"correcto\":" + (error == null ? "true" : "false") + ",\"etapa\":" + etapa + ",\"detalle\":\"" + (error ?? "Salida 1-2 continua; entrada en final de 2; salida 2-3 continua; ambos fundidos completos.").Replace("\"", "'") + "\"}");
        Debug.Log("Hope: prueba transiciones: " + (error ?? "correcta")); EditorApplication.isPlaying = false;
    }
}
