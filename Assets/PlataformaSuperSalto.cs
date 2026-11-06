using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// Altura fija del impulso. No se suma al salto de Hope ni se apila entre plataformas.
public class PlataformaSuperSalto : MonoBehaviour
{
    const string EstadoSalto = "plataforma_salto";
    const string EstadoQuieto = "plataforma_salto_idle";

    [Tooltip("Altura del impulso en unidades del mundo.")]
    public float alturaSalto = 4f;

    [HideInInspector]
    [FormerlySerializedAs("multiplicadorAltura")]
    public float multiplicadorAltura = 0f;

    Coroutine vuelta;
    static readonly int HashSalto = Animator.StringToHash(EstadoSalto);

    // Una pasada de plataforma_salto cada vez que Hope se apoya y sale despedida.
    public void Rebotar()
    {
        Animator anim = AnimatorDelDibujo();
        if (anim == null || !anim.HasState(0, HashSalto)) return;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        anim.enabled = true;
        anim.speed = 1f;
        anim.Play(EstadoSalto, 0, 0f);
        anim.Update(0f);
        if (vuelta != null) StopCoroutine(vuelta);
        vuelta = StartCoroutine(VolverAQuieto(anim));
    }

    IEnumerator VolverAQuieto(Animator anim)
    {
        yield return new WaitForSeconds(0.26f);
        vuelta = null;
        if (anim == null) yield break;
        anim.Play(EstadoQuieto, 0, 0f);
        anim.Update(0f);
    }

    Animator AnimatorDelDibujo()
    {
        Animator propio = GetComponent<Animator>();
        if (EsDeSalto(propio)) return propio;
        Animator hijo = GetComponentInChildren<Animator>(true);
        if (EsDeSalto(hijo)) return hijo;
        Animator padre = GetComponentInParent<Animator>();
        if (EsDeSalto(padre)) return padre;

        Animator mejor = null;
        float mejorDist = 1.8f;
        Vector3 aca = transform.position;
        Animator[] todos = FindObjectsOfType<Animator>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            Animator candidato = todos[i];
            if (!EsDeSalto(candidato)) continue;
            float dist = Vector2.Distance(aca, candidato.transform.position);
            if (dist >= mejorDist) continue;
            mejor = candidato;
            mejorDist = dist;
        }
        return mejor;
    }

    static bool EsDeSalto(Animator anim)
    {
        return anim != null
            && anim.runtimeAnimatorController != null
            && anim.runtimeAnimatorController.name.IndexOf("plataforma_salto", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
