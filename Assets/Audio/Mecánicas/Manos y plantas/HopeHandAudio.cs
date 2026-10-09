using UnityEngine;

// Evaluar despues de CameraFollow.LateUpdate para usar el encuadre actualizado.
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public sealed class HopeHandAudio : MonoBehaviour
{
    public HopeHandAudioSettings ajustes;
    HandChaser mano;
    SpriteRenderer dibujo;
    Camera camara;
    AudioSource fuente;
    float nivel;
    public float NivelMusical => isActiveAndEnabled && fuente != null && fuente.isPlaying ? nivel : 0f;

    void Start()
    {
        mano = GetComponent<HandChaser>();
        dibujo = GetComponent<SpriteRenderer>();
        camara = Camera.main;
        if (ajustes == null) ajustes = Resources.Load<HopeHandAudioSettings>("HopeHandAudioSettings");
        if (mano == null || dibujo == null || ajustes == null || ajustes.motivo == null)
        {
            Debug.LogWarning("Hope: faltan referencias del motivo de la mano.", this);
            enabled = false;
            return;
        }
        var objeto = new GameObject("M - Manos y plantas (canal 22)");
        objeto.transform.SetParent(transform, false);
        fuente = objeto.AddComponent<AudioSource>();
        HopeAudioRouting.Asignar(fuente, HopeAudioBus.Mecanicas);
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.spatialBlend = 0f;
        fuente.dopplerLevel = 0f;
        fuente.clip = ajustes.motivo;
        fuente.volume = 0f;
    }

    void LateUpdate()
    {
        if (fuente == null || Time.deltaTime <= 0f) return;
        if (camara == null) camara = Camera.main;
        // La musica de persecucion suena mientras dura la persecucion de la ChaseZone,
        // aunque la mano quede escondida en la oscuridad o fuera de cuadro.
        float objetivo = mano.enabled && mano.PersecucionAudible ? 1f : 0f;
        float duracion = objetivo > nivel ? ajustes.entrada : ajustes.salida;
        nivel = Mathf.MoveTowards(nivel, objetivo, Time.deltaTime / Mathf.Max(0.01f, duracion));
        if (objetivo > 0f && !fuente.isPlaying) fuente.Play();
        fuente.volume = ajustes.volumen * nivel;
        if (nivel <= 0f && fuente.isPlaying) fuente.Stop();
    }

    public void Reiniciar()
    {
        nivel = 0f;
        if (fuente != null) { fuente.Stop(); fuente.volume = 0f; }
    }
    void OnDisable() { Reiniciar(); }
}
