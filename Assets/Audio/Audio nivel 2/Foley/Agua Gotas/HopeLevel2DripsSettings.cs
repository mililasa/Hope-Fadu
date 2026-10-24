using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Hope/Audio/Gotas nivel 2")]
public sealed class HopeLevel2DripsSettings : ScriptableObject
{
    public AudioClip[] clips;
    public AudioMixerGroup[] salidas;
    public AudioMixerGroup faroles;
}
