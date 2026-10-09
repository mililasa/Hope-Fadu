using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public sealed class HopeLanternAudio : MonoBehaviour
{
    public HopeLanternSettings ajustes;
    AudioSource chispa, encendido, fuego;
    Transform oyente;
    GameObject luz;
    bool activo;
    float inicioFuego;

    void Awake()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeLanternSettings>("HopeLanternSettings");
        chispa = Fuente("F - Faroles chispa (canal 14)", false);
        encendido = Fuente("F - Faroles encendido (canal 15)", false);
        fuego = Fuente("F - Gas fuego faroles (canal 16)", true);
    }

    AudioSource Fuente(string nombre, bool loop)
    {
        var objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        var fuente = objeto.AddComponent<AudioSource>();
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
        luz = objetivo;
        oyente = jugador;
        activo = true;
        ActualizarVolumen();
        StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        chispa.PlayOneShot(ajustes.chispa);
        yield return new WaitForSeconds(ajustes.retrasoEncendido);
        if (!activo || luz == null || !luz.activeInHierarchy) yield break;
        encendido.PlayOneShot(ajustes.encendido);
        fuego.clip = ajustes.fuego;
        inicioFuego = Time.time;
        fuego.volume = 0f;
        fuego.Play();
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
        fuego.volume = ajustes.volumenFuego * nivel * Mathf.Clamp01((Time.time - inicioFuego) / Mathf.Max(0.01f, ajustes.entradaFuego));
        chispa.panStereo = encendido.panStereo = fuego.panStereo = pan;
    }

    public void Reiniciar()
    {
        StopAllCoroutines();
        activo = false;
        if (chispa != null) chispa.Stop();
        if (encendido != null) encendido.Stop();
        if (fuego != null) fuego.Stop();
    }
    void OnDisable() { Reiniciar(); }
}
