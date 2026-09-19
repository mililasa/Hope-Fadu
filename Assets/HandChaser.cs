using UnityEngine;

// La mano mantiene el tamano seteado en el Inspector.
// Se estira despacio hacia Pp, hasta el empty de la primera luz.
// Si la punta la alcanza, Pp vuelve al check point y la mano se retrae.
// Si Pp llega a la luz o la mano termina el estirado sin agarrarla, sigue.
public class HandChaser : MonoBehaviour
{
    [Header("Referencias")]
    public Transform target;          // Pp
    public Transform primeraLuz;      // empty que marca hasta donde llega
    public Transform checkPoint;

    [Header("Movimiento")]
    public float velocidadDeApuntado = 1.2f;
    public float velocidadEstirado = 0.25f;      // unidades de escala por segundo
    public float velocidadRetraccion = 0.7f;
    public float demoraAntesDeEstirar = 1.2f;

    [Header("Agarre")]
    public float radioAgarre = 0.5f;
    [Tooltip("0 = punta de los dedos. 0.2 = mas atras, a la altura de los nudillos.")]
    [Range(0f, 0.5f)]
    public float atrasoHastaNudillos = 0.22f;

    private enum Fase { Esperar, Estirar, Retractar, Inactiva }
    private Fase fase = Fase.Esperar;

    private SpriteRenderer sr;
    private Vector3 escalaSeteada;
    private float rotacionSeteada;
    private float anchoSprite = 5f;
    private float escalaX;
    private float anguloActual;
    private float cronometro;
    private bool yaAgarro;
    private bool luzEncendida;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        escalaSeteada = transform.localScale;
        rotacionSeteada = transform.eulerAngles.z;
        anguloActual = rotacionSeteada;
        escalaX = escalaSeteada.x;

        if (sr != null && sr.sprite != null)
            anchoSprite = sr.sprite.bounds.size.x;

        if (primeraLuz == null)
        {
            GameObject luz = GameObject.Find("PrimeraLuz");
            if (luz != null) primeraLuz = luz.transform;
        }

        if (checkPoint == null)
        {
            GameObject cp = GameObject.Find("CheckPoint");
            if (cp != null) checkPoint = cp.transform;
        }
    }

    void Start()
    {
        Aplicar();
    }

    void Update()
    {
        if (target == null) return;

        cronometro += Time.deltaTime;
        Apuntar();

        switch (fase)
        {
            case Fase.Esperar:
                escalaX = escalaSeteada.x;
                if (luzEncendida)
                {
                    CambiarFase(Fase.Inactiva);
                    break;
                }
                if (cronometro >= demoraAntesDeEstirar && !JugadorPasoLaLuz())
                    CambiarFase(Fase.Estirar);
                break;

            case Fase.Estirar:
                if (luzEncendida || JugadorPasoLaLuz())
                {
                    CambiarFase(Fase.Retractar);
                    break;
                }

                escalaX = Mathf.MoveTowards(escalaX, EscalaXHastaLaLuz(), Time.deltaTime * velocidadEstirado);
                if (IntentarAgarre())
                    break;

                if (Mathf.Abs(escalaX - EscalaXHastaLaLuz()) < 0.01f)
                    CambiarFase(Fase.Retractar);
                break;

            case Fase.Retractar:
                escalaX = Mathf.MoveTowards(escalaX, escalaSeteada.x, Time.deltaTime * velocidadRetraccion);
                if (!yaAgarro && !luzEncendida)
                    IntentarAgarre();

                if (Mathf.Abs(escalaX - escalaSeteada.x) < 0.01f)
                {
                    if (yaAgarro && !luzEncendida)
                    {
                        yaAgarro = false;
                        CambiarFase(Fase.Esperar);
                    }
                    else
                    {
                        CambiarFase(Fase.Inactiva);
                    }
                }
                break;

            case Fase.Inactiva:
                escalaX = escalaSeteada.x;
                break;
        }

        Aplicar();
    }

    void Apuntar()
    {
        if (fase == Fase.Inactiva) return;

        Vector3 mira = target.position;
        if (primeraLuz != null && JugadorPasoLaLuz())
            mira = primeraLuz.position;

        Vector2 hacia = mira - transform.position;
        float deseado = Mathf.Atan2(hacia.y, hacia.x) * Mathf.Rad2Deg;
        anguloActual = Mathf.LerpAngle(anguloActual, deseado, Time.deltaTime * velocidadDeApuntado);
    }

    float EscalaXHastaLaLuz()
    {
        if (primeraLuz == null) return escalaSeteada.x;

        float distLuz = Vector2.Distance(transform.position, primeraLuz.position);
        return Mathf.Max(escalaSeteada.x, distLuz / Mathf.Max(0.01f, anchoSprite));
    }

    bool JugadorPasoLaLuz()
    {
        if (primeraLuz == null) return false;
        Vector2 origen = transform.position;
        Vector2 eje = ((Vector2)primeraLuz.position - origen).normalized;
        if (eje.sqrMagnitude < 0.0001f) return false;

        float hastaLuz = Vector2.Dot((Vector2)primeraLuz.position - origen, eje);
        float hastaJugador = Vector2.Dot((Vector2)target.position - origen, eje);
        return hastaJugador > hastaLuz;
    }

    bool IntentarAgarre()
    {
        if (yaAgarro || target == null) return false;
        if (Vector2.Distance(PuntoDeAgarre(), target.position) > radioAgarre)
            return false;

        yaAgarro = true;

        PlayerController2D pp = target.GetComponent<PlayerController2D>();
        if (pp != null)
            pp.VolverAlCheckPoint();
        else if (checkPoint != null)
            target.position = checkPoint.position;

        return true;
    }

    void Aplicar()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, anguloActual);
        transform.localScale = new Vector3(escalaX, escalaSeteada.y, escalaSeteada.z);
    }

    public void Reiniciar()
    {
        luzEncendida = false;
        yaAgarro = false;
        escalaX = escalaSeteada.x;
        anguloActual = rotacionSeteada;
        CambiarFase(Fase.Esperar);
        Aplicar();
    }

    public void RetractarPorLuz()
    {
        luzEncendida = true;
        yaAgarro = false;
        if (fase != Fase.Inactiva)
            CambiarFase(Fase.Retractar);
    }

    public Vector3 PuntaDeLosDedos()
    {
        return transform.position + transform.right * LargoActual();
    }

    public Vector3 PuntoDeAgarre()
    {
        float largo = LargoActual();
        float atraso = Mathf.Clamp01(atrasoHastaNudillos);
        return transform.position + transform.right * (largo * (1f - atraso));
    }

    float LargoActual()
    {
        return anchoSprite * escalaX;
    }

    void CambiarFase(Fase nueva)
    {
        fase = nueva;
        cronometro = 0f;
    }

    void OnDrawGizmosSelected()
    {
        float ancho = anchoSprite;
        if (!Application.isPlaying)
        {
            SpriteRenderer r = GetComponent<SpriteRenderer>();
            if (r != null && r.sprite != null)
                ancho = r.sprite.bounds.size.x;
        }

        Gizmos.color = Color.red;
        Vector3 agarre = Application.isPlaying
            ? PuntoDeAgarre()
            : transform.position + transform.right * (ancho * transform.localScale.x * (1f - Mathf.Clamp01(atrasoHastaNudillos)));
        Gizmos.DrawWireSphere(agarre, radioAgarre);

        if (primeraLuz != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, primeraLuz.position);
            Gizmos.DrawWireSphere(primeraLuz.position, 0.25f);
        }
    }
}
