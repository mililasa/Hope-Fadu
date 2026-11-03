using UnityEngine;
using UnityEngine.SceneManagement;
[DisallowMultipleComponent]
public sealed class HopeLevel2Music : MonoBehaviour
{
    public HopeLevel2MusicSettings ajustes;
    AudioSource pulso, piano;
    bool pausado;
    int muestraPausa;
    double origenRitmico;
    PlayerController2D jugador;
    Collider2D salidaNivel;
    bool cercaSalida;
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
            if (raiz.GetComponentInChildren<HopeLevel2Music>(true) != null) return;
        var objeto = new GameObject("Audio nivel 2 - Música");
        SceneManager.MoveGameObjectToScene(objeto, escena);
        objeto.AddComponent<HopeLevel2Music>();
    }
    void Start()
    {
        if (ajustes == null) ajustes = Resources.Load<HopeLevel2MusicSettings>("HopeLevel2MusicSettings");
        if (ajustes == null || ajustes.pulso == null || ajustes.piano == null || ajustes.salidaPulso == null || ajustes.salidaPiano == null ||
            ajustes.pulso.samples != ajustes.piano.samples || ajustes.pulso.frequency != ajustes.piano.frequency)
        { Debug.LogError("Hope: faltan clips, salidas o duración coincidente de música de Nivel 2.", this); enabled = false; return; }
        pulso = Crear("Música N2 - Pulso", ajustes.pulso, ajustes.salidaPulso);
        piano = Crear("Música N2 - Piano", ajustes.piano, ajustes.salidaPiano);
        jugador = FindObjectOfType<PlayerController2D>();
        var destino = GameObject.Find("NIVEL3");
        if (destino != null) salidaNivel = destino.GetComponent<Collider2D>();
        pulso.volume = piano.volume = HopeMusicTransition.GananciaFondo(gameObject.scene);
        double inicio = AudioSettings.dspTime + .2;
        origenRitmico = inicio;
        pulso.PlayScheduled(inicio); piano.PlayScheduled(inicio);
    }
    AudioSource Crear(string nombre, AudioClip clip, UnityEngine.Audio.AudioMixerGroup salida)
    {
        var objeto = new GameObject(nombre); objeto.transform.SetParent(transform, false);
        var a = objeto.AddComponent<AudioSource>();
        a.playOnAwake = false; a.loop = true; a.clip = clip; a.volume = 1; a.pitch = 1;
        a.spatialBlend = 0; a.dopplerLevel = 0; a.outputAudioMixerGroup = salida;
        return a;
    }
    void Update()
    {
        if (pulso == null) return;
        bool pausa = Time.timeScale == 0;
        if (pausa == pausado) return;
        if (pausa)
        {
            muestraPausa = pulso.timeSamples;
            pulso.Stop(); piano.Stop();
        }
        else
        {
            pulso.timeSamples = muestraPausa; piano.timeSamples = muestraPausa;
            double inicio = AudioSettings.dspTime + .1;
            origenRitmico = inicio - (double)muestraPausa / pulso.clip.frequency;
            pulso.PlayScheduled(inicio); piano.PlayScheduled(inicio);
        }
        pausado = pausa;
    }
    void LateUpdate()
    {
        if (pulso == null || piano == null) return;
        if (jugador != null && salidaNivel != null && Time.timeScale > 0 && !AudioListener.pause)
        {
            Vector2 punto = jugador.transform.position;
            float distancia = Vector2.Distance(punto, salidaNivel.ClosestPoint(punto));
            float anticipacion = Mathf.Max(.25f, Mathf.Abs(jugador.moveSpeed) * HopeMusicTransition.AnticipacionSalida);
            if (!cercaSalida && distancia <= anticipacion) cercaSalida = true;
            else if (cercaSalida && distancia > anticipacion + 1) cercaSalida = false;
            HopeMusicTransition.SolicitarSalida(gameObject.scene, cercaSalida);
        }
        pulso.volume = piano.volume = HopeMusicTransition.GananciaFondo(gameObject.scene);
    }
    // Grid follows the same DSP clock as the scheduled music, including resume.
    public bool ProximaCorchea(double anticipacion, out double cuando)
    {
        cuando = AudioSettings.dspTime + anticipacion;
        if (pulso == null) return false;
        double pasos = System.Math.Ceiling((cuando - origenRitmico) / .4);
        cuando = origenRitmico + System.Math.Max(0, pasos) * .4;
        return true;
    }
    public double ErrorDeRejilla(double cuando)
    {
        double paso = (cuando - origenRitmico) / .4;
        return System.Math.Abs(paso - System.Math.Round(paso)) * .4;
    }
    public float RelieveRitmico()
    {
        if (pulso == null) return 1;
        double tiempo = System.Math.Max(0, AudioSettings.dspTime - origenRitmico);
        float fase = (float)(tiempo % .4);
        return .72f + .28f * Mathf.Exp(-fase * 12);
    }
    void OnDisable()
    {
        if (pulso != null) pulso.Stop();
        if (piano != null) piano.Stop();
    }
}
