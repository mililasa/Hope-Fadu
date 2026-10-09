using UnityEngine;

[DisallowMultipleComponent]
public sealed class HopeFootsteps : MonoBehaviour
{
    public HopeFootstepSettings ajustes;
    PlayerController2D jugador;
    AudioSource fuente;
    Vector3 anterior;
    float recorrido;
    bool primerPasoEmitido;
    int ultimo = -1;

    void Start()
    {
        jugador = GetComponent<PlayerController2D>();
        if (ajustes == null) ajustes = Resources.Load<HopeFootstepSettings>("HopeFootstepSettings");
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
        if (!primerPasoEmitido)
        {
            if (distancia <= 0.0001f) return;
            primerPasoEmitido = true;
            recorrido = 0f;
            ReproducirPaso();
            return;
        }
        // Los siguientes pasos conservan la cadencia por distancia recorrida.
        recorrido += distancia;
        float zancada = Mathf.Max(0.05f, Mathf.Abs(jugador.moveSpeed) * ajustes.segundosEntrePasos);
        if (recorrido < zancada) return;
        recorrido %= zancada;
        ReproducirPaso();
    }

    void ReproducirPaso()
    {
        int cantidad = ajustes.pasos.Length;
        int indice = Random.Range(0, cantidad);
        if (cantidad > 1 && indice == ultimo) indice = (indice + Random.Range(1, cantidad)) % cantidad;
        ultimo = indice;
        if (ajustes.pasos[indice] == null) return;
        fuente.volume = ajustes.volumenCanal;
        fuente.PlayOneShot(ajustes.pasos[indice], Random.Range(ajustes.volumenFragmentoMin, ajustes.volumenFragmentoMax));
    }
}
