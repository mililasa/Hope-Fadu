using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Configuracion de audio")]
public sealed class HopeAudioSettings : ScriptableObject
{
    [Header("Mezcla - ajustar fuera de Play para guardar")]
    [Range(0, 1)] public float general = 0.8f;
    [Range(0, 1)] public float musica = 0.12f;
    [Range(0, 1)] public float foleys = 0.3f;
    [Range(0, 1)] public float mecanicas = 0.25f;

    [Header("Musica")]
    public AudioClip pulsoContinuo;
    [Range(0, 1)] public float musicaConFarol = 0.65f;
    [Min(0)] public float segundosAlivio = 2.5f;

    [Header("Personaje")]
    public AudioClip[] pasos;
    public AudioClip salto;
    public AudioClip apoyo;
    public AudioClip[] rebotes;
    [Min(0.1f)] public float distanciaEntrePasos = 1.15f;
    [Range(0, 1)] public float volumenPaso = 0.65f;
    [Range(0, 1)] public float volumenSalto = 0.3f;
    [Range(0, 1)] public float volumenApoyo = 0.7f;
    [Range(0, 1)] public float volumenRebote = 0.8f;

    [Header("Faroles")]
    public AudioClip chispa;
    public AudioClip encendido;
    public AudioClip llama;
    public AudioClip motivoLuz;
    [Range(0, 1)] public float volumenChispa = 0.7f;
    [Range(0, 1)] public float volumenEncendido = 0.65f;
    [Range(0, 1)] public float volumenLlama = 0.12f;
    [Range(0, 1)] public float volumenMotivo = 0.35f;
    [Min(0.1f)] public float distanciaFarol = 8;
}
