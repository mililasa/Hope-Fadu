using UnityEngine;
using UnityEngine.Audio;
[CreateAssetMenu(menuName = "Hope/Audio/Música Nivel 2")]
public sealed class HopeLevel2MusicSettings : ScriptableObject
{
    public AudioClip pulso, piano;
    public AudioMixerGroup salidaPulso, salidaPiano;
}
