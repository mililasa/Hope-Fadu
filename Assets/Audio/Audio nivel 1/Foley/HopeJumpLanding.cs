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
    HopeLevel2CapeSettings ajustesCapaNivel2;
    void Awake()
    {
        jugador = GetComponent<PlayerController2D>();
        esNivel2 = gameObject.scene.name == "Nivel_2";
        if (esNivel2)
        {
            ajustesCapaNivel2 = Resources.Load<HopeLevel2CapeSettings>("HopeLevel2CapeSettings");
            fuenteCapa = Crear("F - Capa doble salto nivel 2");
            if (ajustesCapaNivel2 != null && ajustesCapaNivel2.sonido != null && ajustesCapaNivel2.salida != null)
                fuenteCapa.outputAudioMixerGroup = ajustesCapaNivel2.salida;
            else Debug.LogError("Hope: falta el sonido o la salida de capa de Nivel 2.", this);
            Reiniciar();
            return;
        }
        if (apoyos == null) apoyos = Resources.Load<HopeFoleyClipSettings>("HopeLandingSettings");
        if (saltos == null) saltos = Resources.Load<HopeFoleyClipSettings>("HopeJumpSettings");
        if (capa == null) capa = Resources.Load<HopeFoleyClipSettings>("HopeCapeSettings");
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
            // Filtra pequeÃ±os cortes del sensor al caminar sobre bordes.
            if (tiempoAire >= 0.08f && !HopeCorpsePileAudio.TryPlayContacto(jugador, true))
                Reproducir(fuenteApoyo, apoyos);
            tiempoAire = 0f;
        }
    }

    public void Saltar(bool reproducirSaltoNormal = true, bool dobleSalto = false)
    {
        if (esNivel2)
        {
            if (dobleSalto && fuenteCapa != null && ajustesCapaNivel2 != null && ajustesCapaNivel2.sonido != null)
            {
                fuenteCapa.Stop(); fuenteCapa.volume = ajustesCapaNivel2.volumenCanal;
                fuenteCapa.PlayOneShot(ajustesCapaNivel2.sonido, ajustesCapaNivel2.volumenFragmento);
            }
            return;
        }
        if (reproducirSaltoNormal) Reproducir(fuenteSalto, saltos);
        if (esNivel2 && !dobleSalto) return;
        if (fuenteCapa != null) fuenteCapa.Stop();
        if (esNivel2 && ajustesCapaNivel2 != null && ajustesCapaNivel2.sonido != null)
        {
            fuenteCapa.volume = ajustesCapaNivel2.volumenCanal;
            fuenteCapa.PlayOneShot(ajustesCapaNivel2.sonido, ajustesCapaNivel2.volumenFragmento);
        }
        else Reproducir(fuenteCapa, capa);
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
        fuente.PlayOneShot(ajustes.sonido, ajustes.volumenFragmento);
    }
}
