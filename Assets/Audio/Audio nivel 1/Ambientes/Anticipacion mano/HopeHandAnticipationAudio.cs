using UnityEngine;
using UnityEngine.SceneManagement;
[DisallowMultipleComponent]
public sealed class HopeHandAnticipationAudio : MonoBehaviour
{
    public HopeHandAnticipationSettings ajustes;
    AudioSource fuente;
    bool sonando, pausado;
    float tiempo, duracion;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preparar() { SceneManager.sceneLoaded -= AlCargar; SceneManager.sceneLoaded += AlCargar; }
    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "SampleScene" && escena.name != "Nivel_3") return;
        foreach (var raiz in escena.GetRootGameObjects())
            if (raiz.GetComponentInChildren<HopeHandAnticipationAudio>(true) != null) return;
        var go = new GameObject("Audio " + escena.name + " - Anticipacion de la mano");
        SceneManager.MoveGameObjectToScene(go, escena); go.AddComponent<HopeHandAnticipationAudio>();
    }
    static float Elegir(Vector2 rango) { return Random.Range(Mathf.Max(.1f, Mathf.Min(rango.x,rango.y)), Mathf.Max(.1f, Mathf.Max(rango.x,rango.y))); }
    void Start()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeHandAnticipationSettings>(gameObject.scene.name == "Nivel_3" ? "HopeLevel3HandAnticipationSettings" : "HopeHandAnticipationSettings");
        if (ajustes == null || ajustes.sonido == null) { enabled = false; return; }
        fuente = gameObject.AddComponent<AudioSource>(); fuente.playOnAwake = false; fuente.loop = true; fuente.spatialBlend = 0;
        fuente.clip = ajustes.sonido; fuente.volume = 0;
        HopeAudioRouting.Asignar(fuente, HopeAudioBus.Ambientes);
        fuente.Play(); duracion = Elegir(ajustes.silencio);
    }
    void Update()
    {
        if (fuente == null) return;
        bool pausa = Time.timeScale <= 0;
        if (pausa != pausado) { if (pausa) fuente.Pause(); else fuente.UnPause(); pausado = pausa; }
        if (pausa || AudioListener.pause) return;
        tiempo += Time.deltaTime;
        if (tiempo >= duracion)
        {
            tiempo = 0; sonando = !sonando;
            duracion = Elegir(sonando ? ajustes.duracion : ajustes.silencio);
        }
        float ganancia = 0;
        if (sonando)
        {
            float entrada = Mathf.Min(ajustes.entrada, duracion * .45f);
            float salida = Mathf.Min(ajustes.salida, duracion * .45f);
            ganancia = Mathf.SmoothStep(0,1,Mathf.Clamp01(tiempo / Mathf.Max(.01f,entrada)))
                * Mathf.SmoothStep(0,1,Mathf.Clamp01((duracion-tiempo) / Mathf.Max(.01f,salida)));
        }
        fuente.volume = ajustes.volumen * ganancia * HopeDeathNarrativeAudio.GananciaFondo(gameObject.scene, false);
    }
    void OnDisable() { if (fuente != null) fuente.Stop(); }
}
