using UnityEngine;

// La e del nivel 1 y la E del nivel 2 se quedan en el dibujo quieto
// hasta que Hope se acerca. Ahí corre eanim una sola vez y queda en el último cuadro.
[RequireComponent(typeof(SpriteRenderer))]
public class LetraEAparece : MonoBehaviour
{
    public RuntimeAnimatorController controlador;
    public string nombreEstado = "eanim";
    [Tooltip("Distancia a la que Hope dispara la aparición.")]
    public float distancia = 4.5f;

    Animator anim;
    Transform hope;
    bool yaSalio;

    void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = gameObject.AddComponent<Animator>();
        if (controlador != null)
            anim.runtimeAnimatorController = controlador;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        anim.updateMode = AnimatorUpdateMode.Normal;
        anim.speed = 0f;
        anim.enabled = false;
    }

    void Start()
    {
        GameObject jugador = GameObject.Find("Pp");
        if (jugador != null)
            hope = jugador.transform;
    }

    void Update()
    {
        if (hope == null || anim == null) return;
        if (Time.timeScale < 0.01f) return;

        if (!yaSalio)
        {
            Vector2 delta = hope.position - transform.position;
            if (delta.sqrMagnitude > distancia * distancia) return;

            yaSalio = true;
            anim.enabled = true;
            anim.speed = 1f;
            if (anim.runtimeAnimatorController != null)
                anim.Play(nombreEstado, 0, 0f);
            return;
        }

        if (!anim.enabled) return;
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName(nombreEstado)) return;
        if (info.normalizedTime < 1f) return;
        anim.speed = 0f;
    }
}
