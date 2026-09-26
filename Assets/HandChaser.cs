using UnityEngine;

// La mano se queda en su transform de escena.
// Cuando Pp pasa "primer persecución" apunta a Pp y se estira hacia él.
// El largo máximo llega hasta "final persecución".
// Si los nudillos tocan a Pp, vuelve al check point.
// Si Pp pasa el final sin ser tocado, se retracta y termina.
public class HandChaser : MonoBehaviour
{
    [Header("Referencias")]
    public Transform target;
    public Transform inicioPersecucion;
    public Transform finalPersecucion;
    public Transform checkPoint;

    [Header("Movimiento")]
    public float velocidadDeApuntado = 1.2f;
    public float velocidadEstirado = 1.27f;
    public float velocidadRetraccion = 0.7f;

    [Header("Agarre")]
    [Tooltip("Hijo de la mano. Arrastralo en Scene para mover el circulo.")]
    public Transform puntoAgarre;
    public float radioAgarre = 0.45f;

    private enum Fase { Esperar, Estirar, Retractar, Inactiva }
    private Fase fase = Fase.Esperar;

    private Vector3 escalaSeteada;
    private Vector3 posicionFija;
    private float rotacionSeteada;
    private float escalaX;
    private float anguloActual;
    private bool yaAgarro;

    void Awake()
    {
        escalaSeteada = transform.localScale;
        posicionFija = transform.position;
        rotacionSeteada = transform.eulerAngles.z;
        anguloActual = rotacionSeteada;
        escalaX = escalaSeteada.x;

        if (inicioPersecucion == null)
            inicioPersecucion = BuscarTransform("primer persecución", "primer persecucion");

        if (finalPersecucion == null)
            finalPersecucion = BuscarTransform("final persecución", "final persecucion");

        if (checkPoint == null)
        {
            GameObject cp = GameObject.Find("CheckPoint");
            if (cp != null) checkPoint = cp.transform;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = false;
            rb.constraints = RigidbodyConstraints2D.FreezePosition;
        }

        CapsuleCollider2D capsula = GetComponent<CapsuleCollider2D>();
        if (capsula != null)
            capsula.enabled = false;

        AsegurarPuntoAgarre();
    }

    void Start()
    {
        Aplicar();
    }

    void Update()
    {
        if (target == null) return;

        switch (fase)
        {
            case Fase.Esperar:
                escalaX = escalaSeteada.x;
                if (JugadorPaso(inicioPersecucion))
                    CambiarFase(Fase.Estirar);
                break;

            case Fase.Estirar:
                if (JugadorPaso(finalPersecucion))
                {
                    CambiarFase(Fase.Retractar);
                    break;
                }

                Apuntar();
                escalaX = Mathf.MoveTowards(escalaX, EscalaXObjetivo(), Time.deltaTime * velocidadEstirado);
                IntentarAgarre();
                break;

            case Fase.Retractar:
                if (!yaAgarro && !JugadorPaso(finalPersecucion))
                    Apuntar();
                escalaX = Mathf.MoveTowards(escalaX, escalaSeteada.x, Time.deltaTime * velocidadRetraccion);

                if (Mathf.Abs(escalaX - escalaSeteada.x) < 0.01f)
                    CambiarFase(yaAgarro ? Fase.Esperar : Fase.Inactiva);
                break;

            case Fase.Inactiva:
                escalaX = escalaSeteada.x;
                break;
        }

        Aplicar();
    }

    void Apuntar()
    {
        Vector2 hacia = (Vector2)target.position - (Vector2)posicionFija;
        if (hacia.sqrMagnitude < 0.0001f) return;

        float deseado = Mathf.Atan2(hacia.y, hacia.x) * Mathf.Rad2Deg;
        anguloActual = Mathf.LerpAngle(anguloActual, deseado, Time.deltaTime * velocidadDeApuntado);
    }

    float EscalaXObjetivo()
    {
        float hastaJugador = EscalaXParaAlcanzar(target.position);
        if (finalPersecucion == null)
            return hastaJugador;
        return Mathf.Min(hastaJugador, EscalaXParaAlcanzar(finalPersecucion.position));
    }

    float EscalaXParaAlcanzar(Vector3 destino)
    {
        float largoDeseado = Vector2.Distance(posicionFija, destino);
        float alcanceEscala1 = Mathf.Max(0.01f, AlcanceLocal());
        return Mathf.Max(escalaSeteada.x, largoDeseado / alcanceEscala1);
    }

    bool JugadorPaso(Transform marca)
    {
        if (marca == null || target == null) return false;
        Vector2 eje = EjePersecucion();
        return Vector2.Dot((Vector2)target.position - (Vector2)marca.position, eje) >= 0f;
    }

    Vector2 EjePersecucion()
    {
        if (inicioPersecucion != null && finalPersecucion != null)
        {
            Vector2 eje = (Vector2)finalPersecucion.position - (Vector2)inicioPersecucion.position;
            if (eje.sqrMagnitude > 0.0001f)
                return eje.normalized;
        }
        return Vector2.right;
    }

    bool IntentarAgarre()
    {
        if (yaAgarro || target == null) return false;
        if (Vector2.Distance(Nudillos(), target.position) > radioAgarre)
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
        transform.position = posicionFija;
        transform.rotation = Quaternion.Euler(0f, 0f, anguloActual);
        transform.localScale = new Vector3(escalaX, escalaSeteada.y, escalaSeteada.z);
    }

    public Vector3 Nudillos()
    {
        if (puntoAgarre != null)
            return puntoAgarre.position;
        return transform.position;
    }

    float AlcanceLocal()
    {
        if (puntoAgarre == null)
            return 1f;
        Vector3 local = puntoAgarre.localPosition;
        local.z = 0f;
        return Mathf.Max(0.01f, local.magnitude);
    }

    void AsegurarPuntoAgarre()
    {
        if (puntoAgarre != null) return;

        Transform existente = transform.Find("Agarre");
        if (existente != null)
        {
            puntoAgarre = existente;
            return;
        }

        GameObject go = new GameObject("Agarre");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(2.2f, -1.1f, 0f);
        puntoAgarre = go.transform;
    }

    public void Reiniciar()
    {
        yaAgarro = false;
        escalaX = escalaSeteada.x;
        anguloActual = rotacionSeteada;
        CambiarFase(Fase.Esperar);
        Aplicar();
    }

    public void RetractarPorLuz()
    {
        // La persecución ya no se corta con la primera luz.
    }

    void CambiarFase(Fase nueva)
    {
        if (nueva == Fase.Esperar)
            yaAgarro = false;
        fase = nueva;
    }

    static Transform BuscarTransform(params string[] nombres)
    {
        for (int i = 0; i < nombres.Length; i++)
        {
            GameObject go = GameObject.Find(nombres[i]);
            if (go != null) return go.transform;
        }
        return null;
    }

    void OnDrawGizmos()
    {
        if (puntoAgarre == null)
            puntoAgarre = transform.Find("Agarre");
        if (puntoAgarre == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(puntoAgarre.position, radioAgarre);

        if (inicioPersecucion != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(inicioPersecucion.position, 0.25f);
        }
        if (finalPersecucion != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(finalPersecucion.position, 0.25f);
            Gizmos.DrawLine(transform.position, finalPersecucion.position);
        }
    }
}
