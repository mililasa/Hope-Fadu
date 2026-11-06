using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public sealed class HopeLanternAudio : MonoBehaviour
{
    public HopeLanternSettings ajustes;
    HopeLanternSettings ajustesFuego;
    AudioSource chispa, encendido, fuego, cuerpoBarril;
    Transform oyente;
    GameObject luz;
    bool activo;
    float inicioFuego;

    void Awake()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeLanternSettings>("HopeLanternSettings");
        chispa = Fuente("F - Faroles chispa (canal 14)", false);
        encendido = Fuente("F - Faroles encendido (canal 15)", false);
        bool nivelDos = gameObject.scene.name == "Nivel_2";
        fuego = Fuente(nivelDos ? "F - Barril leña" : "F - Gas fuego faroles (canal 16)", true);
        if (nivelDos) cuerpoBarril = Fuente("F - Barril resonancia metálica", true);
    }

    AudioSource Fuente(string nombre, bool loop)
    {
        var objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        var fuente = objeto.AddComponent<AudioSource>();
        HopeAudioRouting.Asignar(fuente, HopeAudioBus.Foley);
        fuente.playOnAwake = false;
        fuente.loop = loop;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
        fuente.volume = 0f;
        return fuente;
    }

    public void Encender(GameObject objetivo, Transform jugador)
    {
        if (activo || objetivo == null || jugador == null) return;
        if (ajustes == null || ajustes.chispa == null || ajustes.encendido == null || ajustes.fuego == null)
        {
            Debug.LogWarning("Hope: faltan clips o ajustes de faroles.", this);
            return;
        }
        ajustesFuego = ajustes;
        if (gameObject.scene.name == "SampleScene" &&
            objetivo.TryGetComponent<Lantern>(out var farol) && farol.EsBarril)
        {
            var barriles = Resources.Load<HopeLanternSettings>("HopeLevel2LanternSettings");
            if (barriles != null && barriles.fuego != null && barriles.cuerpoBarril != null)
            {
                ajustesFuego = barriles;
                if (cuerpoBarril == null)
                    cuerpoBarril = Fuente("F - Faroles resonancia del barril", true);
            }
        }
        luz = objetivo;
        oyente = jugador;
        activo = true;
        ActualizarVolumen();
        StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        HopeAjustes.Disparo(chispa, ajustes.chispa);
        yield return new WaitForSeconds(ajustes.retrasoEncendido);
        if (!activo || luz == null || !luz.activeInHierarchy) yield break;
        HopeAjustes.Disparo(encendido, ajustes.encendido);
        fuego.clip = ajustesFuego.fuego;
        inicioFuego = Time.time;
        fuego.volume = 0f;
        fuego.Play();
        if (cuerpoBarril != null && ajustesFuego.cuerpoBarril != null)
        {
            cuerpoBarril.clip = ajustesFuego.cuerpoBarril;
            cuerpoBarril.volume = 0f;
            cuerpoBarril.Play();
        }
    }

    void Update()
    {
        if (!activo) return;
        if (luz == null || !luz.activeInHierarchy || oyente == null) { Reiniciar(); return; }
        ActualizarVolumen();
    }

    void ActualizarVolumen()
    {
        float distancia = Vector2.Distance(transform.position, oyente.position);
        float rango = Mathf.Max(0.1f, ajustes.alcance - ajustes.distanciaPlena);
        float nivel = 1f - Mathf.Clamp01((distancia - ajustes.distanciaPlena) / rango);
        float pan = Mathf.Clamp((transform.position.x - oyente.position.x) / Mathf.Max(0.1f, ajustes.alcance), -0.7f, 0.7f);
        chispa.volume = ajustes.volumenChispa * nivel;
        encendido.volume = ajustes.volumenEncendido * nivel;
        float rangoFuego = Mathf.Max(.1f, ajustesFuego.alcance - ajustesFuego.distanciaPlena);
        float nivelFuego = 1f - Mathf.Clamp01((distancia - ajustesFuego.distanciaPlena) / rangoFuego);
        if (ajustesFuego.cuerpoBarril != null) nivelFuego = Mathf.SmoothStep(0f, 1f, nivelFuego);
        float entrada = Mathf.Clamp01((Time.time - inicioFuego) / Mathf.Max(.01f, ajustesFuego.entradaFuego));
        fuego.volume = ajustesFuego.volumenFuego * nivelFuego * entrada;
        chispa.panStereo = encendido.panStereo = pan;
        float panFuego = Mathf.Clamp((transform.position.x - oyente.position.x) /
            Mathf.Max(.1f, ajustesFuego.alcance), -.7f, .7f);
        fuego.panStereo = panFuego;
        if (cuerpoBarril != null)
        {
            cuerpoBarril.volume = ajustesFuego.volumenCuerpoBarril * nivelFuego * entrada;
            cuerpoBarril.panStereo = panFuego;
        }
    }

    public void Reiniciar()
    {
        StopAllCoroutines();
        activo = false;
        if (chispa != null) chispa.Stop();
        if (encendido != null) encendido.Stop();
        if (fuego != null) fuego.Stop();
        if (cuerpoBarril != null) cuerpoBarril.Stop();
    }
    void OnDisable() { Reiniciar(); }
}
