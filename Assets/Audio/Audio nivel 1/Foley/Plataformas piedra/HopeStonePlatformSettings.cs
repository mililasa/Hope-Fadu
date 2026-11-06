using UnityEngine;
[CreateAssetMenu(menuName = "Hope/Plataformas de piedra nivel 1")]
public sealed class HopeStonePlatformSettings : ScriptableObject
{
    public AudioClip roce;
    public AudioClip[] contactos;
    [Range(0, 1)] public float volumenRoce = .5f;
    [Range(0, 1)] public float volumenContacto = .35f;
    [Min(0)] public float distanciaPlena = 2;
    [Min(.1f)] public float distanciaSilencio = 9;
}
