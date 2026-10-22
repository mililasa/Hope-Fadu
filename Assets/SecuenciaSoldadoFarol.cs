using System.Collections;
using UnityEngine;

// Farol (5): al prenderlo, primero soldadomoribundo y despues manomatasoldado encima.
public class SecuenciaSoldadoFarol : MonoBehaviour
{
    public Lantern farol;
    public Animator animatorMoribundo;
    public GameObject mano;
    public Animator animatorMano;
    public SpriteRenderer spriteMoribundo;
    public string estadoMoribundo = "soldadomoribundo";
    public string estadoMano = "manomatasoldado";
    [Tooltip("Cuantas veces se ve soldadomoribundo antes de que entre la mano.")]
    public int vueltasMoribundo = 2;

    bool disparada;
    Sprite spriteQuieto;

    void Awake()
    {
        if (animatorMoribundo == null)
            animatorMoribundo = GetComponent<Animator>();
        if (spriteMoribundo == null)
            spriteMoribundo = GetComponent<SpriteRenderer>();
        if (spriteMoribundo != null)
            spriteQuieto = spriteMoribundo.sprite;
        if (animatorMoribundo != null)
        {
            animatorMoribundo.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animatorMoribundo.enabled = false;
        }
        if (mano != null)
            mano.SetActive(false);
    }

    public void Reiniciar()
    {
        ConservarSiYaPaso();
    }

    // Al morir Hope: si ya corrió, no se vuelve a disparar.
    public void ConservarSiYaPaso()
    {
        if (disparada)
        {
            StopAllCoroutines();
            return;
        }
        StopAllCoroutines();
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        if (spriteMoribundo != null)
        {
            spriteMoribundo.enabled = true;
            if (spriteQuieto != null)
                spriteMoribundo.sprite = spriteQuieto;
        }
        if (animatorMoribundo != null)
        {
            animatorMoribundo.speed = 1f;
            animatorMoribundo.enabled = false;
        }
        if (animatorMano != null)
            animatorMano.speed = 1f;
        if (mano != null)
        {
            SpriteRenderer srMano = mano.GetComponent<SpriteRenderer>();
            if (srMano != null) srMano.enabled = true;
            mano.SetActive(false);
        }
    }

    void Update()
    {
        if (disparada || farol == null) return;
        if (farol.EstadoActual == Lantern.Estado.Off) return;
        disparada = true;
        StartCoroutine(Reproducir());
    }

    IEnumerator Reproducir()
    {
        if (animatorMoribundo != null)
        {
            animatorMoribundo.enabled = true;
            animatorMoribundo.speed = 1f;
            animatorMoribundo.Play(estadoMoribundo, 0, 0f);
            float duracion = Duracion(animatorMoribundo, estadoMoribundo);
            int vueltas = Mathf.Max(1, vueltasMoribundo);
            if (duracion > 0f)
                yield return new WaitForSeconds(duracion * vueltas);
            animatorMoribundo.Play(estadoMoribundo, 0, 0f);
            animatorMoribundo.Update(0f);
        }

        if (mano != null)
            mano.SetActive(true);
        if (animatorMano != null)
        {
            animatorMano.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animatorMano.speed = 1f;
            animatorMano.Play(estadoMano, 0, 0f);
            animatorMano.Update(0f);
        }

        // El primer cuadro de la mano es el mismo soldado. Se apaga el de abajo
        // para que, cuando la mano se lo lleva, no vuelva a aparecer.
        if (spriteMoribundo != null)
            spriteMoribundo.enabled = false;
        if (animatorMoribundo != null)
            animatorMoribundo.enabled = false;

        float manoDuracion = animatorMano != null ? Duracion(animatorMano, estadoMano) : 0f;
        if (manoDuracion > 0.05f)
        {
            PlayerController2D pp = FindObjectOfType<PlayerController2D>();
            float t = 0f;
            while (t < manoDuracion)
            {
                if (pp != null) pp.LockMovement(0.3f);
                if (animatorMano != null)
                {
                    AnimatorStateInfo info = animatorMano.GetCurrentAnimatorStateInfo(0);
                    if (info.normalizedTime >= 0.97f)
                        break;
                }
                t += Time.deltaTime;
                yield return null;
            }
            if (animatorMano != null)
                animatorMano.speed = 0f;
            if (pp != null) pp.UnlockMovementYSoltarAnim();
        }

        // Se oculta antes de que el clip, que esta en loop, vuelva al soldado.
        if (spriteMoribundo != null)
            spriteMoribundo.enabled = false;
        if (mano != null)
        {
            SpriteRenderer srMano = mano.GetComponent<SpriteRenderer>();
            if (srMano != null) srMano.enabled = false;
        }
        gameObject.SetActive(false);
    }

    static float Duracion(Animator animator, string estado)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        if (clips == null || clips.Length == 0) return 0f;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].name == estado)
                return clips[i].length;
        }
        return clips[0] != null ? clips[0].length : 0f;
    }
}
