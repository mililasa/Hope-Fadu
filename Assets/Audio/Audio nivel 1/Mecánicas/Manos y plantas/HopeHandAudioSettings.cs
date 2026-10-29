using UnityEngine;
[CreateAssetMenu(menuName = "Hope/Audio de la mano")]
public sealed class HopeHandAudioSettings : ScriptableObject
{
    public AudioClip motivo;
    [Range(0,1)] public float volumen = 0.30902954f;
    [Min(0.01f)] public float entrada = 0.28f;
    [Min(0.01f)] public float salida = 0.85f;
    [Range(0.01f,1f)] public float fraccionPantallaPlena = 0.2f;
}
