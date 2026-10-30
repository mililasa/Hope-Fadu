using UnityEngine;
[CreateAssetMenu(menuName = "Hope/Mano apagando el fuego")]
public sealed class HopeHandSizzleSettings : ScriptableObject
{
    public AudioClip sonido;
    [Range(0, 1)] public float volumen = 0.3f;
    [Min(0)] public float retrasoTrasImpacto = 0.08f;
}
