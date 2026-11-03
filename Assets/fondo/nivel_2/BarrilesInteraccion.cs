using UnityEngine;

// Barriles de Nivel 2. E junto a cualquiera de los dos lados mete a Hope.
// Adentro, la flecha izquierda o derecha elige por dónde sale.
[DefaultExecutionOrder(-50)]
public class BarrilesInteraccion : MonoBehaviour
{
    public RuntimeAnimatorController controlador;
    public AnimationClip animacion;
    public string nombreEstado = "barril";
    public float margenX = 0.5f;
    public float margenY = 0.45f;

    const float TiempoPausa = 1.5f;
    const float CeldaAncho = 83f;
    const float CeldaAlto = 41f;
    const float PiesPxX = 71.44f;
    const float PiesDesdeAbajo = 3f;

    // Centro del cuerpo en cada cuadro (x desde la izquierda, y desde arriba).
    // x < 0: no hay cuerpo, Hope está dentro del barril.
    static readonly Vector2[] centroPx =
    {
        new Vector2(-1f, -1f),
        new Vector2(11.166f, 21.611f),
        new Vector2(12.197f, 22.191f),
        new Vector2(13.071f, 23.154f),
        new Vector2(14.441f, 23.697f),
        new Vector2(15.623f, 23.068f),
        new Vector2(15.597f, 23.129f),
        new Vector2(16.663f, 23.148f),
        new Vector2(17.049f, 23.703f),
        new Vector2(19.995f, 23.386f),
        new Vector2(21.311f, 23.951f),
        new Vector2(22.500f, 23.833f),
        new Vector2(22.846f, 23.538f),
        new Vector2(-1f, -1f),
        new Vector2(60.429f, 26.000f),
        new Vector2(62.283f, 23.348f),
        new Vector2(62.781f, 24.083f),
        new Vector2(63.678f, 23.278f),
        new Vector2(64.219f, 23.829f),
        new Vector2(65.118f, 23.157f),
        new Vector2(65.562f, 21.767f),
        new Vector2(66.695f, 22.051f),
        new Vector2(67.213f, 21.778f),
        new Vector2(69.405f, 22.200f),
        new Vector2(71.166f, 21.611f)
    };

    enum Fase { Quieto, Entrando, Adentro, Saliendo }

    class Barril
    {
        public Transform transform;
        public SpriteRenderer sr;
        public Animator anim;
        public Collider2D col;
        public Fase fase;
        public bool oculto;
        public bool entroPorLaDerecha;
        public bool salirPorLaDerecha;
        public bool bordeApagado;
        public float vistoUltimo = -1f;
    }

    Barril[] barriles;
    Barril activo;
    PlayerController2D hope;
    Transform luz;
    Vector3 luzLocal;
    bool luzGuardada;
    float normPausa = 0.5142857f;

    // Desde el primer E hasta que termina la animación 2 y Hope sale del barril.
    public bool HopeEscondidaEnBarril
    {
        get
        {
            return activo != null &&
                (activo.fase == Fase.Entrando || activo.fase == Fase.Adentro || activo.fase == Fase.Saliendo);
        }
    }

    void Start()
    {
        GameObject jugador = GameObject.Find("Pp");
        if (jugador != null)
        {
            hope = jugador.GetComponent<PlayerController2D>();
            luz = jugador.transform.Find("Oscuridad");
            if (luz != null)
            {
                luzLocal = luz.localPosition;
                luzGuardada = true;
            }
        }

        if (animacion != null && animacion.length > 0.01f)
            normPausa = TiempoPausa / animacion.length;

        int cantidad = transform.childCount;
        var lista = new System.Collections.Generic.List<Barril>();
        for (int i = 0; i < cantidad; i++)
        {
            Transform hijo = transform.GetChild(i);
            SpriteRenderer sr = hijo.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) continue;
            if (sr.sprite.name.IndexOf("barril", System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            Animator anim = hijo.GetComponent<Animator>();
            if (anim == null)
                anim = hijo.gameObject.AddComponent<Animator>();
            anim.runtimeAnimatorController = controlador;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.updateMode = AnimatorUpdateMode.Normal;
            anim.speed = 0f;
            if (controlador != null)
            {
                anim.Play(nombreEstado, 0, 0f);
                anim.Update(0f);
                anim.speed = 0f;
            }

            lista.Add(new Barril
            {
                transform = hijo,
                sr = sr,
                anim = anim,
                col = hijo.GetComponent<Collider2D>(),
                fase = Fase.Quieto
            });
        }
        barriles = lista.ToArray();
    }

    void Update()
    {
        if (hope == null || barriles == null) return;
        if (Time.timeScale < 0.01f) return;

        if (activo == null)
        {
            Barril cerca = BuscarCercano();
            bool puede = cerca != null && !hope.IsLocked && hope.EstaEnSuelo;
            if (puede && Input.GetKeyDown(KeyCode.E))
                Empezar(cerca);
        }
        else if (activo.fase == Fase.Adentro)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                Continuar(activo, false);
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                Continuar(activo, true);
        }

        if (activo != null)
            Avanzar(activo);
    }

    void LateUpdate()
    {
        if (activo == null || !activo.oculto || luz == null) return;
        SeguirLuz(activo);
    }

    public void Cancelar()
    {
        if (barriles != null)
            foreach (var barril in barriles) HopeBarrelHideAudio.Cancelar(barril.transform);
        RestaurarLuz();
        if (hope != null)
        {
            hope.MostrarSpriteBarril();
            hope.UnlockMovement();
        }
        if (barriles == null) return;
        for (int i = 0; i < barriles.Length; i++)
            ReiniciarVisual(barriles[i]);
        activo = null;
    }

    void Empezar(Barril b)
    {
        activo = b;
        b.fase = Fase.Entrando;
        b.oculto = false;
        b.vistoUltimo = -1f;
        b.entroPorLaDerecha = hope.transform.position.x > b.transform.position.x;
        b.sr.flipX = false;
        hope.BloquearParaBarril();
        Reproducir(b, 0f, 1f);
        HopeBarrelHideAudio.Reproducir(b.transform, true);
    }

    void Continuar(Barril b, bool salirALaDerecha)
    {
        b.salirPorLaDerecha = salirALaDerecha;
        b.fase = Fase.Saliendo;
        b.vistoUltimo = -1f;
        b.anim.speed = 1f;
        HopeBarrelHideAudio.Reproducir(b.transform, false);
    }

    void Avanzar(Barril b)
    {
        if (b.anim == null) return;
        AnimatorStateInfo info = b.anim.GetCurrentAnimatorStateInfo(0);
        float nt = info.normalizedTime;
        int frame = FrameDe(b.sr != null ? b.sr.sprite : null);

        if (b.fase == Fase.Entrando)
        {
            if (!b.oculto && frame >= 1)
            {
                hope.OcultarSpriteBarril();
                b.oculto = true;
            }
            if (nt >= normPausa || frame >= 13)
            {
                Reproducir(b, TiempoPausa, 0f);
                b.fase = Fase.Adentro;
            }
        }
        else if (b.fase == Fase.Saliendo)
        {
            bool termino = nt >= 1.02f;
            if (frame >= 24)
            {
                if (b.vistoUltimo < 0f) b.vistoUltimo = Time.time;
                if (Time.time - b.vistoUltimo >= 1f / 12f)
                    termino = true;
            }
            if (termino)
            {
                Terminar(b);
                return;
            }
        }

        AplicarFlip(b);
        AplicarBorde(b);
    }

    // El borde amarillo es del sprite entero. Con Hope dibujada en el sheet
    // se le marca el contorno, así que se apaga solo mientras rueda.
    void AplicarBorde(Barril b)
    {
        if (b.sr == null) return;
        RelieveSprite relieve = b.sr.GetComponent<RelieveSprite>();
        if (relieve == null) return;

        int frame = FrameDe(b.sr.sprite);
        bool conHope = (b.fase == Fase.Entrando || b.fase == Fase.Saliendo) &&
            frame >= 1 && frame != 13;
        if (conHope)
        {
            relieve.usarParametrosPropios = true;
            relieve.usarColorContorno = false;
            relieve.brilloBorde = 0f;
            b.bordeApagado = true;
            return;
        }

        if (!b.bordeApagado) return;
        relieve.usarParametrosPropios = true;
        relieve.usarColorContorno = true;
        relieve.colorContorno = new Color(1f, 0.84f, 0.12f, 1f);
        relieve.brilloBorde = 1.15f;
        b.bordeApagado = false;
    }

    void AplicarFlip(Barril b)
    {
        if (b.sr == null) return;
        int frame = FrameDe(b.sr.sprite);
        bool quieto = frame <= 0 || frame == 13;
        if (quieto)
            b.sr.flipX = false;
        else if (b.fase == Fase.Saliendo)
            b.sr.flipX = !b.salirPorLaDerecha;
        else
            b.sr.flipX = b.entroPorLaDerecha;
    }

    void Terminar(Barril b)
    {
        bool salioALaDerecha = b.salirPorLaDerecha;
        Vector2 pos = PosicionSalida(b, salioALaDerecha);
        RestaurarLuz();
        ReiniciarVisual(b);
        activo = null;
        hope.AparecerTrasBarril(pos, salioALaDerecha);
    }

    void ReiniciarVisual(Barril b)
    {
        if (b == null) return;
        b.fase = Fase.Quieto;
        b.oculto = false;
        b.vistoUltimo = -1f;
        if (b.sr != null) b.sr.flipX = false;
        Reproducir(b, 0f, 0f);
        AplicarBorde(b);
    }

    void Reproducir(Barril b, float tiempo, float velocidad)
    {
        if (b.anim == null || controlador == null) return;
        float largo = animacion != null ? animacion.length : 2.9166665f;
        float norm = largo > 0.01f ? Mathf.Clamp01(tiempo / largo) : 0f;
        b.anim.speed = velocidad > 0f ? velocidad : 1f;
        b.anim.Play(nombreEstado, 0, norm);
        b.anim.Update(0f);
        b.anim.speed = velocidad;
    }

    Vector2 PosicionSalida(Barril b, bool derecha)
    {
        Sprite sprite = b.sr != null ? b.sr.sprite : null;
        float ppu = sprite != null && sprite.pixelsPerUnit > 1f ? sprite.pixelsPerUnit : 32f;
        float pivotX = sprite != null ? sprite.pivot.x : CeldaAncho * 0.5f;
        float pivotY = sprite != null ? sprite.pivot.y : CeldaAlto * 0.5f;
        float localX = (PiesPxX - pivotX) / ppu;
        float localY = (PiesDesdeAbajo - pivotY) / ppu;
        if (!derecha) localX = -localX;

        Vector3 pies = b.transform.TransformPoint(new Vector3(localX, localY, 0f));
        float piesJugador = hope.PiesLocalY() * hope.transform.lossyScale.y;
        Vector2 pos = new Vector2(pies.x, pies.y - piesJugador);

        CapsuleCollider2D cap = hope.GetComponent<CapsuleCollider2D>();
        if (cap != null && b.col != null)
        {
            float mitad = cap.size.x * 0.5f * Mathf.Abs(hope.transform.lossyScale.x);
            float borde = derecha ? b.col.bounds.max.x : b.col.bounds.min.x;
            if (derecha && pos.x - mitad < borde + 0.02f)
                pos.x = borde + mitad + 0.02f;
            else if (!derecha && pos.x + mitad > borde - 0.02f)
                pos.x = borde - mitad - 0.02f;
        }
        return pos;
    }

    void SeguirLuz(Barril b)
    {
        int frame = FrameDe(b.sr != null ? b.sr.sprite : null);
        bool cuerpoALaDerecha = b.fase == Fase.Saliendo ? b.salirPorLaDerecha : b.entroPorLaDerecha;
        bool espejar = b.fase == Fase.Saliendo ? !b.salirPorLaDerecha : b.entroPorLaDerecha;
        Vector3 local = Vector3.zero;
        if (frame >= 0 && frame < centroPx.Length && centroPx[frame].x >= 0f && b.sr != null && b.sr.sprite != null)
        {
            Sprite sprite = b.sr.sprite;
            float ppu = sprite.pixelsPerUnit > 1f ? sprite.pixelsPerUnit : 32f;
            float px = centroPx[frame].x + 0.5f;
            float desdeAbajo = CeldaAlto - (centroPx[frame].y + 0.5f);
            local.x = (px - sprite.pivot.x) / ppu;
            local.y = (desdeAbajo - sprite.pivot.y) / ppu;
            if (espejar) local.x = -local.x;
        }

        Vector3 mundo = b.transform.TransformPoint(local);
        float signo = cuerpoALaDerecha ? 1f : -1f;
        mundo.x += 0.07f * signo;
        mundo.y += 0.09f;
        Vector3 p = luz.position;
        p.x = mundo.x;
        p.y = mundo.y;
        luz.position = p;
    }

    void RestaurarLuz()
    {
        if (!luzGuardada || luz == null) return;
        luz.localPosition = luzLocal;
    }

    Barril BuscarCercano()
    {
        Barril mejor = null;
        float mejorDist = float.MaxValue;
        Vector2 p = hope.transform.position;
        for (int i = 0; i < barriles.Length; i++)
        {
            Barril b = barriles[i];
            if (!EnRango(b, p)) continue;
            float d = ((Vector2)b.transform.position - p).sqrMagnitude;
            if (d < mejorDist)
            {
                mejorDist = d;
                mejor = b;
            }
        }
        return mejor;
    }

    bool EnRango(Barril b, Vector2 jugador)
    {
        float sx = Mathf.Abs(b.transform.lossyScale.x);
        float sy = Mathf.Abs(b.transform.lossyScale.y);
        float medioX = (CeldaAncho / 32f) * 0.5f * sx;
        float medioY = (CeldaAlto / 32f) * 0.5f * sy;
        Vector2 d = jugador - (Vector2)b.transform.position;
        return Mathf.Abs(d.x) <= medioX + margenX && Mathf.Abs(d.y) <= medioY + margenY;
    }

    static int FrameDe(Sprite sprite)
    {
        if (sprite == null) return -1;
        string n = sprite.name;
        int corte = n.LastIndexOf('_');
        if (corte < 0 || corte >= n.Length - 1) return -1;
        int frame;
        if (!int.TryParse(n.Substring(corte + 1), out frame)) return -1;
        return frame;
    }
}
