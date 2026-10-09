using UnityEngine;
[CreateAssetMenu(menuName = "Hope/Configuracion de faroles")]
public sealed class HopeLanternSettings : ScriptableObject
{
    public AudioClip chispa, encendido, fuego;
    [Range(0,1)] public float volumenChispa = 0.316228f;
    [Range(0,1)] public float volumenEncendido = 0.223872f;
    [Range(0,1)] public float volumenFuego = 0.003467335f;
    [Min(0)] public float retrasoEncendido = 0.1f;
    [Min(0.01f)] public float entradaFuego = 0.15f;
    [Min(0)] public float distanciaPlena = 2.5f;
    [Min(0.1f)] public float alcance = 8f;
}
