using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class HopeLevel2Footsteps : MonoBehaviour
{
    public HopeLevel2FootstepSettings ajustes;
    PlayerController2D jugador;
    AudioSource fuente;
    Vector3 anterior;
    float recorrido;
    bool primerPaso, pausado;
    int ultimo = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preparar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "Nivel_2") return;
        foreach (var raiz in escena.GetRootGameObjects())
            foreach (var pp in raiz.GetComponentsInChildren<PlayerController2D>(true))
                if (pp.GetComponent<HopeLevel2Footsteps>() == null)
                    pp.gameObject.AddComponent<HopeLevel2Footsteps>();
    }

    void Start()
    {
        jugador = GetComponent<PlayerController2D>();
        if (ajustes == null) ajustes = Resources.Load<HopeLevel2FootstepSettings>("HopeLevel2FootstepSettings");
        if (jugador == null || ajustes == null || ajustes.salida == null || ajustes.pasos == null || ajustes.pasos.Length == 0)
        { Debug.LogError("Hope: faltan ajustes, clips o salida de pasos de nivel 2.", this); enabled = false; return; }
        foreach (var clip in ajustes.pasos)
            if (clip == null) { Debug.LogError("Hope: hay un clip de pasos de nivel 2 sin asignar.", this); enabled = false; return; }
        var objeto = new GameObject("F - Pasos metal nivel 2");
        objeto.transform.SetParent(transform, false);
        fuente = objeto.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0;
        fuente.dopplerLevel = 0;
        fuente.volume = 1;
        fuente.outputAudioMixerGroup = ajustes.salida;
        anterior = transform.position;
    }

    void LateUpdate()
    {
        if (fuente == null) return;
        bool pausa = Time.timeScale == 0;
        if (pausa != pausado)
        {
            if (pausa) fuente.Pause(); else fuente.UnPause();
            pausado = pausa;
        }
        Vector3 actual = transform.position;
        float distancia = Mathf.Abs(actual.x - anterior.x);
        float desplazamiento = Vector2.Distance(actual, anterior);
        anterior = actual;
        if (pausa || AudioListener.pause) return;
        bool teletransporte = desplazamiento > Mathf.Max(1, Mathf.Abs(jugador.moveSpeed) * Time.deltaTime * 3);
        if (!jugador.PuedeSonarPaso || teletransporte)
        {
            recorrido = 0;
            primerPaso = false;
            if (teletransporte) fuente.Stop();
            return;
        }
        if (!primerPaso)
        {
            if (distancia <= .0001f) return;
            primerPaso = true;
            recorrido = 0;
            Paso();
            return;
        }
        recorrido += distancia;
        float zancada = Mathf.Max(.05f, Mathf.Abs(jugador.moveSpeed) * ajustes.segundosEntrePasos);
        if (recorrido < zancada) return;
        recorrido %= zancada;
        Paso();
    }

    void Paso()
    {
        int cantidad = ajustes.pasos.Length;
        int indice = Random.Range(0, cantidad);
        if (cantidad > 1 && indice == ultimo) indice = (indice + Random.Range(1, cantidad)) % cantidad;
        ultimo = indice;
        float minimo = Mathf.Min(ajustes.intensidadMinima, ajustes.intensidadMaxima);
        float maximo = Mathf.Max(ajustes.intensidadMinima, ajustes.intensidadMaxima);
        // OneShot permite superponer las colas de sala sin cortar la pisada anterior.
        fuente.PlayOneShot(ajustes.pasos[indice], Random.Range(minimo, maximo));
    }
}
