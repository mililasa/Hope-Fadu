using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class HopeLevelAmbience : MonoBehaviour
{
    public HopeAmbienceSettings ajustes;
    AudioSource viento, presion, arcos;
    readonly List<Renderer> zonas = new List<Renderer>();
    float tiempo, intensidad;

    void Start()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeAmbienceSettings>("HopeAmbienceSettings");
        if (ajustes == null || ajustes.viento == null || ajustes.presion == null || ajustes.arcos == null)
        {
            Debug.LogWarning("Hope: faltan ajustes o clips de ambientes.", this);
            enabled = false;
            return;
        }
        foreach (string nombre in ajustes.nombresArcos)
        {
            GameObject arco = GameObject.Find(nombre);
            Renderer zona = arco != null ? arco.GetComponent<Renderer>() : null;
            if (zona != null) zonas.Add(zona);
            else Debug.LogWarning("Hope: no se encontro el arco de ambiente: " + nombre, this);
        }
        viento = Fuente("A - Viento", ajustes.viento);
        presion = Fuente("A - Presion de oscuridad", ajustes.presion);
        arcos = Fuente("A - Viento Intenso (arcos)", ajustes.arcos);
        viento.Play();
        presion.Play();
    }

    AudioSource Fuente(string nombre, AudioClip clip)
    {
        GameObject objeto = new GameObject(nombre);
        objeto.transform.SetParent(transform, false);
        AudioSource fuente = objeto.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
        fuente.clip = clip;
        fuente.volume = 0f;
        return fuente;
    }

    void Update()
    {
        if (viento == null) return;
        tiempo += Time.deltaTime;
        viento.volume = ajustes.volumenViento * Mathf.Clamp01(tiempo);
        presion.volume = ajustes.volumenPresion * Mathf.Clamp01(tiempo / 2f);
        float objetivo = 0f;
        foreach (Renderer zona in zonas)
        {
            if (zona == null || !zona.enabled || !zona.gameObject.activeInHierarchy) continue;
            Bounds limites = zona.bounds;
            Vector3 posicion = transform.position;
            posicion.z = limites.center.z;
            float distancia = Vector3.Distance(posicion, limites.ClosestPoint(posicion));
            objetivo = Mathf.Max(objetivo, Mathf.Clamp01(1f - distancia / Mathf.Max(0.1f, ajustes.alcanceArcos)));
        }
        // Una sola voz evita duplicar volumen si las zonas se superponen.
        intensidad = Mathf.MoveTowards(intensidad, objetivo, Time.deltaTime / Mathf.Max(0.1f, ajustes.fundidoArcos));
        if (objetivo > 0f && !arcos.isPlaying) arcos.Play();
        arcos.volume = ajustes.volumenArcos * intensidad;
        if (objetivo <= 0f && intensidad <= 0f && arcos.isPlaying) arcos.Stop();
    }
}
