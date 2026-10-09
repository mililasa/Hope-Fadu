using UnityEngine;

[DisallowMultipleComponent]
public sealed class HopeLampRattle : MonoBehaviour
{
    public HopeLampRattleSettings ajustes;
    PlayerController2D jugador;
    AudioSource fuente;
    Vector3 anterior;
    float recorrido;
    int ultimo = -1;

    void Start()
    {
        jugador = GetComponent<PlayerController2D>();
        if (ajustes == null) ajustes = Resources.Load<HopeLampRattleSettings>("HopeLampRattleSettings");
        if (jugador == null || ajustes == null || ajustes.sonidos == null || ajustes.sonidos.Length == 0)
        {
            Debug.LogWarning("Hope: faltan ajustes o clips del canal F - Lampara.", this);
            enabled = false;
            return;
        }
        GameObject objeto = new GameObject("F - Lampara metal y vidrio (canal 13)");
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
            return;
        }
        // Medir avance real evita movimiento sonoro al empujar una pared o quedarse quieto.
        recorrido += distancia;
        float zancada = Mathf.Max(0.05f, Mathf.Abs(jugador.moveSpeed) * ajustes.segundosEntreMovimientos);
        if (recorrido < zancada) return;
        recorrido %= zancada;
        int cantidad = ajustes.sonidos.Length;
        int indice = Random.Range(0, cantidad);
        if (cantidad > 1 && indice == ultimo) indice = (indice + Random.Range(1, cantidad)) % cantidad;
        ultimo = indice;
        if (ajustes.sonidos[indice] == null) return;
        fuente.volume = ajustes.volumenCanal;
        fuente.PlayOneShot(ajustes.sonidos[indice], Random.Range(ajustes.volumenFragmentoMin, ajustes.volumenFragmentoMax));
    }
}
