using System.Collections;
using UnityEngine;

// Caballo embrujado hasta que Hope llega al centro, hace prendefarol y queda normal.
public class CaballoEmbrujado : MonoBehaviour
{
    public Animator animator;
    public RuntimeAnimatorController controllerEmbrujado;
    public RuntimeAnimatorController controllerNormal;
    public string estadoEmbrujado = "caballoembrujado";
    public string estadoNormal = "caballonormal";
    [Tooltip("Distancia al centro del caballo para que Hope lo limpie.")]
    public float radioCentro = 0.5f;

    bool limpio;
    PlayerController2D jugador;

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null) return;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (controllerEmbrujado != null)
            animator.runtimeAnimatorController = controllerEmbrujado;
        animator.Play(estadoEmbrujado, 0, 0f);
    }

    void Update()
    {
        if (limpio) return;
        if (jugador == null)
            jugador = FindObjectOfType<PlayerController2D>();
        if (jugador == null || jugador.IsLocked) return;

        Vector2 delta = (Vector2)jugador.transform.position - (Vector2)transform.position;
        if (delta.sqrMagnitude > radioCentro * radioCentro) return;

        limpio = true;
        StartCoroutine(Limpiar());
    }

    IEnumerator Limpiar()
    {
        float duracion = DuracionPrendeFarol();
        if (duracion <= 0f) duracion = 0.6f;

        if (jugador != null)
        {
            jugador.LockMovement(duracion + 0.2f);
            jugador.ReproducirPrendeFarol(duracion);
        }

        yield return new WaitForSeconds(duracion);

        if (animator != null)
        {
            if (controllerNormal != null)
                animator.runtimeAnimatorController = controllerNormal;
            animator.speed = 1f;
            animator.Play(estadoNormal, 0, 0f);
        }

        float duracionNormal = DuracionController(controllerNormal, estadoNormal);
        if (duracionNormal <= 0f) duracionNormal = 0.6f;
        if (jugador != null)
            jugador.LockMovement(duracionNormal + 0.1f);
        yield return new WaitForSeconds(duracionNormal);

        if (jugador != null)
            jugador.UnlockMovementYSoltarAnim();
    }

    public void Reiniciar()
    {
        ConservarSiYaPaso();
    }

    // Al morir Hope: si ya se limpió, no vuelve a embrujado.
    public void ConservarSiYaPaso()
    {
        if (limpio)
        {
            StopAllCoroutines();
            return;
        }
        StopAllCoroutines();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (animator == null) return;
        animator.speed = 1f;
        if (controllerEmbrujado != null)
            animator.runtimeAnimatorController = controllerEmbrujado;
        animator.Play(estadoEmbrujado, 0, 0f);
    }

    float DuracionPrendeFarol()
    {
        if (jugador == null) return 0f;
        Animator animJugador = jugador.GetComponent<Animator>();
        if (animJugador == null || animJugador.runtimeAnimatorController == null) return 0f;
        AnimationClip[] clips = animJugador.runtimeAnimatorController.animationClips;
        if (clips == null) return 0f;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == jugador.prendeFarolStateName)
                return clips[i].length;
        }
        return 0f;
    }

    static float DuracionController(RuntimeAnimatorController ctrl, string estado)
    {
        if (ctrl == null) return 0f;
        AnimationClip[] clips = ctrl.animationClips;
        if (clips == null) return 0f;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == estado)
                return clips[i].length;
        }
        return clips.Length > 0 && clips[0] != null ? clips[0].length : 0f;
    }
}
