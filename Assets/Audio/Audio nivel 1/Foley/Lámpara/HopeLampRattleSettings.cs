using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Configuracion de lampara")]
public sealed class HopeLampRattleSettings : ScriptableObject
{
    public AudioClip[] sonidos;
    [Range(0f, 1f)] public float volumenCanal = 0.0851138f;
    [Range(0f, 1f)] public float volumenFragmentoMin = 0.056234f;
    [Range(0f, 1f)] public float volumenFragmentoMax = 0.056234f;
    [Min(0.08f)] public float segundosEntreMovimientos = 0.58f;
}
