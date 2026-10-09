using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Configuracion de pasos")]
public sealed class HopeFootstepSettings : ScriptableObject
{
    public AudioClip[] pasos;
    [Range(0f, 1f)] public float volumenCanal = 0.12589254f;
    [Range(0f, 1f)] public float volumenFragmentoMin = 0.194984f;
    [Range(0f, 1f)] public float volumenFragmentoMax = 0.25704f;
    [Min(0.08f)] public float segundosEntrePasos = 0.29f;
}
