using UnityEngine;

[CreateAssetMenu(menuName = "Hope/Configuracion de muerte")]
public sealed class HopeDeathSettings : ScriptableObject
{
    public AudioClip sonidoCaida;
    [Range(0f, 1f)] public float volumenCaida = 0.18f;
    public AudioClip sonidoMano;
    [Range(0f, 1f)] public float volumenMano = 0.18f;
}
