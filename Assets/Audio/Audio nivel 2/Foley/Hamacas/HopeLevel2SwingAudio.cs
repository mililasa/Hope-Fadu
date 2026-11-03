using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
[DisallowMultipleComponent]
public sealed class HopeLevel2SwingAudio : MonoBehaviour
{
    public HopeLevel2SwingSettings ajustes;
    readonly List<Collider2D> superficies = new List<Collider2D>();
    PlayerController2D jugador;
    AudioSource fuente;
    bool iniciado, pausado;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preparar()
    {
        SceneManager.sceneLoaded -= AlCargar; SceneManager.sceneLoaded += AlCargar;
    }
    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "Nivel_2") return;
        foreach (var raiz in escena.GetRootGameObjects())
            foreach (var pp in raiz.GetComponentsInChildren<PlayerController2D>(true))
                if (pp.GetComponent<HopeLevel2SwingAudio>() == null) pp.gameObject.AddComponent<HopeLevel2SwingAudio>();
    }
    void Start()
    {
        jugador = GetComponent<PlayerController2D>();
        if (ajustes == null) ajustes = Resources.Load<HopeLevel2SwingSettings>("HopeLevel2SwingSettings");
        if (ajustes == null || ajustes.hamacas == null || ajustes.salida == null || jugador == null || ajustes.superficies == null || ajustes.superficies.Length == 0)
        { Debug.LogError("Hope: faltan ajustes de hamacas de nivel 2.", this); enabled = false; return; }
        foreach (string ruta in ajustes.superficies)
        {
            Transform encontrado = null;
            foreach (var raiz in gameObject.scene.GetRootGameObjects())
            {
                if (!ruta.StartsWith(raiz.name + "/", System.StringComparison.Ordinal)) continue;
                encontrado = raiz.transform.Find(ruta.Substring(raiz.name.Length + 1));
                if (encontrado != null) break;
            }
            var col = encontrado == null ? null : encontrado.GetComponent<Collider2D>();
            if (col == null) { Debug.LogError("Hope: falta superficie de hamaca: " + ruta, this); enabled = false; return; }
            superficies.Add(col);
        }
        var objeto = new GameObject("F - Hamacas nivel 2"); objeto.transform.SetParent(transform, false);
        fuente = objeto.AddComponent<AudioSource>(); fuente.playOnAwake = false; fuente.clip = ajustes.hamacas;
        fuente.loop = true; fuente.spatialBlend = 0; fuente.dopplerLevel = 0; fuente.volume = 0;
        fuente.outputAudioMixerGroup = ajustes.salida;
    }
    public Bounds ZonaConjunto()
    {
        Bounds zona = superficies[0].bounds;
        for (int i = 1; i < superficies.Count; i++) if (superficies[i] != null) zona.Encapsulate(superficies[i].bounds);
        // El volumen incluye los huecos y el arco de los saltos, sin sensores de pisada.
        zona.Expand(new Vector3(0, ajustes.alturaExtraZona * 2, 0));
        return zona;
    }
    void LateUpdate()
    {
        if (fuente == null) return;
        bool pausa = Time.timeScale == 0;
        if (pausa != pausado) { if (pausa) fuente.Pause(); else fuente.UnPause(); pausado = pausa; }
        if (pausa || AudioListener.pause) return;
        Bounds zona = ZonaConjunto(); Vector3 p = transform.position; p.z = zona.center.z;
        float distancia = Vector3.Distance(p, zona.ClosestPoint(p));
        float margen = Mathf.Max(.25f, Mathf.Abs(jugador.moveSpeed) * ajustes.segundosAnticipacion);
        float destino = 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(distancia / margen));
        // Arranca una sola vez al acercarse; no reinicia al pisar, saltar o retroceder.
        if (!iniciado && destino > .0001f) { fuente.Play(); iniciado = true; }
        float duracion = destino > fuente.volume ? ajustes.fadeEntrada : ajustes.fadeSalida;
        fuente.volume = Mathf.MoveTowards(fuente.volume, destino, Time.deltaTime / Mathf.Max(.001f, duracion));
    }
    void OnDisable() { if (fuente != null) fuente.Stop(); }
}
