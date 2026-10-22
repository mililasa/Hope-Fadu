using UnityEngine;
using System.Collections;

// En cada hijo "explosion" de un farol/barril. Oculto; al apagar la mano se reproduce una vez.
public class ExplosionesChase : MonoBehaviour
{
    public string estadoExplosion = "explosion";
    public RuntimeAnimatorController controllerExplosion;

    Animator animator;
    SpriteRenderer sr;
    static RuntimeAnimatorController controllerCache;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = gameObject.AddComponent<Animator>();
        if (controllerExplosion == null)
            controllerExplosion = CargarController();
        if (animator.runtimeAnimatorController == null && controllerExplosion != null)
            animator.runtimeAnimatorController = controllerExplosion;
        if (GetComponent<HopeExplosionAudio>() == null)
            gameObject.AddComponent<HopeExplosionAudio>();
        Ocultar();
    }

    static RuntimeAnimatorController CargarController()
    {
        if (controllerCache != null) return controllerCache;
        controllerCache = Resources.Load<RuntimeAnimatorController>("explosion-Sheet_0");
#if UNITY_EDITOR
        if (controllerCache == null)
            controllerCache = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/fondo/explosion-Sheet_0.controller");
#endif
        return controllerCache;
    }

    public static void ReproducirEn(Lantern farol)
    {
        if (farol == null) return;
        ExplosionesChase[] propias = farol.GetComponentsInChildren<ExplosionesChase>(true);
        if (propias != null && propias.Length > 0)
        {
            for (int i = 0; i < propias.Length; i++)
                if (propias[i] != null) propias[i].Reproducir();
            return;
        }

        // Si aún no tienen el script, busca hijos cuyo nombre empiece con "explosion".
        Transform t = farol.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            Transform hijo = t.GetChild(i);
            if (hijo.name.IndexOf("explosion", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            ExplosionesChase ex = hijo.GetComponent<ExplosionesChase>();
            if (ex == null) ex = hijo.gameObject.AddComponent<ExplosionesChase>();
            ex.Reproducir();
        }
    }

    public void Reproducir()
    {
        StopAllCoroutines();
        if (animator == null) animator = GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController == null)
        {
            if (controllerExplosion == null) controllerExplosion = CargarController();
            animator.runtimeAnimatorController = controllerExplosion;
        }
        Mostrar(true);
        if (animator != null)
        {
            animator.enabled = true;
            animator.Play(estadoExplosion, 0, 0f);
            animator.speed = 1f;
            StartCoroutine(OcultarAlTerminar());
        }
        else
            StartCoroutine(OcultarDespues(0.5f));
    }

    IEnumerator OcultarAlTerminar()
    {
        yield return null;
        float dur = 0.5f;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null &&
                    clips[i].name.IndexOf("explosion", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    dur = Mathf.Max(0.15f, clips[i].length);
                    break;
                }
            }
        }
        yield return new WaitForSeconds(dur);
        Ocultar();
    }

    IEnumerator OcultarDespues(float t)
    {
        yield return new WaitForSeconds(t);
        Ocultar();
    }

    void Ocultar()
    {
        Mostrar(false);
        if (animator != null)
            animator.enabled = false;
    }

    void Mostrar(bool visible)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = visible;
    }
}
