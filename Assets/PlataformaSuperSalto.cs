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

    // Una pasada del trampolín por cada impulso. Si vuelve a rebotar, arranca de nuevo.
    public void Rebotar()
    {
        Animator anim = GetComponent<Animator>();
        if (anim == null || anim.runtimeAnimatorController == null) return;
        if (anim.runtimeAnimatorController.name.IndexOf("plataforma_salto", System.StringComparison.OrdinalIgnoreCase) < 0)
            return;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        anim.enabled = true;
        anim.speed = 1.5f;
        anim.Play("trampolin", 0, 0f);
        anim.Update(0f);
    }
}
