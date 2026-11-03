using UnityEngine;
using UnityEngine.SceneManagement;

// Musical states follow the existing lighthouse door and final lantern.
[DefaultExecutionOrder(1200)]
[DisallowMultipleComponent]
public sealed class HopeLevel3Music : MonoBehaviour
{
    public HopeLevel3MusicSettings ajustes;
    AudioSource exterior, pulso, marimba, cierre;
    HopeMusicSettings nivel1;
    FaroFez faro;
    float interior, fondoFinal = 1;
    bool iniciadoInterior, terminado, pausado;
    int muestraExterior, muestraInterior;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Preparar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }
    static void AlCargar(Scene escena, LoadSceneMode modo)
    {
        if (escena.name != "Nivel_3") return;
        foreach (var raiz in escena.GetRootGameObjects())
            if (raiz.GetComponentInChildren<HopeLevel3Music>(true) != null) return;
        var go = new GameObject("Audio nivel 3 - Musica del faro");
        SceneManager.MoveGameObjectToScene(go, escena);
        go.AddComponent<HopeLevel3Music>();
    }
    void Start()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeLevel3MusicSettings>("HopeLevel3MusicSettings");
        nivel1 = Resources.Load<HopeMusicSettings>("HopeMusicSettings");
        faro = FindObjectOfType<FaroFez>();
        if (ajustes == null || nivel1 == null || nivel1.musicaMain == null || faro == null ||
            ajustes.pulsoInterior == null || ajustes.marimbaInterior == null || ajustes.cierreLuz == null ||
            ajustes.pulsoInterior.samples != ajustes.marimbaInterior.samples ||
            ajustes.pulsoInterior.frequency != ajustes.marimbaInterior.frequency)
        { Debug.LogError("Hope: falta configuracion de musica del faro o clips alineados.", this); enabled = false; return; }
        exterior = Crear("Exterior - Pulso nivel 1", nivel1.musicaMain, true);
        pulso = Crear("Interior - Pulso version 2", ajustes.pulsoInterior, true);
        marimba = Crear("Interior - Marimba version 2", ajustes.marimbaInterior, true);
        cierre = Crear("Final - Luz y marimba", ajustes.cierreLuz, false);
        exterior.Play();
    }
    AudioSource Crear(string nombre, AudioClip clip, bool loop)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        var a = go.AddComponent<AudioSource>();
        a.playOnAwake = false; a.loop = loop; a.clip = clip;
        a.spatialBlend = 0; a.dopplerLevel = 0; a.volume = 0;
        HopeAudioRouting.Asignar(a, HopeAudioBus.Musica);
        return a;
    }
    void Update()
    {
        if (exterior == null || faro == null) return;
        bool pausa = Time.timeScale == 0 || AudioListener.pause;
        if (pausa != pausado)
        {
            if (pausa)
            {
                muestraExterior = exterior.timeSamples;
                muestraInterior = pulso.timeSamples;
                exterior.Stop(); pulso.Stop(); marimba.Stop(); cierre.Pause();
            }
            else
            {
                double cuando = AudioSettings.dspTime + .1;
                exterior.timeSamples = muestraExterior; exterior.PlayScheduled(cuando);
                if (iniciadoInterior)
                {
                    pulso.timeSamples = marimba.timeSamples = muestraInterior;
                    pulso.PlayScheduled(cuando); marimba.PlayScheduled(cuando);
                }
                cierre.UnPause();
            }
            pausado = pausa;
        }
        if (pausa) return;
        if (!iniciadoInterior && faro.MezclaInteriorAudio > .001f)
        {
            // Start both approved stems together on the first actual entry.
            double cuando = AudioSettings.dspTime + .08;
            pulso.PlayScheduled(cuando); marimba.PlayScheduled(cuando);
            iniciadoInterior = true;
        }
        // After the first entry, keep V2 playing across the final outdoor section.
        interior = Mathf.MoveTowards(interior, iniciadoInterior ? 1f : 0f,
            Time.deltaTime / Mathf.Max(.05f, ajustes.fundidoPuerta));
        if (!terminado && faro.farolFinal != null && faro.farolFinal.EstadoActual == Lantern.Estado.Lit)
        {
            terminado = true;
            cierre.volume = ajustes.volumenGeneral * ajustes.volumenCierre;
            cierre.Play();
        }
        if (terminado) fondoFinal = Mathf.MoveTowards(fondoFinal, 0,
            Time.deltaTime / Mathf.Max(.01f, ajustes.fundidoFinal));
    }
    void LateUpdate()
    {
        if (exterior == null) return;
        float puente = HopeMusicTransition.GananciaFondo(gameObject.scene);
        exterior.volume = nivel1.volumenMain * (1 - interior) * fondoFinal * puente;
        pulso.volume = ajustes.volumenGeneral * ajustes.volumenPulso * interior * fondoFinal * puente;
        marimba.volume = ajustes.volumenGeneral * ajustes.volumenMarimba * interior * fondoFinal * puente;
        cierre.volume = ajustes.volumenGeneral * ajustes.volumenCierre;
    }
    void OnDisable()
    {
        if (exterior != null) exterior.Stop();
        if (pulso != null) pulso.Stop();
        if (marimba != null) marimba.Stop();
        if (cierre != null) cierre.Stop();
    }
}
