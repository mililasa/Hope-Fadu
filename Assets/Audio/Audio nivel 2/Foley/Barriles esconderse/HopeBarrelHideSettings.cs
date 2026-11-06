using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Hope/Audio/Barriles esconderse")]
public sealed class HopeBarrelHideSettings : ScriptableObject
{
    public AudioClip entrada, salida;
    public AudioMixerGroup salidaFoley;
    [Range(0, 1)] public float volumenEntrada = .55f;
    [Range(0, 1)] public float volumenSalida = .55f;
}
