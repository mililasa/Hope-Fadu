using UnityEngine;
using System.Collections;

// pilacadaver: apagado = pila_cadaver 1_0; al Farol (4) reproduce cadaveres y queda en 1_9.
[RequireComponent(typeof(SpriteRenderer))]
public class CadaverChase : MonoBehaviour
{
    public Sprite spriteApagado;
    public Sprite spriteFinal;
    public string estadoAnim = "cadaveres";
    public RuntimeAnimatorController controllerCadaver;

    SpriteRenderer sr;
    Animator animator;
    bool yaDesperto;
    bool despertando;

    public bool YaDesperto => yaDesperto;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = gameObject.AddComponent<Animator>();
        CargarAssetsSiFaltan();
        if (!yaDesperto)
            AplicarApagado();
    }

    void CargarAssetsSiFaltan()
    {
#if UNITY_EDITOR
        if (controllerCadaver == null)
            controllerCadaver = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/fondo/pila_cadaver 1_1.controller");
        if (spriteApagado == null || spriteFinal == null)
        {
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/fondo/pila_cadaver 1.png");
            for (int i = 0; i < assets.Length; i++)
            {
                Sprite s = assets[i] as Sprite;
                if (s == null) continue;
                if (spriteApagado == null && s.name == "pila_cadaver 1_0")
                    spriteApagado = s;
                if (spriteFinal == null && s.name == "pila_cadaver 1_9")
                    spriteFinal = s;
            }
        }
#endif
        if (controllerCadaver == null && animator != null && animator.runtimeAnimatorController != null)
            controllerCadaver = animator.runtimeAnimatorController;

        // Fallback: sprites del mismo atlas que ya tiene el SpriteRenderer.
        if ((spriteApagado == null || spriteFinal == null) && sr != null && sr.sprite != null)
        {
            string path = null;
#if UNITY_EDITOR
            path = UnityEditor.AssetDatabase.GetAssetPath(sr.sprite);
            if (!string.IsNullOrEmpty(path))
            {
                Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                for (int i = 0; i < assets.Length; i++)
                {
                    Sprite s = assets[i] as Sprite;
                    if (s == null) continue;
                    if (spriteApagado == null && s.name.EndsWith("_0"))
                        spriteApagado = s;
                    if (spriteFinal == null && (s.name == "pila_cadaver 1_9" || s.name.EndsWith("_9")))
                        spriteFinal = s;
                }
            }
#endif
        }
    }

    public void AplicarApagado()
    {
        if (yaDesperto) return;
        if (animator != null)
            animator.enabled = false;
        if (sr != null && spriteApagado != null)
            sr.sprite = spriteApagado;
    }

    public void Despertar()
    {
        if (yaDesperto || despertando) return;
        despertando = true;
        yaDesperto = true;
        StopAllCoroutines();
        CargarAssetsSiFaltan();

        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        if (animator == null)
            animator = gameObject.AddComponent<Animator>();
        if (controllerCadaver != null)
            animator.runtimeAnimatorController = controllerCadaver;

        if (animator.runtimeAnimatorController == null)
        {
            despertando = false;
            if (sr != null && spriteFinal != null)
                sr.sprite = spriteFinal;
            Debug.LogWarning("Hope: pilacadaver sin AnimatorController; se muestra frame final.", this);
            return;
        }

        animator.enabled = true;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.speed = 1f;
        animator.Play(estadoAnim, 0, 0f);
        animator.Update(0f);
        StartCoroutine(CongelarEnFrameFinal());
    }

    IEnumerator CongelarEnFrameFinal()
    {
        yield return null;
        float dur = 1.58f;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null &&
                    clips[i].name.IndexOf("cadaver", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    dur = Mathf.Max(0.1f, clips[i].length);
                    break;
                }
            }
        }
        yield return new WaitForSeconds(dur);

        // Queda fijo en pila_cadaver 1_9.
        if (animator != null)
        {
            animator.speed = 0f;
            animator.enabled = false;
        }
        if (sr != null)
        {
            if (spriteFinal == null) CargarAssetsSiFaltan();
            if (spriteFinal != null)
                sr.sprite = spriteFinal;
        }
        despertando = false;
    }

    public void ConservarEstadoEnCheckpoint()
    {
        if (!yaDesperto)
        {
            AplicarApagado();
            return;
        }
        StopAllCoroutines();
        despertando = false;
        if (animator != null)
        {
            animator.speed = 0f;
            animator.enabled = false;
        }
        if (sr != null)
        {
            if (spriteFinal == null) CargarAssetsSiFaltan();
            if (spriteFinal != null)
                sr.sprite = spriteFinal;
        }
    }
}
