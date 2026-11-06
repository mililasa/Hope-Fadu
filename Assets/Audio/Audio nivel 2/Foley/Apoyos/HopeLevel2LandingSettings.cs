using UnityEngine;
using UnityEngine.Audio;
[CreateAssetMenu(menuName = "Hope/Audio/Apoyos metal nivel 2")]
public sealed class HopeLevel2LandingSettings : ScriptableObject
{
    public AudioClip[] apoyos;
    public AudioMixerGroup salida;
    public bool habilitado = true;
    [Range(0, 1)] public float volumen = .55f;
    [Min(.01f)] public float minimoTiempoEnAire = .10f;
    [Min(0)] public float ignorarAlReiniciar = .2f;
}
