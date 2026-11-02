using UnityEngine;
using UnityEngine.SceneManagement;
[DisallowMultipleComponent]
public sealed class HopeRockfallAudio : MonoBehaviour
{
    public HopeRockfallSettings ajustes;
    PlayerController2D jugador;
    AudioSource fuente;
    float inicioX, finX, avance, espera = -1, disponible;
    float[] momentos;
    int siguiente;
    bool pausado, vertical;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preparar()
    {
        SceneManager.sceneLoaded -= AlCargar; SceneManager.sceneLoaded += AlCargar;
    }
    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "SampleScene" && escena.name != "Nivel_3") return;
        foreach (var raiz in escena.GetRootGameObjects())
            if (raiz.GetComponentInChildren<HopeRockfallAudio>(true) != null) return;
        var go = new GameObject("Audio " + escena.name + " - Estampidas de piedra");
        SceneManager.MoveGameObjectToScene(go, escena); go.AddComponent<HopeRockfallAudio>();
    }
    void Start()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeRockfallSettings>(gameObject.scene.name == "Nivel_3" ? "HopeLevel3RockfallSettings" : "HopeRockfallSettings");
        foreach (var j in FindObjectsOfType<PlayerController2D>())
            if (j.gameObject.scene == gameObject.scene) { jugador = j; break; }
        var musica = Resources.Load<HopeMusicSettings>("HopeMusicSettings");
        var frente = musica != null ? GameObject.Find(musica.nombreSalida) : null;
        vertical = gameObject.scene.name == "Nivel_3";
        FaroFez faro = vertical ? FindObjectOfType<FaroFez>() : null;
        if (jugador == null || ajustes == null || ajustes.sonido == null || (vertical ? faro == null : frente == null))
        { Debug.LogWarning("Hope: falta configurar las estampidas de piedra.", this); enabled = false; return; }
        inicioX = vertical ? jugador.transform.position.y : jugador.transform.position.x;
        finX = vertical ? (faro.pivote != null ? faro.pivote.position.y : faro.transform.position.y) + faro.alturaLinterna : frente.transform.TransformPoint(musica.puntoPuertaLocal).x;
        fuente = gameObject.AddComponent<AudioSource>(); fuente.playOnAwake = false; fuente.loop = false; fuente.spatialBlend = 0;
        fuente.clip = ajustes.sonido; fuente.volume = 0;
        HopeAudioRouting.Asignar(fuente, HopeAudioBus.Ambientes);
        int cantidad = Random.Range(2, 4); momentos = new float[cantidad];
        float tramo = .7f / cantidad;
        for (int i = 0; i < cantidad; i++) momentos[i] = .12f + i * tramo + Random.Range(.15f, .85f) * tramo;
    }
    void Update()
    {
        if (fuente == null) return;
        bool pausa = Time.timeScale <= 0;
        if (pausa != pausado) { if (pausa) fuente.Pause(); else fuente.UnPause(); pausado = pausa; }
        fuente.volume = ajustes.volumen * HopeDeathNarrativeAudio.GananciaFondo(gameObject.scene, false);
        if (pausa || AudioListener.pause || siguiente >= momentos.Length) return;
        float posicion = Mathf.InverseLerp(inicioX, finX, vertical ? jugador.transform.position.y : jugador.transform.position.x);
        avance = Mathf.Max(avance, posicion);
        if (avance < momentos[siguiente] || Time.time < disponible) return;
        if (espera < 0) espera = Random.Range(.5f, 2.5f);
        espera -= Time.deltaTime;
        if (espera > 0) return;
        fuente.pitch = Random.Range(.96f, 1.03f);
        fuente.panStereo = Random.Range(-ajustes.panoramaMaximo, ajustes.panoramaMaximo);
        fuente.Play(); siguiente++; espera = -1;
        disponible = Time.time + ajustes.sonido.length / fuente.pitch + ajustes.separacionMinima;
    }
    void OnDisable() { if (fuente != null) fuente.Stop(); }
}
