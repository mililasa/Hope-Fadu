using UnityEngine;
[CreateAssetMenu(menuName = "Hope/Anticipacion de la mano")]
public sealed class HopeHandAnticipationSettings : ScriptableObject
{
    public AudioClip sonido;
    [Range(0, 1)] public float volumen = .045f;
    public Vector2 duracion = new Vector2(2, 3);
    public Vector2 silencio = new Vector2(4, 5);
    [Min(.01f)] public float entrada = .45f;
    [Min(.01f)] public float salida = .65f;
}
