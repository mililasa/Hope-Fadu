using UnityEngine;

[DisallowMultipleComponent]
public sealed class HopeJumpLanding : MonoBehaviour
{
    public HopeFoleyClipSettings apoyos, saltos, capa;
    AudioSource fuenteApoyo, fuenteSalto, fuenteCapa;
    PlayerController2D jugador;
    float tiempoAire, ignorarHasta;

    void Awake()
    {
        jugador = GetComponent<PlayerController2D>();
        if (apoyos == null) apoyos = Resources.Load<HopeFoleyClipSettings>("HopeLandingSettings");
        if (saltos == null) saltos = Resources.Load<HopeFoleyClipSettings>("HopeJumpSettings");
        if (capa == null) capa = Resources.Load<HopeFoleyClipSettings>("HopeCapeSettings");
        fuenteApoyo = Crear("F - Apoyos (canal 11)");
        fuenteSalto = Crear("F - Saltos (canal 12)");
        fuenteCapa = Crear("F - Capa durante salto");
        if (apoyos == null || saltos == null || capa == null)
            Debug.LogWarning("Hope: falta la configuracion de apoyos o saltos.", this);
        Reiniciar();
    }

    AudioSource Crear(string nombre)
    {
        GameObject objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        AudioSource fuente = objeto.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
        return fuente;
    }

    void LateUpdate()
    {
        if (jugador == null || !jugador.enabled || jugador.BloqueaFoleyDeSalto || Time.time < ignorarHasta)
        {
            tiempoAire = 0f;
            return;
        }
        if (Time.deltaTime <= 0f) return;
        if (!jugador.EnSueloParaFoley) tiempoAire += Time.deltaTime;
        else
        {
            // Filtra pequeños cortes del sensor al caminar sobre bordes.
            if (tiempoAire >= 0.08f) Reproducir(fuenteApoyo, apoyos);
            tiempoAire = 0f;
        }
    }

    public void Saltar(bool reproducirSaltoNormal = true)
    {
        if (reproducirSaltoNormal) Reproducir(fuenteSalto, saltos);
        if (fuenteCapa != null) fuenteCapa.Stop();
        Reproducir(fuenteCapa, capa);
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
