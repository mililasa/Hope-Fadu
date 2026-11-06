using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class HopeLevel2Drips : MonoBehaviour
{
    public HopeLevel2DripsSettings ajustes;
    AudioSource[] fuentes;
    bool pausado;

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
            if (raiz.GetComponentInChildren<HopeLevel2Drips>(true) != null) return;
        var config = Resources.Load<HopeLevel2DripsSettings>("HopeLevel2DripsSettings");
        if (config == null) { Debug.LogError("Hope: faltan los ajustes de Agua Gotas de nivel 2."); return; }
        var go = new GameObject("Audio nivel 2 - Agua Gotas");
        SceneManager.MoveGameObjectToScene(go, escena);
        go.AddComponent<HopeLevel2Drips>().ajustes = config;
    }

    void Start()
    {
        if (ajustes == null || ajustes.clips == null || ajustes.clips.Length != 4 ||
            ajustes.salidas == null || ajustes.salidas.Length != 4)
        { Debug.LogError("Hope: Agua Gotas requiere cuatro clips y cuatro salidas.", this); enabled = false; return; }
        for (int i = 0; i < 4; i++)
            if (ajustes.clips[i] == null || ajustes.salidas[i] == null)
            { Debug.LogError("Hope: falta un clip o salida de Agua Gotas.", this); enabled = false; return; }
        fuentes = new AudioSource[4];
        double inicio = AudioSettings.dspTime + .1;
        for (int i = 0; i < fuentes.Length; i++)
        {
            var go = new GameObject("Agua gotea " + (i + 1) + " - loop completo");
            go.transform.SetParent(transform, false);
            var fuente = go.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.loop = true;
            fuente.spatialBlend = 0;
            fuente.volume = 1;
            fuente.pitch = 1;
            fuente.panStereo = 0;
            fuente.clip = ajustes.clips[i];
            fuente.outputAudioMixerGroup = ajustes.salidas[i];
            fuente.PlayScheduled(inicio);
            fuentes[i] = fuente;
        }
    }

    void Update()
    {
        if (fuentes == null) return;
        bool pausa = Time.timeScale == 0;
        if (pausa == pausado) return;
        foreach (var fuente in fuentes)
            if (pausa) fuente.Pause(); else fuente.UnPause();
        pausado = pausa;
    }
}
