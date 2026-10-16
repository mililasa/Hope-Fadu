using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class HopeCaballitoAudio : MonoBehaviour
{
    public HopeCaballitoSettings ajustes;
    Animator animador;
    AudioSource resorte, soporte;
    Transform jugador;
    Camera camara;
    int estadoAnterior, variante;
    float tiempoAnterior;
    bool sincronizado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void LimpiarRegistro() { SceneManager.sceneLoaded -= AlCargarEscena; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Inicializar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        Conectar();
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo) { Conectar(); }

    static void Conectar()
    {
        foreach (Animator a in Object.FindObjectsOfType<Animator>())
        {
            if (a.gameObject.name != "caballito_0") continue;
            if (a.GetComponent<HopeCaballitoAudio>() == null)
                a.gameObject.AddComponent<HopeCaballitoAudio>();
        }
    }

    void Awake()
    {
        animador = GetComponent<Animator>();
        if (ajustes == null) ajustes = Resources.Load<HopeCaballitoSettings>("HopeCaballitoSettings");
        if (ajustes == null) { enabled = false; return; }
        resorte = Crear("Caballito - rechinar del resorte");
        soporte = Crear("Caballito - apoyo del soporte");
        var p = Object.FindObjectOfType<PlayerController2D>();
        if (p != null) jugador = p.transform;
        camara = Camera.main;
    }

    AudioSource Crear(string nombre)
    {
        var objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        var fuente = objeto.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
        fuente.volume = 0f;
        HopeAudioRouting.Asignar(fuente, HopeAudioBus.Foley);
        return fuente;
    }

    void LateUpdate()
    {
        if (ajustes == null || resorte == null) return;
        if (camara == null) camara = Camera.main;
        Transform oyente = jugador != null ? jugador : (camara != null ? camara.transform : null);
        float ganancia = 0f, pan = 0f;
        if (oyente != null)
        {
            Vector2 delta = transform.position - oyente.position;
            float distancia = delta.magnitude;
            float maximo = Mathf.Max(ajustes.distanciaPlena + 0.1f, ajustes.distanciaMaxima);
            ganancia = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ajustes.distanciaPlena, maximo, distancia));
            pan = Mathf.Clamp(delta.x / maximo, -1f, 1f) * ajustes.panoramaMaximo;
        }
        resorte.volume = ajustes.volumen * ganancia;
        soporte.volume = ajustes.volumen * ajustes.volumenApoyo * ganancia;
        resorte.panStereo = soporte.panStereo = pan;
        if (Time.deltaTime <= 0f || !animador.isActiveAndEnabled || animador.runtimeAnimatorController == null) return;

        AnimatorStateInfo estado = animador.GetCurrentAnimatorStateInfo(0);
        if (estado.length <= 0f) return;
        float tiempo = estado.normalizedTime * estado.length;
        if (!sincronizado || estado.fullPathHash != estadoAnterior || tiempo < tiempoAnterior)
        {
            tiempoAnterior = tiempo - 0.001f;
            estadoAnterior = estado.fullPathHash;
            sincronizado = true;
        }
        if (ganancia > 0.001f && tiempo - tiempoAnterior < estado.length)
        {
            if (estado.IsName("caballito_normal") && ajustes.momentosNormal != null)
            {
                foreach (float momento in ajustes.momentosNormal)
                    if (Cruzo(tiempoAnterior, tiempo, momento, estado.length)) Rechinar();
            }
            else if (estado.IsName("caballito_embrujado"))
            {
                if (Cruzo(tiempoAnterior, tiempo, 0.01f, estado.length) ||
                    Cruzo(tiempoAnterior, tiempo, estado.length * 0.5f, estado.length)) Rechinar();
            }
        }
        tiempoAnterior = tiempo;
    }

    static bool Cruzo(float anterior, float actual, float momento, float ciclo)
    {
        float siguiente = (Mathf.Floor((anterior - momento) / ciclo) + 1f) * ciclo + momento;
        return siguiente > anterior && siguiente <= actual;
    }

    void Rechinar()
    {
        if (ajustes.rechinidos == null || ajustes.rechinidos.Length == 0) return;
        AudioClip clip = ajustes.rechinidos[variante++ % ajustes.rechinidos.Length];
        if (clip != null) resorte.PlayOneShot(clip);
        if (ajustes.apoyo != null) soporte.PlayOneShot(ajustes.apoyo);
    }

    void OnDisable()
    {
        sincronizado = false;
        if (resorte != null) resorte.Stop();
        if (soporte != null) soporte.Stop();
    }
}
