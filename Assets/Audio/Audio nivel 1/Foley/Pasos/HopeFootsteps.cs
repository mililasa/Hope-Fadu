using UnityEngine;

[DisallowMultipleComponent]
public sealed class HopeFootsteps : MonoBehaviour
{
    public HopeFootstepSettings ajustes;
    PlayerController2D jugador;
    AudioSource fuente;
    FaroFez faro;
    bool esFaro;
    HopeFootstepSettings tierra;
    HopeLevel2FootstepSettings metal;
    Collider2D escaleraEntrada, cuerpo;
    Rigidbody2D fisica;
    readonly ContactPoint2D[] contactos = new ContactPoint2D[12];
    public enum Superficie { Piedra, Tierra, Metal }
    Superficie superficieAnterior;
    int ultimaTierra = -1, ultimoMetal = -1;
    Vector3 anterior;
    float recorrido;
    bool primerPasoEmitido;
    int ultimo = -1;

    void Start()
    {
        jugador = GetComponent<PlayerController2D>();
        if (ajustes == null) ajustes = Resources.Load<HopeFootstepSettings>(gameObject.scene.name == "Nivel_3" ? "HopeFootstepSettingsFaroConservado" : "HopeFootstepSettings");
        if (jugador == null || ajustes == null || ajustes.pasos == null || ajustes.pasos.Length == 0)
        {
            Debug.LogWarning("Hope: faltan ajustes o clips del canal F - Pasos.", this);
            enabled = false;
            return;
        }
        GameObject objeto = new GameObject("F - Pasos (canal 10)");
        objeto.transform.SetParent(transform, false);
        fuente = objeto.AddComponent<AudioSource>();
        HopeAudioRouting.Asignar(fuente, HopeAudioBus.Foley);
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
        esFaro = gameObject.scene.name == "Nivel_3";
        if (esFaro)
        {
            faro = FindObjectOfType<FaroFez>();
            tierra = Resources.Load<HopeFootstepSettings>("HopeFootstepSettings");
            metal = Resources.Load<HopeLevel2FootstepSettings>("HopeLevel2FootstepSettings");
            cuerpo = GetComponent<Collider2D>();
            fisica = GetComponent<Rigidbody2D>();
            foreach (var raiz in gameObject.scene.GetRootGameObjects())
            {
                if (raiz.name != "escalera") continue;
                escaleraEntrada = raiz.GetComponent<EdgeCollider2D>();
                break;
            }
        }
        anterior = transform.position;
    }

    void LateUpdate()
    {
        Vector3 actual = transform.position;
        float distancia = Mathf.Abs(actual.x - anterior.x);
        float desplazamiento = Vector2.Distance(actual, anterior);
        anterior = actual;
        if (Time.deltaTime <= 0f || !jugador.PuedeSonarPaso ||
            desplazamiento > Mathf.Max(1f, Mathf.Abs(jugador.moveSpeed) * Time.deltaTime * 3f))
        {
            recorrido = 0f;
            primerPasoEmitido = false;
            return;
        }
        // Primer paso con el primer avance, sin esperar una zancada completa.
        // Esperar avance real evita sonido si la flecha solo empuja contra una pared.
        Superficie superficie = SuperficieActual();
        if (!primerPasoEmitido || superficie != superficieAnterior)
        {
            if (distancia <= 0.0001f) return;
            primerPasoEmitido = true;
            superficieAnterior = superficie;
            recorrido = 0f;
            ReproducirPaso(superficie);
            return;
        }
        // Los siguientes pasos conservan la cadencia por distancia recorrida.
        recorrido += distancia;
        float segundos = ajustes.segundosEntrePasos;
        if (superficie == Superficie.Metal && metal != null) segundos = metal.segundosEntrePasos;
        if (superficie == Superficie.Tierra && tierra != null) segundos = tierra.segundosEntrePasos;
        float zancada = Mathf.Max(0.05f, Mathf.Abs(jugador.moveSpeed) * segundos);
        if (recorrido < zancada) return;
        recorrido %= zancada;
        ReproducirPaso(superficie);
    }

    public Superficie SuperficieActual()
    {
        if (!esFaro || (faro != null && faro.MezclaInteriorAudio >= .5f))
            return Superficie.Piedra;
        if (escaleraEntrada == null || !escaleraEntrada.isActiveAndEnabled || cuerpo == null)
            return Superficie.Tierra;

        Bounds limites = cuerpo.bounds;
        Vector2 pie = new Vector2(limites.center.x, limites.min.y);
        Bounds escalera = escaleraEntrada.bounds;
        if (pie.x < escalera.min.x || pie.x > escalera.max.x)
            return Superficie.Tierra;

        int cantidad = fisica != null ? fisica.GetContacts(contactos) : 0;
        for (int i = 0; i < cantidad; i++)
        {
            var contacto = contactos[i];
            if (contacto.normal.y > .4f &&
                (contacto.collider == escaleraEntrada || contacto.otherCollider == escaleraEntrada))
                return Superficie.Metal;
        }
        Vector2 apoyo = escaleraEntrada.ClosestPoint(pie);
        return (pie - apoyo).sqrMagnitude <= .17f * .17f ? Superficie.Metal : Superficie.Tierra;
    }

    static int Elegir(AudioClip[] clips, ref int ultimoIndice)
    {
        int indice = Random.Range(0, clips.Length);
        if (clips.Length > 1 && indice == ultimoIndice)
            indice = (indice + Random.Range(1, clips.Length)) % clips.Length;
        ultimoIndice = indice;
        return indice;
    }

    void ReproducirPaso(Superficie superficie)
    {
        if (superficie == Superficie.Metal && metal != null && metal.pasos != null && metal.pasos.Length > 0)
        {
            // Compensa la atenuacion del fader de metal del nivel 2.
            fuente.volume = .46f;
            HopeSurfaceMix.Reproducir(fuente, metal.pasos[Elegir(metal.pasos, ref ultimoMetal)],
                Random.Range(metal.intensidadMinima, metal.intensidadMaxima), 2);
            return;
        }
        if (superficie == Superficie.Tierra && tierra != null && tierra.pasos != null && tierra.pasos.Length > 0)
        {
            fuente.volume = tierra.volumenCanal;
            HopeSurfaceMix.Reproducir(fuente, tierra.pasos[Elegir(tierra.pasos, ref ultimaTierra)],
                Random.Range(tierra.volumenFragmentoMin, tierra.volumenFragmentoMax), 1);
            return;
        }
        if (HopeCorpsePileAudio.TryPlayContacto(jugador, false)) return;
        if (HopeWoodFootsteps.TryPlay(jugador)) return;
        int indice = Elegir(ajustes.pasos, ref ultimo);
        if (ajustes.pasos[indice] == null) return;
        fuente.volume = ajustes.volumenCanal;
        float intensidad = Random.Range(ajustes.volumenFragmentoMin, ajustes.volumenFragmentoMax);
        HopeSurfaceMix.Reproducir(fuente, ajustes.pasos[indice], intensidad);
        float interior = faro != null ? Mathf.Clamp01(faro.MezclaInteriorAudio) : 0f;
        var controles = esFaro ? HopeSurfaceMix.Nivel3 : null;
        if (ajustes.ataquesAdoquin != null && indice < ajustes.ataquesAdoquin.Length &&
            ajustes.ataquesAdoquin[indice] != null)
        {
            float nivel = Mathf.Lerp(ajustes.ataqueExterior, controles != null ? controles.ataquePiedra : ajustes.ataqueInterior, interior);
            HopeAjustes.Disparo(fuente, ajustes.ataquesAdoquin[indice], intensidad * nivel);
        }
        if (ajustes.reverbsAdoquin != null && indice < ajustes.reverbsAdoquin.Length &&
            ajustes.reverbsAdoquin[indice] != null)
        {
            float nivel = Mathf.Lerp(ajustes.reverbExterior, controles != null ? controles.reverbPiedra : ajustes.reverbInterior, interior);
            HopeAjustes.Disparo(fuente, ajustes.reverbsAdoquin[indice], intensidad * nivel);
        }
    }
}
