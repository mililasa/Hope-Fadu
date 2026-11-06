using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public sealed class HopeJumpLanding : MonoBehaviour
{
    public HopeFoleyClipSettings apoyos, saltos, capa;
    AudioSource fuenteApoyo, fuenteSalto, fuenteCapa;
    PlayerController2D jugador;
    float tiempoAire, ignorarHasta;

    bool esNivel2, capaPausada;
    bool esNivel1, esNivel3;
    int ultimoSalto = -1, ultimoApoyo = -1;
    FaroFez faro;
    HopeFootsteps pisadas;
    HopeFoleyClipSettings apoyosTierra;
    int ultimaCapa = -1;
    static readonly Vector2[] rocesCapa =
    {
        new Vector2(.93f, .83f),
        new Vector2(.97f, .96f),
        new Vector2(1f, .88f),
        new Vector2(1.04f, 1f),
        new Vector2(1.08f, .79f)
    };
    HopeLevel2CapeSettings ajustesCapaNivel2;
    void Awake()
    {
        jugador = GetComponent<PlayerController2D>();
        esNivel1 = gameObject.scene.name == "SampleScene";
        esNivel3 = gameObject.scene.name == "Nivel_3";
        esNivel2 = gameObject.scene.name == "Nivel_2";
        if (esNivel2)
        {
            ajustesCapaNivel2 = Resources.Load<HopeLevel2CapeSettings>("HopeLevel2CapeSettings");
            fuenteCapa = Crear("F - Capa durante salto nivel 2");
            if (ajustesCapaNivel2 != null && ajustesCapaNivel2.sonido != null && ajustesCapaNivel2.salida != null)
                fuenteCapa.outputAudioMixerGroup = ajustesCapaNivel2.salida;
            else Debug.LogError("Hope: falta el sonido o la salida de capa de Nivel 2.", this);
            Reiniciar();
            return;
        }
        if (apoyos == null) apoyos = Resources.Load<HopeFoleyClipSettings>(esNivel3 ? "HopeLandingSettingsNivel3" : "HopeLandingSettings");
        if (saltos == null) saltos = Resources.Load<HopeFoleyClipSettings>(gameObject.scene.name == "Nivel_3" ? "HopeJumpSettingsFaroConservado" : "HopeJumpSettings");
        if (capa == null) capa = Resources.Load<HopeFoleyClipSettings>(esNivel3 ? "HopeCapeSettingsNivel3" : "HopeCapeSettings");
        fuenteApoyo = Crear("F - Apoyos (canal 11)");
        fuenteSalto = Crear("F - Saltos (canal 12)");
        fuenteCapa = Crear("F - Capa durante salto");
        if (esNivel2)
        {
            if (ajustesCapaNivel2 != null && ajustesCapaNivel2.salida != null)
                fuenteCapa.outputAudioMixerGroup = ajustesCapaNivel2.salida;
            else Debug.LogError("Hope: falta el ajuste o la salida de Capa doble salto del nivel 2.", this);
        }
        if (apoyos == null || saltos == null || capa == null)
            Debug.LogWarning("Hope: falta la configuracion de apoyos o saltos.", this);
        Reiniciar();
    }

    void Start()
    {
        if (esNivel3)
        {
            faro = FindObjectOfType<FaroFez>();
            pisadas = GetComponent<HopeFootsteps>();
            apoyosTierra = Resources.Load<HopeFoleyClipSettings>("HopeLandingSettings");
        }
    }

    AudioSource Crear(string nombre)
    {
        GameObject objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        AudioSource fuente = objeto.AddComponent<AudioSource>();
        HopeAudioRouting.Asignar(fuente, HopeAudioBus.Foley);
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
        return fuente;
    }

    void LateUpdate()
    {
        if (esNivel2)
        {
            bool pausa = Time.timeScale == 0;
            if (pausa != capaPausada && fuenteCapa != null)
            { if (pausa) fuenteCapa.Pause(); else fuenteCapa.UnPause(); capaPausada = pausa; }
            return;
        }
        if (jugador == null || !jugador.enabled || jugador.BloqueaFoleyDeSalto || Time.time < ignorarHasta)
        {
            tiempoAire = 0f;
            return;
        }
        if (Time.deltaTime <= 0f) return;
        if (!jugador.EnSueloParaFoley) tiempoAire += Time.deltaTime;
        else
        {
            // Filtra pequeÃƒÂ±os cortes del sensor al caminar sobre bordes.
            if (tiempoAire >= 0.08f && !HopeCorpsePileAudio.TryPlayContacto(jugador, true))
                ReproducirApoyo();
            tiempoAire = 0f;
        }
    }

    void ReproducirApoyo()
    {
        var superficie = esNivel3 && pisadas != null
            ? pisadas.SuperficieActual()
            : HopeFootsteps.Superficie.Piedra;
        if (esNivel3 && pisadas != null)
        {
            if (superficie == HopeFootsteps.Superficie.Tierra && apoyosTierra != null)
            {
                fuenteApoyo.volume = apoyosTierra.volumenCanal;
                HopeSurfaceMix.Reproducir(fuenteApoyo, apoyosTierra.sonido, apoyosTierra.volumenFragmento, 1);
                return;
            }

        }
        var clips = apoyos != null ? apoyos.variaciones : null;
        if (!esNivel3 || clips == null || clips.Length == 0)
        {
            if (esNivel1 && apoyos != null && fuenteApoyo != null)
            {
                fuenteApoyo.volume = apoyos.volumenCanal;
                HopeSurfaceMix.Reproducir(fuenteApoyo, apoyos.sonido, apoyos.volumenFragmento);
            }
            else Reproducir(fuenteApoyo, apoyos);
            return;
        }

        int indice = Random.Range(0, clips.Length);
        if (clips.Length > 1 && indice == ultimoApoyo)
            indice = (indice + Random.Range(1, clips.Length)) % clips.Length;
        ultimoApoyo = indice;
        fuenteApoyo.volume = apoyos.volumenCanal;
        var controles = HopeSurfaceMix.Nivel3;
        AudioClip clip = clips[indice];
        if (clip != null)
        {
            AudioClip suave = null;
            if (controles != null && controles.apoyosSuaves != null)
                foreach (var toma in controles.apoyosSuaves)
                    if (toma != null && toma.original == clip) { suave = toma.suave; break; }
            float ataque = suave != null ? controles.ataqueApoyos : 1f;
            if (ataque > 0f) HopeAjustes.Disparo(fuenteApoyo, clip, apoyos.volumenFragmento * ataque);
            if (ataque < 1f) HopeAjustes.Disparo(fuenteApoyo, suave, apoyos.volumenFragmento * (1f - ataque));
        }

        if (superficie == HopeFootsteps.Superficie.Metal) return;
        var colas = apoyos.reverbsApoyo;
        if (colas == null || indice >= colas.Length || colas[indice] == null) return;
        float interior = faro != null ? Mathf.Clamp01(faro.MezclaInteriorAudio) : 0f;
        float reverb = Mathf.Lerp(
            controles != null ? controles.reverbApoyosExterior : apoyos.reverbApoyoExterior,
            controles != null ? controles.reverbApoyosInterior : apoyos.reverbApoyoInterior, interior);
        HopeAjustes.Disparo(fuenteApoyo, colas[indice], apoyos.volumenFragmento * reverb);
    }

    public void Saltar(bool reproducirSaltoNormal = true, bool dobleSalto = false)
    {
        if (esNivel2)
        {
            if (reproducirSaltoNormal && !dobleSalto && fuenteCapa != null && ajustesCapaNivel2 != null && ajustesCapaNivel2.activarCapa && ajustesCapaNivel2.sonido != null)
            {
                fuenteCapa.Stop(); fuenteCapa.volume = ajustesCapaNivel2.volumenCanal;
                ReproducirCapa(ajustesCapaNivel2.sonido, ajustesCapaNivel2.volumenFragmento);
            }
            return;
        }
        if (esNivel1 || esNivel3)
        {
            if (reproducirSaltoNormal && fuenteSalto != null && saltos != null)
            {
                var clips = saltos.variaciones;
                if (clips != null && clips.Length > 0)
                {
                    int i = Random.Range(0, clips.Length);
                    if (clips.Length > 1 && i == ultimoSalto) i = (i + Random.Range(1, clips.Length)) % clips.Length;
                    ultimoSalto = i;
                    fuenteSalto.volume = saltos.volumenCanal;
                    if (clips[i] != null) HopeAjustes.Disparo(fuenteSalto, clips[i], saltos.volumenFragmento);
                }
                else Reproducir(fuenteSalto, saltos);
            }
            if (reproducirSaltoNormal && !dobleSalto)
            {
                if (fuenteCapa != null) fuenteCapa.Stop();
                if (fuenteCapa != null && capa != null && capa.activarCapa)
                {
                    fuenteCapa.volume = capa.volumenCanal;
                    ReproducirCapa(capa.sonido, capa.volumenFragmento);
                }
            }
            return;
        }
        if (reproducirSaltoNormal) Reproducir(fuenteSalto, saltos);
        if (esNivel2 && !dobleSalto) return;
        if (fuenteCapa != null) fuenteCapa.Stop();
        if (esNivel2 && ajustesCapaNivel2 != null && ajustesCapaNivel2.sonido != null)
        {
            fuenteCapa.volume = ajustesCapaNivel2.volumenCanal;
            HopeAjustes.Disparo(fuenteCapa, ajustesCapaNivel2.sonido, ajustesCapaNivel2.volumenFragmento);
        }
        else Reproducir(fuenteCapa, capa);
    }

    void ReproducirCapa(AudioClip clip, float volumen)
    {
        if (clip == null) return;
        int indice = Random.Range(0, rocesCapa.Length);
        if (indice == ultimaCapa)
            indice = (indice + Random.Range(1, rocesCapa.Length)) % rocesCapa.Length;
        ultimaCapa = indice;
        Vector2 roce = rocesCapa[indice];
        fuenteCapa.pitch = roce.x;
        HopeAjustes.Disparo(fuenteCapa, clip, volumen * roce.y);
    }

    public void Reiniciar()
    {
        tiempoAire = 0f;
        ignorarHasta = Time.time + 0.15f;
        if (fuenteApoyo != null) fuenteApoyo.Stop();
        if (fuenteSalto != null) fuenteSalto.Stop();
        if (fuenteCapa != null) fuenteCapa.Stop();
    }

    static void Reproducir(AudioSource fuente, HopeFoleyClipSettings ajustes)
    {
        if (fuente == null || ajustes == null || ajustes.sonido == null) return;
        fuente.volume = ajustes.volumenCanal;
        HopeAjustes.Disparo(fuente, ajustes.sonido, ajustes.volumenFragmento);
    }
}
