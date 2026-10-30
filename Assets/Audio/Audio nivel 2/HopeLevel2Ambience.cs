using UnityEngine;
using UnityEngine.SceneManagement;

// Ambiente 1 intermitente; ambientes 2-5 en loop. Mezcla controlada por los faders.
public sealed class HopeLevel2Ambience : MonoBehaviour
{
    public HopeLevel2AmbienceSettings ajustes;
    AudioSource[] fuentes;
    bool pausado;
    bool ambienteUnoEnCurso;
    float esperaAmbienteUno;
    float duracionAmbienteUno;
    float tiempoAmbienteUno;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preparar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "Nivel_2") return;
        foreach (GameObject raiz in escena.GetRootGameObjects())
            if (raiz.GetComponentInChildren<HopeLevel2Ambience>(true) != null) return;
        var config = Resources.Load<HopeLevel2AmbienceSettings>("HopeLevel2AmbienceSettings");
        if (config == null) { Debug.LogError("Hope: falta la configuración de ambientes de nivel 2."); return; }
        var objeto = new GameObject("Audio nivel 2 - Ambientes");
        SceneManager.MoveGameObjectToScene(objeto, escena);
        objeto.AddComponent<HopeLevel2Ambience>().ajustes = config;
    }

    void Start()
    {
        if (ajustes == null || ajustes.clips == null || ajustes.salidas == null ||
            ajustes.clips.Length != 5 || ajustes.salidas.Length != 5)
        {
            Debug.LogError("Hope: ambientes de nivel 2 requiere cinco clips y cinco salidas.", this);
            enabled = false;
            return;
        }
        for (int i = 0; i < 5; i++)
            if (ajustes.clips[i] == null || ajustes.salidas[i] == null)
            {
                Debug.LogError("Hope: falta un clip o un canal de ambientes de nivel 2.", this);
                enabled = false;
                return;
            }
        fuentes = new AudioSource[5];
        double inicio = AudioSettings.dspTime + .1;
        esperaAmbienteUno = Random.Range(5f, 15f);
        for (int i = 0; i < fuentes.Length; i++)
        {
            var objeto = new GameObject("Ambiente " + (i + 1) + (i == 0 ? " - apariciones aleatorias" : " - loop completo"));
            objeto.transform.SetParent(transform, false);
            var fuente = objeto.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0;
            fuente.volume = 1;
            fuente.pitch = 1;
            fuente.panStereo = 0;
            fuente.outputAudioMixerGroup = ajustes.salidas[i];
            fuente.loop = i != 0;
            fuente.clip = ajustes.clips[i];
            if (i != 0) fuente.PlayScheduled(inicio);
            fuentes[i] = fuente;
        }
    }

    void Update()
    {
        if (fuentes == null) return;
        bool pausa = Time.timeScale == 0;
        if (pausa != pausado)
        {
            foreach (var fuente in fuentes)
                if (pausa) fuente.Pause(); else fuente.UnPause();
            pausado = pausa;
        }
        if (pausa || AudioListener.pause) return;

        // Cada aparición incluye una entrada de 30 ms y una salida de 150 ms.
        // La duración aleatoria es total; la espera empieza después de apagar la fuente.
        if (ambienteUnoEnCurso)
        {
            tiempoAmbienteUno += Time.deltaTime;
            float entrada = Mathf.Clamp01(tiempoAmbienteUno / .03f);
            float salida = Mathf.Clamp01((duracionAmbienteUno - tiempoAmbienteUno) / .15f);
            fuentes[0].volume = Mathf.Min(entrada, salida);
            if (tiempoAmbienteUno >= duracionAmbienteUno || !fuentes[0].isPlaying)
            {
                fuentes[0].Stop();
                fuentes[0].volume = 0f;
                esperaAmbienteUno = Random.Range(5f, 15f);
                ambienteUnoEnCurso = false;
            }
        }
        else
        {
            esperaAmbienteUno -= Time.deltaTime;
            if (esperaAmbienteUno <= 0f)
            {
                duracionAmbienteUno = Random.Range(2f, 5f);
                tiempoAmbienteUno = 0f;
                fuentes[0].volume = 0f;
                // Elegir un fragmento completo que quepa dentro del WAV.
                float ultimoInicio = Mathf.Max(0f, fuentes[0].clip.length - duracionAmbienteUno);
                fuentes[0].time = Random.Range(0f, ultimoInicio);
                fuentes[0].Play();
                ambienteUnoEnCurso = true;
            }
        }
    }
}
