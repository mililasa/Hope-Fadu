using UnityEngine;
using UnityEngine.Serialization;

// Altura fija del impulso. No se suma al salto de Hope ni se apila entre plataformas.
public class PlataformaSuperSalto : MonoBehaviour
{
    [Tooltip("Altura del impulso en unidades del mundo.")]
    public float alturaSalto = 4f;

    [HideInInspector]
    [FormerlySerializedAs("multiplicadorAltura")]
    public float multiplicadorAltura = 0f;
}
