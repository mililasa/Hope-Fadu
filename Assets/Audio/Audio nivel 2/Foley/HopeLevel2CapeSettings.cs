using UnityEngine;
using UnityEngine.Audio;
[CreateAssetMenu(menuName = "Hope/Audio/Capa Nivel 2")]
public sealed class HopeLevel2CapeSettings : ScriptableObject
{
    public AudioClip sonido;
    public AudioMixerGroup salida;
    [Range(0,1)] public float volumenCanal = .827f;
    [Range(0,1)] public float volumenFragmento = .817f;
}
