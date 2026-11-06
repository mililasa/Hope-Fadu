using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public sealed class HopeEnemyAudioLayer
{
    public bool habilitada = true;
    public AudioClip[] clips;
    public AudioMixerGroup salida;
    [Range(0, 1)] public float volumen = .65f;
    [Min(0)] public float distanciaPlena = 3;
    [Min(.1f)] public float distanciaMaxima = 12;
    [Min(.01f)] public float entrada = .12f;
    [Min(.01f)] public float salidaFade = .6f;
    public bool soloAlDetectar;
    public bool soloEnMovimiento;
}

[CreateAssetMenu(menuName = "Hope/Audio/Enemigo gigante nivel 2")]
public sealed class HopeEnemyAudioSettings : ScriptableObject
{
    public HopeEnemyAudioLayer pisadas = new HopeEnemyAudioLayer();
    public HopeEnemyAudioLayer respiracion = new HopeEnemyAudioLayer();
    public HopeEnemyAudioLayer crujidos = new HopeEnemyAudioLayer();
    public HopeEnemyAudioLayer zumbido = new HopeEnemyAudioLayer();
    [Header("Ritmo de movimiento")]
    [Min(.1f)] public float distanciaEntrePasos = 1.95f;
    [Tooltip("Activar cuando la caminata tenga eventos que llamen PasoDeAnimacion. Desactiva los pasos por distancia.")]
    public bool usarEventosDeAnimacion;
    [Header("Respiracion")]
    [Min(.1f)] public float intervaloRespiracionMin = 2.4f;
    [Min(.1f)] public float intervaloRespiracionMax = 3.3f;
    [Min(0)] public float primeraRespiracionMin = .5f;
    [Min(0)] public float primeraRespiracionMax = 1.4f;
    [Range(0, 1)] public float panoramaMaximo = .35f;
}
