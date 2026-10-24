using UnityEngine;
using UnityEngine.Audio;
[CreateAssetMenu(menuName = "Hope/Audio/Plataformas Nivel 2")]
public sealed class HopeLevel2PlatformSettings : ScriptableObject
{
    [System.Serializable] public class Sonidos
    {
        public AudioClip metal, servo, electricidad, inversion;
    }
    public Sonidos horizontal, vertical;
    public AudioMixerGroup salidaMetal, salidaServo, salidaElectricidad;
    [Min(0)] public float distanciaPlena = 2;
    [Min(.1f)] public float distanciaMaxima = 11;
    [Range(0,1)] public float paneoMaximo = .55f;
    [Min(.01f)] public float fadeEntrada = .12f;
    [Min(.01f)] public float fadeSalida = .4f;
}
