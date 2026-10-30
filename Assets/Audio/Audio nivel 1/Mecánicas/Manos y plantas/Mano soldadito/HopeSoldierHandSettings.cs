using UnityEngine;
[CreateAssetMenu(menuName = "Hope/Mano se lleva al soldadito")]
public sealed class HopeSoldierHandSettings : ScriptableObject
{
    public AudioClip aparicion, motivo, agarre, retirada;
    [Range(0,1)] public float volumenGeneral = .65f;
    [Range(0,1)] public float volumenAparicion = 1, volumenMotivo = 1, volumenAgarre = 1, volumenRetirada = 1;
    [Range(0,1)] public float faseAparicion = .05f, faseMotivo = .108f, faseAgarre = .6f, faseRetirada = .7f;
}
