using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent, DefaultExecutionOrder(100)]
public sealed class HopeLevel2PlatformAudio : MonoBehaviour
{
    public HopeLevel2PlatformSettings ajustes;
    [SerializeField] float velocidadActual, intensidadMovimiento, cercania;
    [SerializeField] int inversiones, recorridosSonoros;
    public float VelocidadActual => velocidadActual;
    public float IntensidadMovimiento => intensidadMovimiento;
    public float Cercania => cercania;
    public int Inversiones => inversiones;
    public int RecorridosSonoros => recorridosSonoros;
    public bool EsPropietaria => propietaria == this;
    public double ErrorRitmico { get; private set; }
    static readonly List<HopeLevel2PlatformAudio> todas = new List<HopeLevel2PlatformAudio>();
    static HopeLevel2PlatformAudio propietaria, deseada;
    static int frameSeleccion = -1;
    Rigidbody2D cuerpo;
    Transform hope;
    HopeLevel2Music musica;
    AudioSource metal, servo, electricidad, giro;
    AudioSource[] fuentes;
    HopeLevel2PlatformSettings.Sonidos clips;
    PlataformaMovil vertical;
    PlataformaMovilHorizontal horizontal;
    Vector2 anterior, ultimaDireccion;
    float ultimoGiro = -10;
    bool pausado, pendiente = true, giroPendiente, reiniciando;
    double inicioToma = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preparar()
    {
        todas.Clear(); propietaria = deseada = null; frameSeleccion = -1;
        SceneManager.sceneLoaded -= AlCargar; SceneManager.sceneLoaded += AlCargar;
    }
    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "Nivel_2") return;
        foreach (var raiz in escena.GetRootGameObjects())
        {
            foreach (var p in raiz.GetComponentsInChildren<PlataformaMovil>(true)) Instalar(p.gameObject);
            foreach (var p in raiz.GetComponentsInChildren<PlataformaMovilHorizontal>(true)) Instalar(p.gameObject);
        }
    }
    static void Instalar(GameObject go)
    {
        if (go.GetComponent<HopeLevel2PlatformAudio>() == null) go.AddComponent<HopeLevel2PlatformAudio>();
    }
    void Start()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeLevel2PlatformSettings>("HopeLevel2PlatformSettings");
        cuerpo = GetComponent<Rigidbody2D>(); vertical = GetComponent<PlataformaMovil>(); horizontal = GetComponent<PlataformaMovilHorizontal>();
        foreach (var raiz in gameObject.scene.GetRootGameObjects())
        {
            var jugador = raiz.GetComponentInChildren<PlayerController2D>(true);
            if (jugador != null) { hope = jugador.transform; break; }
        }
        clips = ajustes == null ? null : (vertical != null ? ajustes.vertical : ajustes.horizontal);
        if (cuerpo == null || hope == null || clips == null || clips.metal == null || clips.servo == null || clips.electricidad == null || clips.inversion == null || ajustes.salidaMetal == null || ajustes.salidaServo == null || ajustes.salidaElectricidad == null)
        { Debug.LogError("Hope: faltan ajustes de plataformas de Nivel 2.", this); enabled = false; return; }
        metal = Crear("M - Plataforma - Metal", clips.metal, ajustes.salidaMetal);
        servo = Crear("M - Plataforma - Motor", clips.servo, ajustes.salidaServo);
        electricidad = Crear("M - Plataforma - Electricidad", clips.electricidad, ajustes.salidaElectricidad);
        giro = Crear("M - Plataforma - Inversion", clips.inversion, ajustes.salidaElectricidad);
        fuentes = new[] { metal, servo, electricidad, giro };
        anterior = cuerpo.position; todas.Add(this);
        musica = FindObjectOfType<HopeLevel2Music>();
    }
    AudioSource Crear(string nombre, AudioClip clip, AudioMixerGroup salida)
    {
        var go = new GameObject(nombre); go.transform.SetParent(transform, false);
        var a = go.AddComponent<AudioSource>(); a.playOnAwake = false; a.clip = clip;
        a.loop = false; a.volume = 0; a.spatialBlend = 0; a.dopplerLevel = 0; a.outputAudioMixerGroup = salida;
        return a;
    }
    float Proximidad()
    {
        if (hope == null) return 0;
        float distancia = Vector2.Distance(hope.position, transform.position);
        return 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(ajustes.distanciaPlena, Mathf.Max(ajustes.distanciaPlena + .01f, ajustes.distanciaMaxima), distancia));
    }
    float Puntaje()
    {
        float d = Vector2.Distance(hope.position, transform.position);
        // Al llegar al tramo horizontal se prioriza esa maquina sobre las verticales.
        if (horizontal != null && d < 6) d -= 1.5f;
        return d;
    }
    static void Elegir()
    {
        if (frameSeleccion == Time.frameCount) return;
        frameSeleccion = Time.frameCount;
        HopeLevel2PlatformAudio mejor = null; float puntaje = float.PositiveInfinity;
        foreach (var p in todas)
        {
            if (p == null || !p.isActiveAndEnabled || p.fuentes == null || p.Proximidad() <= .005f) continue;
            float valor = p.Puntaje();
            if (valor < puntaje) { mejor = p; puntaje = valor; }
        }
        // Histéresis: evita alternar entre dos maquinas por un pequeño movimiento.
        if (deseada != null && deseada.isActiveAndEnabled && deseada.Proximidad() > .005f && mejor != deseada &&
            !(mejor != null && mejor.horizontal != null && deseada.vertical != null && mejor.Puntaje() < 4.5f) &&
            deseada.Puntaje() <= puntaje + .9f) mejor = deseada;
        deseada = mejor;
        if (propietaria != null && propietaria != deseada)
        {
            // Primero termina el fade de la saliente. La entrante espera en silencio.
            if (!propietaria.EnSilencio()) return;
            propietaria.Detener(); propietaria = null;
        }
        if (propietaria == null && deseada != null)
        {
            propietaria = deseada; propietaria.pendiente = true;
        }
    }
    bool EnSilencio()
    {
        if (fuentes == null) return true;
        foreach (var a in fuentes) if (a.volume > .0001f) return false;
        return true;
    }
    void Detener()
    {
        if (fuentes != null) foreach (var a in fuentes) { a.Stop(); a.volume = 0; }
        inicioToma = -1; reiniciando = false;
    }
    void FixedUpdate()
    {
        if (fuentes == null || Time.timeScale == 0 || AudioListener.pause) return;
        Vector2 actual = cuerpo.position, delta = actual - anterior; anterior = actual;
        velocidadActual = delta.magnitude / Mathf.Max(.001f, Time.fixedDeltaTime);
        float referencia = vertical != null ? vertical.velocidad : horizontal.moveSpeed;
        bool habilitada = vertical != null ? vertical.isActiveAndEnabled : horizontal.isActiveAndEnabled;
        if (!habilitada || referencia < .01f || delta.magnitude > Mathf.Max(1, referencia * Time.fixedDeltaTime * 4))
        { velocidadActual = 0; ultimaDireccion = Vector2.zero; pendiente = true; return; }
        if (velocidadActual < .025f) return;
        Vector2 direccion = delta.normalized;
        if (ultimaDireccion.sqrMagnitude > .5f && Vector2.Dot(direccion, ultimaDireccion) < -.25f && Time.time - ultimoGiro > .25f)
        {
            ultimoGiro = Time.time; inversiones++; pendiente = true;
            // Se encola: la descarga también espera el próximo pulso musical.
            giroPendiente = EsPropietaria && propietaria == deseada;
        }
        ultimaDireccion = direccion;
    }
    float Paneo() => Mathf.Clamp((transform.position.x - hope.position.x) / Mathf.Max(.1f, ajustes.distanciaMaxima), -1, 1) * ajustes.paneoMaximo;
    void Programar()
    {
        if (musica == null) musica = FindObjectOfType<HopeLevel2Music>();
        double cuando = AudioSettings.dspTime + .05;
        if (musica != null) musica.ProximaCorchea(.05, out cuando);
        Detener(); inicioToma = cuando;
        metal.PlayScheduled(cuando); servo.PlayScheduled(cuando); electricidad.PlayScheduled(cuando);
        if (giroPendiente) giro.PlayScheduled(cuando);
        ErrorRitmico = musica != null ? musica.ErrorDeRejilla(cuando) : 0;
        pendiente = false; giroPendiente = false; recorridosSonoros++;
    }
    void LateUpdate()
    {
        if (fuentes == null) return;
        bool pausa = Time.timeScale == 0 || AudioListener.pause;
        if (pausa != pausado)
        {
            Detener(); pendiente = true; giroPendiente = false;
            pausado = pausa; anterior = cuerpo.position;
        }
        if (pausa) return;
        Elegir();
        bool autorizada = EsPropietaria && propietaria == deseada;
        float referencia = vertical != null ? vertical.velocidad : horizontal.moveSpeed;
        intensidadMovimiento = Mathf.Clamp01(velocidadActual / Mathf.Max(.01f, referencia));
        cercania = Proximidad();
        if (!autorizada && inicioToma > AudioSettings.dspTime) Detener();
        if (autorizada && pendiente && cercania > .005f && velocidadActual > .025f)
        {
            if (inicioToma < 0 || AudioSettings.dspTime >= inicioToma)
            {
                reiniciando = !EnSilencio();
                if (!reiniciando) Programar();
            }
        }
        float movimiento = autorizada && !reiniciando ? Mathf.SmoothStep(0, 1, intensidadMovimiento) : 0;
        // Relieve suave de corcheas; se conserva el cuerpo del mecanismo.
        float ritmo = musica != null ? musica.RelieveRitmico() : 1;
        Ajustar(metal, cercania * movimiento * ritmo);
        Ajustar(servo, cercania * Mathf.Pow(movimiento, .7f) * ritmo);
        Ajustar(electricidad, cercania * movimiento * .8f * ritmo);
        Ajustar(giro, autorizada ? cercania * .8f : 0);
        if (!autorizada && EnSilencio()) Detener();
    }
    void Ajustar(AudioSource a, float destino)
    {
        if (inicioToma < 0 || AudioSettings.dspTime < inicioToma || !a.isPlaying) destino = 0;
        if (a.isPlaying && AudioSettings.dspTime >= inicioToma)
            destino *= Mathf.Min(Mathf.Clamp01(a.time / .015f), Mathf.Clamp01((a.clip.length - a.time) / .08f));
        float duracion = reiniciando ? .06f : (destino > a.volume ? ajustes.fadeEntrada : ajustes.fadeSalida);
        a.volume = Mathf.MoveTowards(a.volume, destino, Time.deltaTime / Mathf.Max(.01f, duracion)); a.panStereo = Paneo();
    }
    void OnDisable() { Detener(); }
    void OnDestroy()
    {
        todas.Remove(this); if (propietaria == this) propietaria = null; if (deseada == this) deseada = null;
    }
}
