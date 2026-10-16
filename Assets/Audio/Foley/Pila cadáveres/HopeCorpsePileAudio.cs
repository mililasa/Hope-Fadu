using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class HopeCorpsePileAudio : MonoBehaviour
{
    public HopeCorpsePileSettings ajustes;
    static readonly List<HopeCorpsePileAudio> pilas = new List<HopeCorpsePileAudio>();
    static readonly ContactPoint2D[] puntos = new ContactPoint2D[32];
    SpriteRenderer dibujo;
    Collider2D suelo;
    PlayerController2D jugador;
    Canal[][] contactos;
    Canal[] voces;
    float nivelVoces, siguienteVoz = 0.12f, ultimoContacto = -100f;
    int ultimaVariante = -1, ultimaVoz = -1;
    PlayerController2D ultimoJugador;
    Vector3 posicionAnterior;
    bool tienePosicion;

    sealed class Canal
    {
        public HopeCorpsePileSound ajuste;
        public AudioSource fuente;
        public float intensidad = 1f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        pilas.Clear();
    }

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
        foreach (SpriteRenderer sr in Object.FindObjectsOfType<SpriteRenderer>())
        {
            if (sr.gameObject.name != "pila_cadaver_0") continue;
            var audio = sr.GetComponent<HopeCorpsePileAudio>();
            if (audio == null) audio = sr.gameObject.AddComponent<HopeCorpsePileAudio>();
            audio.Registrar();
        }
    }

    void Registrar() { if (!pilas.Contains(this)) pilas.Add(this); }
    void OnEnable() { Registrar(); }

    void Awake()
    {
        dibujo = GetComponent<SpriteRenderer>();
        if (ajustes == null) ajustes = Resources.Load<HopeCorpsePileSettings>("HopeCorpsePileSettings");
        if (ajustes == null) { enabled = false; return; }
        GameObject piso = GameObject.Find(ajustes.nombreColliderSuelo);
        if (piso != null) suelo = piso.GetComponent<Collider2D>();
        if (suelo == null) Debug.LogWarning("Hope: falta el collider de apoyo de la pila de cadaveres.", this);
        jugador = Object.FindObjectOfType<PlayerController2D>();
        int cantidad = ajustes.capasContacto != null ? ajustes.capasContacto.Length : 0;
        contactos = new Canal[cantidad][];
        for (int i = 0; i < cantidad; i++)
        {
            var capa = ajustes.capasContacto[i];
            contactos[i] = CrearCanales(capa != null ? capa.sonidos : null, capa != null ? capa.nombre : "Apoyo");
        }
        voces = CrearCanales(ajustes.respiraciones, "Respiracion");
        Registrar();
    }

    Canal[] CrearCanales(HopeCorpsePileSound[] sonidos, string grupo)
    {
        var canales = new Canal[sonidos != null ? sonidos.Length : 0];
        for (int i = 0; i < canales.Length; i++)
        {
            var sonido = sonidos[i];
            var objeto = new GameObject("Pila - " + grupo + " - " + (sonido != null ? sonido.nombre : i.ToString()));
            objeto.transform.SetParent(transform, false);
            var fuente = objeto.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.loop = false;
            fuente.spatialBlend = 0f;
            fuente.dopplerLevel = 0f;
            fuente.volume = 0f;
            HopeAudioRouting.Asignar(fuente, HopeAudioBus.Foley);
            canales[i] = new Canal { ajuste = sonido, fuente = fuente };
        }
        return canales;
    }

    void LateUpdate()
    {
        if (ajustes == null || contactos == null) return;
        if (jugador == null) jugador = Object.FindObjectOfType<PlayerController2D>();
        float objetivo = 0f, pan = 0f;
        if (jugador != null)
        {
            Vector3 p = jugador.transform.position;
            if (tienePosicion && Vector2.Distance(p, posicionAnterior) > 3f) Reiniciar();
            posicionAnterior = p;
            tienePosicion = true;
            if (jugador.enabled && !jugador.BloqueaFoleyDeSalto && dibujo != null && dibujo.enabled)
            {
                Bounds b = dibujo.bounds;
                p.z = b.center.z;
                float distancia = Vector2.Distance(p, b.ClosestPoint(p));
                float inicio = Mathf.Max(ajustes.distanciaPlena + 0.1f, ajustes.distanciaInicio);
                objetivo = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ajustes.distanciaPlena, inicio, distancia));
                pan = Mathf.Clamp((b.center.x - p.x) / inicio, -1f, 1f) * ajustes.panoramaMaximo;
            }
        }
        if (Time.deltaTime > 0f)
        {
            float fade = objetivo > nivelVoces ? ajustes.fadeEntrada : ajustes.fadeSalida;
            nivelVoces = Mathf.MoveTowards(nivelVoces, objetivo, Time.deltaTime / Mathf.Max(0.01f, fade));
        }
        AplicarVolumenes(pan);
        if (Time.deltaTime <= 0f) return;
        if (objetivo <= 0.001f && nivelVoces <= 0.001f)
        {
            foreach (Canal c in voces) c.fuente.Stop();
            siguienteVoz = 0.12f;
            return;
        }
        if (objetivo <= 0.001f || nivelVoces <= 0.01f) return;
        siguienteVoz -= Time.deltaTime;
        if (siguienteVoz <= 0f)
        {
            int activas = 0;
            foreach (Canal c in voces) if (c.fuente.isPlaying) activas++;
            if (activas < 2) Respirar();
            else siguienteVoz = 0.25f;
        }
    }

    void AplicarVolumenes(float pan)
    {
        for (int g = 0; g < contactos.Length; g++)
        {
            float capa = ajustes.capasContacto[g] != null ? ajustes.capasContacto[g].volumen : 0f;
            foreach (Canal c in contactos[g])
                c.fuente.volume = ajustes.volumenContactos * capa * (c.ajuste != null ? c.ajuste.volumen : 0f) * c.intensidad;
        }
        for (int i = 0; i < voces.Length; i++)
        {
            Canal c = voces[i];
            c.fuente.volume = ajustes.volumenRespiraciones * nivelVoces * (c.ajuste != null ? c.ajuste.volumen : 0f);
            c.fuente.panStereo = Mathf.Clamp(pan + ((i % 2 == 0) ? -0.12f : 0.12f), -0.7f, 0.7f);
        }
    }

    void Respirar()
    {
        int i = Elegir(voces.Length, ultimaVoz);
        siguienteVoz = Random.Range(Mathf.Max(0.2f, ajustes.intervaloVozMin), Mathf.Max(ajustes.intervaloVozMin, ajustes.intervaloVozMax));
        if (i < 0) return;
        ultimaVoz = i;
        Reproducir(voces[i], AudioSettings.dspTime + 0.01);
    }

    static int Elegir(int cantidad, int anterior)
    {
        if (cantidad <= 0) return -1;
        int i = Random.Range(0, cantidad);
        if (cantidad > 1 && i == anterior) i = (i + Random.Range(1, cantidad)) % cantidad;
        return i;
    }

    public static bool TryPlayContacto(PlayerController2D jugador, bool aterrizaje)
    {
        if (jugador == null || !jugador.enabled || !jugador.EnSueloParaFoley || jugador.BloqueaFoleyDeSalto) return false;
        Rigidbody2D rb = jugador.GetComponent<Rigidbody2D>();
        if (rb == null) return false;
        int cantidad = rb.GetContacts(puntos);
        foreach (HopeCorpsePileAudio pila in pilas)
        {
            if (pila == null || !pila.isActiveAndEnabled || pila.suelo == null || pila.ajustes == null || pila.contactos == null) continue;
            Bounds b = pila.dibujo.bounds;
            float izquierda = Mathf.Lerp(b.min.x, b.max.x, pila.ajustes.bordeIzquierdo);
            float derecha = Mathf.Lerp(b.min.x, b.max.x, pila.ajustes.bordeDerecho);
            for (int i = 0; i < cantidad; i++)
            {
                ContactPoint2D p = puntos[i];
                if (p.collider != pila.suelo && p.otherCollider != pila.suelo) continue;
                if (Mathf.Abs(p.normal.y) < 0.3f || p.point.x < izquierda || p.point.x > derecha) continue;
                if (p.point.y < b.min.y - 0.15f || p.point.y > b.max.y + 0.2f) continue;
                pila.Apoyar(jugador, aterrizaje);
                return true;
            }
        }
        return false;
    }

    void Apoyar(PlayerController2D quien, bool aterrizaje)
    {
        // Walking's first-step and the landing detector may see the same
        // contact in one frame. Both replace earth, but emit one wet support.
        if (quien == ultimoJugador && Time.time - ultimoContacto < ajustes.intervaloMinimoContacto) return;
        int cantidad = contactos.Length > 0 ? contactos[0].Length : 0;
        int indice = Elegir(cantidad, ultimaVariante);
        if (indice < 0) return;
        ultimaVariante = indice;
        ultimoJugador = quien;
        ultimoContacto = Time.time;
        double inicio = AudioSettings.dspTime + 0.01;
        foreach (Canal[] capa in contactos)
        {
            if (indice >= capa.Length) continue;
            Canal c = capa[indice];
            c.intensidad = aterrizaje ? 1f : ajustes.intensidadCaminata;
        }
        AplicarVolumenes(0f);
        foreach (Canal[] capa in contactos)
            if (indice < capa.Length) Reproducir(capa[indice], inicio);
    }

    static void Reproducir(Canal c, double inicio)
    {
        if (c.ajuste == null || c.ajuste.sonido == null) return;
        c.fuente.Stop();
        c.fuente.clip = c.ajuste.sonido;
        c.fuente.PlayScheduled(inicio);
    }

    void Reiniciar()
    {
        nivelVoces = 0f;
        siguienteVoz = 0.12f;
        ultimoContacto = -100f;
        if (voces != null) foreach (Canal c in voces) c.fuente.Stop();
        if (contactos != null) foreach (Canal[] capa in contactos) foreach (Canal c in capa) c.fuente.Stop();
    }

    void OnDisable() { pilas.Remove(this); Reiniciar(); }
}
