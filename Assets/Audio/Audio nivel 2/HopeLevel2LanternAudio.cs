using UnityEngine;
using UnityEngine.SceneManagement;

// Reutiliza las secuencias de nivel 1 con copias independientes de sus ajustes.
// Observa el encendido real del farol: no modifica interacción, luz ni checkpoint.
[DisallowMultipleComponent]
public sealed class HopeLevel2LanternAudio : MonoBehaviour
{
    Lantern farol;
    HopeLanternAudio foley;
    HopeCheckpointAudio checkpoint;
    Transform jugador;
    Lantern.Estado anterior;

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
            foreach (var farol in raiz.GetComponentsInChildren<Lantern>(true))
                if (farol.name == "Farol (5)" && farol.GetComponent<HopeLevel2LanternAudio>() == null)
                    farol.gameObject.AddComponent<HopeLevel2LanternAudio>();
    }

    void Start()
    {
        farol = GetComponent<Lantern>();
        var ajustesFoley = Resources.Load<HopeLanternSettings>("HopeLevel2LanternSettings");
        var ajustesCheckpoint = Resources.Load<HopeCheckpointSettings>("HopeLevel2CheckpointSettings");
        if (farol == null || ajustesFoley == null || ajustesCheckpoint == null)
        {
            Debug.LogError("Hope: falta la configuración del audio del farol de nivel 2.", this);
            enabled = false;
            return;
        }
        foreach (var raiz in gameObject.scene.GetRootGameObjects())
        {
            var pp = raiz.GetComponentInChildren<PlayerController2D>();
            if (pp != null) { jugador = pp.transform; break; }
        }
        foley = GetComponent<HopeLanternAudio>();
        if (foley == null) foley = gameObject.AddComponent<HopeLanternAudio>();
        foley.ajustes = ajustesFoley;
        var mezcla = Resources.Load<HopeLevel2DripsSettings>("HopeLevel2DripsSettings");
        if (mezcla != null && mezcla.faroles != null)
        {
            foreach (var fuente in foley.GetComponentsInChildren<AudioSource>(true))
                if (fuente.transform.parent == transform && fuente.name.StartsWith("F - "))
                    fuente.outputAudioMixerGroup = mezcla.faroles;
        }

        checkpoint = GetComponent<HopeCheckpointAudio>();
        if (checkpoint == null) checkpoint = gameObject.AddComponent<HopeCheckpointAudio>();
        checkpoint.ajustes = ajustesCheckpoint;
        anterior = Lantern.Estado.Off;
    }

    void LateUpdate()
    {
        if (farol == null || foley == null || checkpoint == null) return;
        var actual = farol.EstadoActual;
        if (actual != Lantern.Estado.Off && anterior == Lantern.Estado.Off && jugador != null)
        {
            foley.Encender(gameObject, jugador);
            checkpoint.Activar(gameObject, true);
        }
        else if (actual == Lantern.Estado.Off && anterior != Lantern.Estado.Off)
        {
            foley.Reiniciar();
            checkpoint.Reiniciar();
        }
        anterior = actual;
    }

    void OnDisable()
    {
        if (foley != null) foley.Reiniciar();
        if (checkpoint != null) checkpoint.Reiniciar();
        anterior = Lantern.Estado.Off;
    }
}
