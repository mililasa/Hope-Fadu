using UnityEngine;
using UnityEngine.SceneManagement;

// One persistent source carries the actual playback position across level loads.
public sealed class HopeMusicTransition : MonoBehaviour
{
    enum Estado { Ninguno, Salida, Espera, Entrada }
    static HopeMusicTransition instancia;
    Estado estado;
    AudioSource fuente;
    HopeMusicSettings musica;
    HopeMusicTransitionSettings ajustes;
    string origen, destino;
    float nivel, tiempoEntrada;
    bool salidaActiva, pausado;
    public static float GananciaFondo(Scene escena)
    {
        var t = instancia;
        if (t == null) return 1;
        if ((t.estado == Estado.Entrada && escena.name == t.destino) ||
            (t.estado == Estado.Salida && escena.name == t.origen)) return 1 - t.nivel;
        if (t.estado == Estado.Espera && escena.name == t.destino) return 0;
        return 1;
    }
    public static float AnticipacionSalida
    {
        get
        {
            var s = Resources.Load<HopeMusicTransitionSettings>("HopeMusicTransitionSettings");
            return s != null ? s.segundosAnticipacionSalida : 2;
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preparar()
    {
        instancia = null;
        SceneManager.sceneLoaded -= AlCargar; SceneManager.sceneLoaded += AlCargar;
    }
    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (instancia == null || modo == LoadSceneMode.Additive) return;
        var t = instancia;
        if (t.estado == Estado.Espera && escena.name == t.destino)
        {
            t.estado = Estado.Entrada; t.tiempoEntrada = 0;
        }
        else
        {
            // Restart, menu or a load that was not an actual completion.
            Destroy(t.gameObject); instancia = null;
        }
    }
    static HopeMusicTransition Obtener()
    {
        if (instancia != null) return instancia;
        var go = new GameObject("Audio - Transicion entre niveles");
        DontDestroyOnLoad(go); instancia = go.AddComponent<HopeMusicTransition>();
        instancia.musica = Resources.Load<HopeMusicSettings>("HopeMusicSettings");
        instancia.ajustes = Resources.Load<HopeMusicTransitionSettings>("HopeMusicTransitionSettings");
        return instancia;
    }
    bool AsegurarFuente()
    {
        if (fuente != null) return true;
        if (musica == null || musica.pasoDeNivel == null)
        { Debug.LogError("Hope: falta la musica de salida para la transicion.", this); return false; }
        var go = new GameObject("Musica de salida - continua entre niveles"); go.transform.SetParent(transform, false);
        fuente = go.AddComponent<AudioSource>(); fuente.playOnAwake = false;
        fuente.loop = true; fuente.clip = musica.pasoDeNivel; fuente.spatialBlend = 0; fuente.dopplerLevel = 0;
        HopeAudioRouting.Asignar(fuente, HopeAudioBus.Musica); fuente.volume = 0;
        return true;
    }
    void ReproducirSiHaceFalta()
    {
        if (fuente.isPlaying) return;
        fuente.time = Mathf.Clamp(musica.inicioPaso, 0, Mathf.Max(0, fuente.clip.length - .05f));
        fuente.Play();
    }
    public static void SolicitarSalida(Scene escena, bool cerca)
    {
        if (escena.name != "Nivel_2") return;
        if (!cerca && (instancia == null || instancia.estado != Estado.Salida)) return;
        var t = Obtener();
        if (t.estado == Estado.Espera) return;
        if (cerca)
        {
            if (!t.AsegurarFuente()) return;
            t.ReproducirSiHaceFalta(); t.estado = Estado.Salida; t.origen = escena.name;
        }
        if (t.estado == Estado.Salida && t.origen == escena.name) t.salidaActiva = cerca;
    }
    public static void PrepararCambio(Scene actual, string siguiente)
    {
        if (!((actual.name == "SampleScene" && siguiente == "Nivel_2") || (actual.name == "Nivel_2" && siguiente == "Nivel_3"))) return;
        var t = Obtener();
        if (actual.name == "SampleScene")
        {
            foreach (var raiz in actual.GetRootGameObjects())
            {
                var m = raiz.GetComponentInChildren<HopeLevelMusic>();
                if (m == null) continue;
                AudioSource heredada = m.ObtenerSalidaParaTransicion();
                if (heredada == null) continue;
                if (t.fuente != null && t.fuente != heredada) Destroy(t.fuente.gameObject);
                heredada.transform.SetParent(t.transform, false); t.fuente = heredada;
                break;
            }
        }
        if (!t.AsegurarFuente()) return;
        t.ReproducirSiHaceFalta();
        t.origen = actual.name; t.destino = siguiente; t.nivel = 1;
        t.fuente.volume = t.musica.volumenPaso; t.estado = Estado.Espera;
    }
    void Update()
    {
        if (fuente == null) return;
        bool pausa = Time.timeScale == 0;
        if (pausa != pausado)
        { if (pausa) fuente.Pause(); else fuente.UnPause(); pausado = pausa; }
        if (pausa || AudioListener.pause) return;
        float fundido = ajustes != null ? ajustes.segundosFundido : 2;
        if (estado == Estado.Salida)
        {
            nivel = Mathf.MoveTowards(nivel, salidaActiva ? 1 : 0, Time.deltaTime / Mathf.Max(.05f, fundido));
            if (!salidaActiva && nivel <= 0) { fuente.Stop(); estado = Estado.Ninguno; }
        }
        else if (estado == Estado.Entrada)
        {
            tiempoEntrada += Time.deltaTime;
            float espera = ajustes != null ? ajustes.segundosAlEntrar : 3;
            if (tiempoEntrada >= espera)
            {
                nivel = Mathf.MoveTowards(nivel, 0, Time.deltaTime / Mathf.Max(.05f, fundido));
                if (nivel <= 0) { fuente.Stop(); estado = Estado.Ninguno; }
            }
        }
        fuente.volume = musica.volumenPaso * nivel;
    }
    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }
}
