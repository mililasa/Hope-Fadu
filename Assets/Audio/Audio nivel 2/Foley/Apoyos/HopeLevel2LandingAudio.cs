using UnityEngine;
using UnityEngine.SceneManagement;
[DisallowMultipleComponent]
public sealed class HopeLevel2LandingAudio : MonoBehaviour
{
    public HopeLevel2LandingSettings ajustes;
    PlayerController2D jugador;
    AudioSource fuente;
    float aire, ignorarHasta;
    Vector3 anterior;
    int ultimo = -1;
    bool pausado;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preparar() { SceneManager.sceneLoaded -= AlCargar; SceneManager.sceneLoaded += AlCargar; }
    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "Nivel_2") return;
        foreach (var raiz in escena.GetRootGameObjects())
            foreach (var pp in raiz.GetComponentsInChildren<PlayerController2D>(true))
                if (pp.GetComponent<HopeLevel2LandingAudio>() == null) pp.gameObject.AddComponent<HopeLevel2LandingAudio>();
    }
    void Start()
    {
        jugador = GetComponent<PlayerController2D>();
        if (ajustes == null) ajustes = Resources.Load<HopeLevel2LandingSettings>("HopeLevel2LandingSettings");
        if (jugador == null || ajustes == null || ajustes.apoyos == null || ajustes.apoyos.Length == 0)
        { Debug.LogError("Hope: faltan apoyos metalicos del nivel 2.", this); enabled = false; return; }
        var go = new GameObject("F - Apoyos metal nivel 2"); go.transform.SetParent(transform, false);
        fuente = go.AddComponent<AudioSource>(); fuente.playOnAwake = false; fuente.loop = false;
        fuente.spatialBlend = 0; fuente.dopplerLevel = 0;
        if (ajustes.salida != null) fuente.outputAudioMixerGroup = ajustes.salida;
        else HopeAudioRouting.Asignar(fuente, HopeAudioBus.Foley);
        Reiniciar();
    }
    void LateUpdate()
    {
        if (fuente == null) return;
        bool pausa = Time.timeScale == 0 || AudioListener.pause;
        if (pausa != pausado) { if (pausa) fuente.Pause(); else fuente.UnPause(); pausado = pausa; }
        float desplazamiento = Vector2.Distance(anterior, transform.position); anterior = transform.position;
        if (pausa) return;
        if (desplazamiento > Mathf.Max(3, Mathf.Abs(jugador.moveSpeed) * Time.deltaTime * 4)) { Reiniciar(); return; }
        fuente.volume = ajustes.volumen;
        if (!ajustes.habilitado || !jugador.enabled || jugador.BloqueaFoleyDeSalto || Time.time < ignorarHasta)
        { aire = 0; return; }
        if (!jugador.EnSueloParaFoley) { aire += Time.deltaTime; return; }
        if (aire >= ajustes.minimoTiempoEnAire)
        {
            int i = Random.Range(0, ajustes.apoyos.Length);
            if (ajustes.apoyos.Length > 1 && i == ultimo) i = (i + Random.Range(1, ajustes.apoyos.Length)) % ajustes.apoyos.Length;
            ultimo = i;
            if (ajustes.apoyos[i] != null) HopeSurfaceMix.Reproducir(fuente, ajustes.apoyos[i], 1f);
        }
        aire = 0;
    }
    public void Reiniciar()
    {
        aire = 0; anterior = transform.position;
        ignorarHasta = Time.time + (ajustes != null ? ajustes.ignorarAlReiniciar : .2f);
        if (fuente != null) fuente.Stop();
    }
    void OnDisable() { if (fuente != null) fuente.Stop(); aire = 0; }
}
