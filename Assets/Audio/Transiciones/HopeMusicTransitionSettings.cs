using UnityEngine;
[CreateAssetMenu(menuName = "Hope/Audio/Transiciones entre niveles")]
public sealed class HopeMusicTransitionSettings : ScriptableObject
{
    [Min(0)] public float segundosAlEntrar = 3;
    [Min(0)] public float segundosAlEntrarNivel2 = 2;
    [Min(.05f)] public float segundosFundido = 2;
    [Min(.05f)] public float segundosFundidoEntradaNivel2 = 1.5f;
    [Min(0)] public float segundosAnticipacionSalida = 2;
}
