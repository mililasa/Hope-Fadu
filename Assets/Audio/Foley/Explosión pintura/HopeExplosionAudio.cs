using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class HopeExplosionAudio : MonoBehaviour
{
    public HopeExplosionSettings ajustes;
    Animator animador;
    SpriteRenderer dibujo;
    AudioSource detonacion, energia, pintura;
    Transform jugador;
    Camera camara;
    int estadoAnterior, cicloAnterior;
    bool sincronizado;
    bool yaSono;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar() { SceneManager.sceneLoaded -= AlCargar; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Inicializar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
        Conectar();
    }

    static void AlCargar(Scene escena, LoadSceneMode modo) { Conectar(); }

    static void Conectar()
    {
        foreach (Animator a in Object.FindObjectsOfType<Animator>())
        {
            string n = a.gameObject.name;
            if (n != "explosion-Sheet_0" &&
                n.IndexOf("explosion", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (a.GetComponent<HopeExplosionAudio>() == null)
                a.gameObject.AddComponent<HopeExplosionAudio>();
        }
    }

    void Awake()
    {
        animador = GetComponent<Animator>();
        dibujo = GetComponent<SpriteRenderer>();
        if (ajustes == null) ajustes = Resources.Load<HopeExplosionSettings>("HopeExplosionSettings");
        if (ajustes == null) { enabled = false; return; }
        detonacion = Crear("Explosion - detonacion belica");
        energia = Crear("Explosion - energia futurista");
        pintura = Crear("Explosion - salpicadura de pintura");
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
        if (ajustes == null || detonacion == null) return;
        if (camara == null) camara = Camera.main;
        Transform oyente = jugador != null ? jugador : (camara != null ? camara.transform : null);
        float ganancia = 0f, pan = 0f;
        if (oyente != null)
        {
            Vector2 delta = transform.position - oyente.position;
            float maximo = Mathf.Max(ajustes.distanciaPlena + 0.1f, ajustes.distanciaMaxima);
            ganancia = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ajustes.distanciaPlena, maximo, delta.magnitude));
            pan = Mathf.Clamp(delta.x / maximo, -1f, 1f) * ajustes.panoramaMaximo;
        }
        float general = ajustes.volumenGeneral * ganancia;
        detonacion.volume = general * ajustes.volumenDetonacion;
        energia.volume = general * ajustes.volumenEnergia;
        pintura.volume = general * ajustes.volumenPintura;
        detonacion.panStereo = energia.panStereo = pintura.panStereo = pan;
        if (Time.deltaTime <= 0f || !animador.isActiveAndEnabled || animador.runtimeAnimatorController == null) return;
        AnimatorStateInfo estado = animador.GetCurrentAnimatorStateInfo(0);
        int ciclo = Mathf.FloorToInt(estado.normalizedTime);
        bool cambio = !sincronizado || estado.fullPathHash != estadoAnterior || ciclo != cicloAnterior;
        if (cambio)
        {
            bool inicio = sincronizado || Mathf.Repeat(estado.normalizedTime, 1f) * estado.length < 0.1f;
            sincronizado = true;
            estadoAnterior = estado.fullPathHash;
            cicloAnterior = ciclo;
            // Do not invent an impact when the player walks into range mid-cycle.
            if (!yaSono && inicio && estado.IsName("explosion") && ganancia > 0.001f && dibujo != null && dibujo.enabled && dibujo.color.a > 0f)
                Disparar();
        }
    }

    void Disparar()
    {
        if (yaSono) return;
        yaSono = true;
        // Files retain the prototype's internal offsets. Start their timelines
        // together on the audio clock to keep all three layers synchronized.
        double inicio = AudioSettings.dspTime + 0.01;
        Programar(detonacion, ajustes.detonacion, inicio);
        Programar(energia, ajustes.energia, inicio);
        Programar(pintura, ajustes.pintura, inicio);
    }

    static void Programar(AudioSource fuente, AudioClip clip, double inicio)
    {
        fuente.Stop();
        fuente.clip = clip;
        if (clip != null) fuente.PlayScheduled(inicio);
    }

    void OnDisable()
    {
        sincronizado = false;
        if (detonacion != null) detonacion.Stop();
        if (energia != null) energia.Stop();
        if (pintura != null) pintura.Stop();
    }
}
