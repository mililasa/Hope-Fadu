using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Hijo "explosion" de un farol: oculto, y se ve una vez cuando la mano lo apaga.
// "explosion" y "explosion (1)" del puente se disparan cuando Hope pasa cerca.
public class ExplosionesChase : MonoBehaviour
{
    public string estadoExplosion = "explosion";
    public RuntimeAnimatorController controllerExplosion;
    [Tooltip("Si esta tildado, se reproduce cuando Hope pasa cerca y no cuando la mano apaga.")]
    public bool activarAlPasarHope;
    public float distanciaHorizontal = 2.6f;
    public float distanciaVertical = 5f;

    Animator animator;
    SpriteRenderer sr;
    Transform hope;
    bool yaDisparada;
    bool reproduciendo;
    float avancePrevio;
    static RuntimeAnimatorController controllerNivel1;
    static RuntimeAnimatorController controllerCache;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EngancharPuente()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        BuscarEnEscena();
    }

    static void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        BuscarEnEscena();
    }

    static void BuscarEnEscena()
    {
        SpriteRenderer[] todos = Object.FindObjectsOfType<SpriteRenderer>(true);
        for (int i = 0; i < todos.Length; i++)
        {
            SpriteRenderer dibujo = todos[i];
            if (dibujo == null || !DisparaAlPasar(dibujo.transform)) continue;
            if (dibujo.GetComponent<ExplosionesChase>() == null)
                dibujo.gameObject.AddComponent<ExplosionesChase>();
        }
    }

    public static bool DisparaAlPasar(Transform t)
    {
        if (t == null) return false;
        string n = t.name;
        if (n == "explosion" || n == "explosion (1)")
            return true;
        Transform p = t.parent;
        while (p != null)
        {
            if (p.name == "Bridge" &&
                n.IndexOf("explosion", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            p = p.parent;
        }
        return false;
    }

    void Awake()
    {
        if (DisparaAlPasar(transform))
            activarAlPasarHope = true;
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = gameObject.AddComponent<Animator>();
        if (controllerExplosion == null || gameObject.scene.name == "SampleScene")
            controllerExplosion = CargarController();
        if ((animator.runtimeAnimatorController == null || gameObject.scene.name == "SampleScene") && controllerExplosion != null)
            animator.runtimeAnimatorController = controllerExplosion;
        if (HopeLegacyAudioPolicy.Permitir(this) && GetComponent<HopeExplosionAudio>() == null)
            gameObject.AddComponent<HopeExplosionAudio>();
        Ocultar();
    }

    bool PadreEsBarril()
    {
        Transform p = transform.parent;
        while (p != null)
        {
            Lantern farol = p.GetComponent<Lantern>();
            if (farol != null) return farol.EsBarril;
            p = p.parent;
        }
        return false;
    }

    void Update()
    {
        if (yaDisparada || PadreEsBarril() || !activarAlPasarHope) return;
        if (hope == null)
        {
            PlayerController2D pp = FindObjectOfType<PlayerController2D>();
            if (pp != null) hope = pp.transform;
            if (hope == null) return;
        }

        // El pivote del sprite está en una esquina. Si se mide desde ahí, Hope
        // cruza la pintura y la explosión no arranca.
        if (gameObject.scene.name == "SampleScene")
        {
            Vector2 delta = hope.position - transform.position;
            if (Mathf.Abs(delta.x) > distanciaHorizontal || Mathf.Abs(delta.y) > distanciaVertical) return;
        }
        else if (!HopeCerca()) return;

        Reproducir();
    }

    bool HopeCerca()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        Vector2 p = hope.position;
        if (sr != null && sr.sprite != null)
        {
            Bounds b = sr.sprite.bounds;
            Vector3 a = transform.TransformPoint(b.min);
            Vector3 c = transform.TransformPoint(b.max);
            float x0 = Mathf.Min(a.x, c.x) - distanciaHorizontal;
            float x1 = Mathf.Max(a.x, c.x) + distanciaHorizontal;
            float y0 = Mathf.Min(a.y, c.y) - distanciaVertical;
            float y1 = Mathf.Max(a.y, c.y) + distanciaVertical;
            return p.x >= x0 && p.x <= x1 && p.y >= y0 && p.y <= y1;
        }
        Vector2 delta = p - (Vector2)transform.position;
        return Mathf.Abs(delta.x) <= distanciaHorizontal && Mathf.Abs(delta.y) <= distanciaVertical;
    }

    static RuntimeAnimatorController CargarController()
    {
        if (SceneManager.GetActiveScene().name == "SampleScene")
        {
            if (controllerNivel1 == null) controllerNivel1 = Resources.Load<RuntimeAnimatorController>("HopeN1ExplosionBackup");
            return controllerNivel1;
        }
        if (controllerCache != null) return controllerCache;
        controllerCache = Resources.Load<RuntimeAnimatorController>("explosion-Sheet_0");
#if UNITY_EDITOR
        if (controllerCache == null)
            controllerCache = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/fondo/Resources/explosion-Sheet_0.controller");
#endif
        return controllerCache;
    }

    public static void ReproducirEn(Lantern farol)
    {
        if (farol == null || farol.EsBarril) return;
        ExplosionesChase[] propias = farol.GetComponentsInChildren<ExplosionesChase>(true);
        if (propias != null && propias.Length > 0)
        {
            for (int i = 0; i < propias.Length; i++)
            {
                if (propias[i] == null || propias[i].activarAlPasarHope) continue;
                propias[i].Reproducir();
            }
            return;
        }

        // Si aún no tienen el script, busca hijos cuyo nombre empiece con "explosion".
        Transform t = farol.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            Transform hijo = t.GetChild(i);
            if (hijo.name.IndexOf("explosion", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            if (DisparaAlPasar(hijo))
                continue;
            ExplosionesChase ex = hijo.GetComponent<ExplosionesChase>();
            if (ex == null) ex = hijo.gameObject.AddComponent<ExplosionesChase>();
            if (ex.activarAlPasarHope) continue;
            ex.Reproducir();
        }
    }

    public void Reproducir()
    {
        if (yaDisparada || PadreEsBarril()) return;
        yaDisparada = true;
        StopAllCoroutines();
        if (animator == null) animator = GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController == null)
        {
            if (controllerExplosion == null) controllerExplosion = CargarController();
            animator.runtimeAnimatorController = controllerExplosion;
        }
        Mostrar(true);
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            // Si el animator nace apagado, se queda en el primer cuadro.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = true;
            animator.speed = 1f;
            animator.Rebind();
            animator.Play(estadoExplosion, 0, 0f);
            animator.Update(0f);
            if (gameObject.scene.name == "SampleScene")
            {
                reproduciendo = true;
                avancePrevio = 0;
                StartCoroutine(OcultarAlTerminarBackup());
            }
            else StartCoroutine(OcultarDespues(Duracion()));
        }
        else
            StartCoroutine(OcultarDespues(0.5f));
    }

    float Duracion()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].name == estadoExplosion)
                    return Mathf.Max(0.1f, clips[i].length);
            }
        }
        return 0.67f;
    }

    IEnumerator OcultarAlTerminarBackup()
    {
        float tope = Time.time + 2f;
        while (Time.time < tope && reproduciendo)
        {
            yield return null;
            if (animator == null || !animator.enabled) break;
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            float avance = info.normalizedTime;
            if (avance >= 0.98f || avance + 0.02f < avancePrevio)
                break;
            avancePrevio = avance;
        }
        Ocultar();
    }

    IEnumerator OcultarDespues(float t)
    {
        yield return new WaitForSeconds(t);
        Ocultar();
    }

    void Ocultar()
    {
        reproduciendo = false;
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
