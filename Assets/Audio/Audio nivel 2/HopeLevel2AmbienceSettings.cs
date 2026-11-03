using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Hope/Audio/Ambientes nivel 2")]
public sealed class HopeLevel2AmbienceSettings : ScriptableObject
{
    [Header("Mezclar en Audio Mixer: Mixer ambientes nivel 2")]
    [Tooltip("Los cinco ambientes completos con crossfade breve incorporado en cada WAV.")]
    public AudioClip[] clips;
    [Tooltip("Un canal por ambiente. Los faders del mixer controlan el volumen y guardan la mezcla.")]
    public AudioMixerGroup[] salidas;
}
