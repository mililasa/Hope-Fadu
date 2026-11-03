using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1700)]
public sealed class HopeEnemyAudio : MonoBehaviour
{
    public HopeEnemyAudioSettings ajustes;
    EnemigoRecorrido enemigo;
    PlayerController2D hope;
    Voz pasos, aire, ropa, deteccion;
    float ultimoX, recorrido, respirarEn;
    bool moviendo, estabaMoviendo, pausado;

    sealed class Voz
    {
        public HopeEnemyAudioLayer capa;
        public AudioSource fuente;
        public float ganancia;
        public int ultimo = -1;
    }
    public static void Conectar(EnemigoRecorrido enemigo)
    {
        if (enemigo == null || enemigo.gameObject.scene.name != "Nivel_2") return;
        if (enemigo.GetComponent<HopeEnemyAudio>() == null) enemigo.gameObject.AddComponent<HopeEnemyAudio>();
    }
    void Start()
    {
        enemigo = GetComponent<EnemigoRecorrido>();
        if (ajustes == null) ajustes = Resources.Load<HopeEnemyAudioSettings>("HopeEnemyAudioSettings");
        foreach (var raiz in gameObject.scene.GetRootGameObjects())
        {
            hope = raiz.GetComponentInChildren<PlayerController2D>();
            if (hope != null) break;
        }
        if (enemigo == null || ajustes == null || hope == null)
        { Debug.LogError("Hope: falta configuracion o personaje para audio del enemigo.", this); enabled = false; return; }
        pasos = Crear("Enemigo - Pisadas", ajustes.pisadas, false, HopeAudioBus.Foley);
        aire = Crear("Enemigo - Respiracion", ajustes.respiracion, false, HopeAudioBus.Foley);
        ropa = Crear("Enemigo - Crujidos", ajustes.crujidos, false, HopeAudioBus.Foley);
        deteccion = Crear("Enemigo - Zumbido deteccion", ajustes.zumbido, true, HopeAudioBus.Mecanicas);
        ultimoX = transform.position.x;
        respirarEn = Random.Range(ajustes.primeraRespiracionMin, Mathf.Max(ajustes.primeraRespiracionMin, ajustes.primeraRespiracionMax));
    }
    Voz Crear(string nombre, HopeEnemyAudioLayer capa, bool loop, HopeAudioBus bus)
    {
        var go = new GameObject(nombre); go.transform.SetParent(transform, false);
        var fuente = go.AddComponent<AudioSource>();
        fuente.playOnAwake = false; fuente.loop = loop; fuente.spatialBlend = 0;
        fuente.dopplerLevel = 0; fuente.volume = 0;
        if (capa != null && capa.salida != null) fuente.outputAudioMixerGroup = capa.salida;
        else HopeAudioRouting.Asignar(fuente, bus);
        return new Voz { capa = capa, fuente = fuente };
    }
    void LateUpdate()
    {
        if (pasos == null || enemigo == null || hope == null) return;
        bool pausa = Time.timeScale == 0 || AudioListener.pause;
        if (pausa != pausado)
        {
            Pausa(pasos, pausa); Pausa(aire, pausa); Pausa(ropa, pausa); Pausa(deteccion, pausa);
            pausado = pausa;
        }
        float dx = Mathf.Abs(transform.position.x - ultimoX); ultimoX = transform.position.x;
        if (pausa) return;
        // Teleports/checkpoint resets must not produce a burst of footsteps.
        bool saltoPosicion = dx > Mathf.Max(3, enemigo.velocidadCorrer * Time.deltaTime * 4);
        moviendo = enemigo.enabled && !saltoPosicion && dx > .0001f;
        Vector2 delta = enemigo.PosicionSonora - (Vector2)hope.transform.position;
        float distancia = delta.magnitude;
        Actualizar(pasos, distancia, delta.x); Actualizar(aire, distancia, delta.x);
        Actualizar(ropa, distancia, delta.x); Actualizar(deteccion, distancia, delta.x);
        if (moviendo)
        {
            recorrido += dx;
            if (!ajustes.usarEventosDeAnimacion && (!estabaMoviendo || recorrido >= ajustes.distanciaEntrePasos))
            {
                EmitirPaso(); recorrido = 0;
            }
        }
        else recorrido = 0;
        estabaMoviendo = moviendo;
        if (Permitida(aire) && aire.ganancia > .001f)
        {
            respirarEn -= Time.deltaTime;
            if (respirarEn <= 0)
            {
                Disparar(aire);
                respirarEn = Random.Range(ajustes.intervaloRespiracionMin,
                    Mathf.Max(ajustes.intervaloRespiracionMin, ajustes.intervaloRespiracionMax));
            }
        }
        if (deteccion.ganancia > .001f && Permitida(deteccion))
        {
            if (!deteccion.fuente.isPlaying && deteccion.capa.clips != null && deteccion.capa.clips.Length > 0 && deteccion.capa.clips[0] != null)
            { deteccion.fuente.clip = deteccion.capa.clips[0]; deteccion.fuente.Play(); }
        }
        else if (deteccion.ganancia <= .001f && deteccion.fuente.isPlaying) deteccion.fuente.Stop();
    }
    bool Permitida(Voz voz)
    {
        return voz.capa != null && voz.capa.habilitada && enemigo.enabled &&
            (!voz.capa.soloAlDetectar || enemigo.PersiguiendoParaAudio) &&
            (!voz.capa.soloEnMovimiento || moviendo);
    }
    void Actualizar(Voz voz, float distancia, float x)
    {
        if (voz.capa == null) return;
        var c = voz.capa;
        float maximo = Mathf.Max(c.distanciaPlena + .1f, c.distanciaMaxima);
        float objetivo = Permitida(voz) ? 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(c.distanciaPlena, maximo, distancia)) : 0;
        float duracion = objetivo > voz.ganancia ? c.entrada : c.salidaFade;
        voz.ganancia = Mathf.MoveTowards(voz.ganancia, objetivo, Time.deltaTime / Mathf.Max(.01f, duracion));
        voz.fuente.volume = c.volumen * voz.ganancia;
        voz.fuente.panStereo = Mathf.Clamp(x / maximo, -1, 1) * ajustes.panoramaMaximo;
    }
    public void PasoDeAnimacion()
    {
        if (ajustes != null && ajustes.usarEventosDeAnimacion && !pausado) EmitirPaso();
    }
    void EmitirPaso() { Disparar(pasos); Disparar(ropa); }
    void Disparar(Voz voz)
    {
        if (voz == null || !Permitida(voz) || voz.capa.clips == null || voz.capa.clips.Length == 0) return;
        int i = Random.Range(0, voz.capa.clips.Length);
        if (voz.capa.clips.Length > 1 && i == voz.ultimo) i = (i + Random.Range(1, voz.capa.clips.Length)) % voz.capa.clips.Length;
        voz.ultimo = i;
        if (voz.capa.clips[i] != null) { voz.fuente.clip = voz.capa.clips[i]; voz.fuente.PlayOneShot(voz.capa.clips[i]); }
    }
    static void Pausa(Voz voz, bool pausa)
    {
        if (voz == null) return;
        if (pausa) voz.fuente.Pause(); else voz.fuente.UnPause();
    }
    void OnDisable()
    {
        foreach (var voz in new[] { pasos, aire, ropa, deteccion })
            if (voz != null) { voz.fuente.Stop(); voz.ganancia = 0; }
        moviendo = estabaMoviendo = false; recorrido = 0;
    }
}
