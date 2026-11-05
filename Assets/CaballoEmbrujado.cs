using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// E cerca del caballo: Hope levanta la luz y el caballito responde.
// 1 a 5: caballito_normal y queda en caballito_0.
// 6 rayado, 7 rajado, 8 por romperse: cada una se reproduce y queda en su primer frame.
// 9 rota y queda en el ultimo frame. Despues no se puede volver a tocar.
public class CaballoEmbrujado : MonoBehaviour
{
    public Animator animator;
    public RuntimeAnimatorController controllerEmbrujado;
    public RuntimeAnimatorController controllerNormal;
    public string estadoEmbrujado = "caballoembrujado";
    public string estadoNormal = "caballonormal";
    public float radioInteraccion = 1.8f;

    public AnimationClip clipNormal;
    public AnimationClip clipRallado;
    public AnimationClip clipRajado;
    public AnimationClip clipPorRomperse;
    public AnimationClip clipRoto;
    public Sprite spriteQuieto;
    public Sprite[] framesNormal;

    public bool TieneReflejoAmarillo { get { return !terminado; } }

    int interacciones;
    bool ocupado;
    bool terminado;
    PlayerController2D jugador;
    SpriteRenderer sprite;
    RelieveSprite reflejo;

    static readonly Color Amarillo = new Color(1f, 0.84f, 0.12f, 1f);

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        AsegurarCapa();
        if (animator == null)
            animator = GetComponent<Animator>();
        CargarClipsSiFaltan();
        if (animator == null) return;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (controllerEmbrujado != null)
            animator.runtimeAnimatorController = controllerEmbrujado;
        animator.speed = 1f;
        animator.Play(estadoEmbrujado, 0, 0f);
    }

    void Start()
    {
        AplicarReflejo(true);
    }

    void Update()
    {
        if (jugador == null)
            jugador = FindObjectOfType<PlayerController2D>();

        if (!PuedeInteractuar()) return;
        if (!HopeControles.Presionada(HopeControles.Accion.Interactuar)) return;
        StartCoroutine(Interactuar());
    }

    bool PuedeInteractuar()
    {
        if (terminado || ocupado) return false;
        if (jugador == null || jugador.IsLocked) return false;
        Vector2 delta = (Vector2)jugador.transform.position - (Vector2)transform.position;
        return delta.sqrMagnitude <= radioInteraccion * radioInteraccion;
    }

    IEnumerator Interactuar()
    {
        ocupado = true;
        interacciones++;
        int paso = interacciones;

        float duracionFarol = DuracionPrendeFarol();
        if (duracionFarol <= 0f) duracionFarol = 0.6f;
        if (jugador != null)
        {
            jugador.LockMovement(duracionFarol);
            jugador.ReproducirPrendeFarol(duracionFarol);
        }

        AnimationClip clip = ClipDelPaso(paso);
        yield return ReproducirEnElSprite(clip, VueltasDelPaso(paso));
        Congelar(paso, clip);

        if (paso >= 9)
        {
            terminado = true;
            AplicarReflejo(false);
        }

        if (jugador != null)
            jugador.UnlockMovementYSoltarAnim();
        ocupado = false;
    }

    AnimationClip ClipDelPaso(int paso)
    {
        if (paso <= 5) return clipNormal;
        if (paso == 6) return clipRallado;
        if (paso == 7) return clipRajado;
        if (paso == 8) return clipPorRomperse;
        return clipRoto;
    }

    int VueltasDelPaso(int paso)
    {
        if (paso <= 5 || paso >= 9) return 1;
        return 2;
    }

    void AsegurarCapa()
    {
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) return;
        sprite.sortingLayerName = "Default";
        sprite.enabled = true;
    }

    // Un frame cada 1/12 s. caballito_normal y la rota van una vez;
    // rayado, rajado y por romperse, dos.
    const float PasoFrame = 1f / 12f;

    IEnumerator ReproducirEnElSprite(AnimationClip clip, int vueltas)
    {
        if (clip == null) yield break;
        if (animator != null)
            animator.enabled = false;

        if (clip == clipNormal)
        {
            yield return ReproducirFrames(FramesDelNormal(), 1);
            yield break;
        }

        List<float> ciclo = CicloDeSprite(clip);
        HopeCaballitoAudio audio = GetComponent<HopeCaballitoAudio>();
        for (int vuelta = 0; vuelta < vueltas; vuelta++)
        {
            for (int i = 0; i < ciclo.Count; i++)
            {
                clip.SampleAnimation(gameObject, ciclo[i]);
                AsegurarCapa();
                if (audio != null)
                    audio.AvanzarSonido(i * PasoFrame, (i + 1) * PasoFrame);
                yield return new WaitForSeconds(PasoFrame);
            }
        }
    }

    Sprite[] FramesDelNormal()
    {
        CargarFramesNormales();
        if (framesNormal == null || framesNormal.Length == 0) return null;
        return framesNormal;
    }

    IEnumerator ReproducirFrames(Sprite[] frames, int vueltas)
    {
        if (frames == null || frames.Length == 0) yield break;
        HopeCaballitoAudio audio = GetComponent<HopeCaballitoAudio>();
        for (int vuelta = 0; vuelta < vueltas; vuelta++)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null || sprite == null) continue;
                sprite.sprite = frames[i];
                AsegurarCapa();
                if (audio != null)
                    audio.AvanzarSonido(i * PasoFrame, (i + 1) * PasoFrame);
                yield return new WaitForSeconds(PasoFrame);
            }
        }
    }

    List<float> CicloDeSprite(AnimationClip clip)
    {
        List<float> tiempos = new List<float>();
        List<Sprite> sprites = new List<Sprite>();
        Sprite anterior = null;
        float fin = Mathf.Max(0f, clip.length);
        const float sonda = 1f / 60f;
        for (float t = 0f; t < fin; t += sonda)
        {
            clip.SampleAnimation(gameObject, t);
            Sprite actual = sprite != null ? sprite.sprite : null;
            if (actual == null || actual == anterior) continue;
            tiempos.Add(t);
            sprites.Add(actual);
            anterior = actual;
        }
        if (tiempos.Count == 0)
        {
            tiempos.Add(0f);
            return tiempos;
        }

        int corte = CorteDeCiclo(sprites);
        if (corte > 0 && corte < tiempos.Count)
            tiempos.RemoveRange(corte, tiempos.Count - corte);
        return tiempos;
    }

    // Si el clip repite un tramo, el ciclo es lo que hay antes de esa copia.
    int CorteDeCiclo(List<Sprite> sprites)
    {
        int n = sprites.Count;
        int mejor = n;
        for (int p = 2; p <= n / 2; p++)
        {
            for (int a = 0; a + 2 * p <= n; a++)
            {
                bool igual = true;
                for (int i = 0; i < p; i++)
                {
                    if (sprites[a + i] != sprites[a + p + i])
                    {
                        igual = false;
                        break;
                    }
                }
                if (!igual) continue;
                int fin = a + p;
                if (fin < mejor) mejor = fin;
            }
        }
        return mejor;
    }

    void Congelar(int paso, AnimationClip clip)
    {
        if (animator != null)
        {
            animator.speed = 0f;
            animator.enabled = false;
        }
        AsegurarCapa();
        if (sprite == null) return;
        Sprite anterior = sprite.sprite;

        if (paso <= 5)
        {
            if (spriteQuieto != null)
                sprite.sprite = spriteQuieto;
            else if (clip != null)
                clip.SampleAnimation(gameObject, 0f);
            return;
        }

        if (clip == null)
        {
            if (sprite.sprite == null) sprite.sprite = anterior;
            return;
        }
        float tiempo = paso >= 9 ? Mathf.Max(0f, clip.length - 0.02f) : 0f;
        clip.SampleAnimation(gameObject, tiempo);
        if (sprite.sprite == null) sprite.sprite = anterior;
        AsegurarCapa();
    }

    void AplicarReflejo(bool amarillo)
    {
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) return;
        LuzRelieveEntorno luz = FindObjectOfType<LuzRelieveEntorno>();
        if (luz != null)
            luz.AplicarA(sprite);
        reflejo = GetComponent<RelieveSprite>();
        if (reflejo == null)
            reflejo = gameObject.AddComponent<RelieveSprite>();
        reflejo.recibirLuz = true;
        reflejo.usarParametrosPropios = true;
        reflejo.usarColorContorno = amarillo;
        reflejo.colorContorno = Amarillo;
        reflejo.brilloBorde = amarillo ? 1.15f : 0f;
        reflejo.grosorContorno = amarillo ? 1.8f : 1f;
        reflejo.umbralContorno = 0.035f;
        reflejo.intensidadRelieve = amarillo ? 1f : 0.4f;
    }

    public void Reiniciar()
    {
        ConservarSiYaPaso();
    }

    public void ConservarSiYaPaso()
    {
        StopAllCoroutines();
        ocupado = false;
        if (interacciones <= 0)
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator == null) return;
            animator.enabled = true;
            animator.speed = 1f;
            if (controllerEmbrujado != null)
                animator.runtimeAnimatorController = controllerEmbrujado;
            animator.Play(estadoEmbrujado, 0, 0f);
            return;
        }

        Congelar(interacciones, ClipDelPaso(interacciones));
        if (terminado)
            AplicarReflejo(false);
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

    void CargarClipsSiFaltan()
    {
#if UNITY_EDITOR
        if (clipNormal == null)
            clipNormal = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/fondo/caballito_normal.anim");
        if (clipRallado == null)
            clipRallado = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/fondo/caballorallado.anim");
        if (clipRajado == null)
            clipRajado = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/fondo/caballorajado.anim");
        if (clipPorRomperse == null)
            clipPorRomperse = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/fondo/caballoporromperse.anim");
        if (clipRoto == null)
            clipRoto = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/fondo/caballoroto.anim");
        if (spriteQuieto == null)
        {
            Object[] partes = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/fondo/caballito.png");
            for (int i = 0; i < partes.Length; i++)
            {
                Sprite candidato = partes[i] as Sprite;
                if (candidato != null && candidato.name == "caballito_0")
                {
                    spriteQuieto = candidato;
                    break;
                }
            }
        }
        CargarFramesNormales();
#endif
    }

    void CargarFramesNormales()
    {
#if UNITY_EDITOR
        if (framesNormal != null && framesNormal.Length >= 8 && framesNormal[0] != null && framesNormal[7] != null)
            return;
        Object[] partes = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/fondo/caballito.png");
        Sprite[] orden = new Sprite[8];
        for (int i = 0; i < partes.Length; i++)
        {
            Sprite candidato = partes[i] as Sprite;
            if (candidato == null) continue;
            for (int n = 0; n < orden.Length; n++)
            {
                if (candidato.name == "caballito_" + n)
                    orden[n] = candidato;
            }
        }
        if (orden[0] != null)
            framesNormal = orden;
#endif
    }
}
