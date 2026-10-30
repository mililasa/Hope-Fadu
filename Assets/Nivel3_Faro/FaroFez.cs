using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================================
// FARO FEZ (nivel 3)
// El faro tiene 4 paredes y cada una tiene dos "paneles": su cara de adentro y
// su cara de afuera. En pantalla se ve siempre UN panel y Hope camina en 2D
// sobre él, con su PlayerController2D de siempre.
//
//   - Al empujar contra el borde, el faro gira 90° y Hope sigue en el panel de
//     al lado (las repisas con flecha continúan a la vuelta de la esquina).
//   - En una puerta o ventana, con ↑ la cámara da media vuelta y Hope pasa al
//     otro lado de la misma pared.
//   - Las repisas de las paredes de costado no se ven ni se pisan.
//
// Cómo se ubica todo: el faro está armado adentro de "Pivote", que gira. La
// cámara nunca gira. "Vista" k = el pivote girado k*90 grados. En la vista k,
// la pared k (0 Norte, 1 Oeste, 2 Sur, 3 Este) queda al fondo si Hope está
// adentro, y la pared opuesta queda de frente si está afuera.
// ============================================================================
public class FaroFez : MonoBehaviour
{
    [Header("Medidas del faro (tienen que coincidir con lo construido)")]
    public float medioAncho = 4f;
    public float grosorMuro = 0.4f;
    [Tooltip("Cuánto salen las repisas de la pared.")]
    public float salidaRepisa = 1f;
    [Tooltip("Altura del piso de la linterna (donde está el farol).")]
    public float alturaLinterna = 30f;

    [Header("Dónde empieza Hope")]
    public bool empiezaAfuera = true;
    [Range(0, 3)] public int vistaInicial = 0;

    [Header("Giros")]
    public float duracionGiro = 0.55f;
    public float duracionPuerta = 0.95f;
    [Tooltip("Si Hope empuja contra el borde, el faro gira solo.")]
    public bool girarAlLlegarAlBorde = true;
    [Tooltip("Segundos empujando contra el borde para que gire. Más = menos giros sin querer.")]
    public float segundosEmpujando = 0.15f;
    public KeyCode teclaPuerta = KeyCode.UpArrow;
    public KeyCode teclaPuertaAlternativa = KeyCode.W;
    [Tooltip("Qué tan cerca de la puerta tiene que estar Hope para poder pasar.")]
    public float distanciaPuerta = 0.75f;

    [Header("Cámara")]
    public float zoomAdentro = 4.5f;
    public float zoomAfuera = 5f;

    [Header("Flechas de esquina")]
    public Material materialFlecha;
    public float tamanoFlecha = 0.9f;

    [Header("Referencias (las completa el constructor)")]
    public Transform pivote;
    public PlayerController2D hope;
    [Tooltip("El farol de arriba (tu Lantern). Cuando Hope lo prende, termina el nivel.")]
    public Lantern farolFinal;
    [Tooltip("Los haces de luz que giran al ganar.")]
    public GameObject hazDeLuz;

    [Header("Final del nivel")]
    public string escenaSiguiente = "Menu";
    public float segundosAntesDeSalir = 4f;
    public Color fondoAlGanar = new Color(0.37f, 0.23f, 0.53f);
    [Tooltip("Objetos que se prenden cuando Hope enciende el farol.")]
    public GameObject[] activarAlGanar;

    // ---------------- estado ----------------
    bool adentro;          // ¿Hope está adentro del faro?
    int vista;             // 0..3
    float angulo;          // giro actual del pivote, en grados
    bool animando, ganado;
    float tiempoDesdeGiro = 99f;
    Vector2 ultimaPos;

    class Repisa { public PiezaFaro pieza; public bool adentro; public int pared, vista; public float a, b, top, grosor; public BoxCollider2D col; }
    class Flecha { public Transform t; public Repisa r; public Vector3 baseLocal; }
    class Puerta { public PuertaFaro p; public int pared; public float u, y; }
    readonly List<Repisa> repisas = new List<Repisa>();
    readonly List<Flecha> flechas = new List<Flecha>();
    readonly List<Puerta> puertas = new List<Puerta>();
    PiezaFaro[] piezas;

    Rigidbody2D rb;
    Collider2D colHope;
    CameraFollow camara;
    int capaPiso;
    BoxCollider2D pisoAdentro, sueloAfuera, linternaAdentro, balconAfuera, paredIzq, paredDer;
    readonly Collider2D[] golpes = new Collider2D[8];
    PhysicsMaterial2D sinFriccion;          // para que Hope no se quede pegada a las paredes
    readonly bool[] bordeArmado = { true, true };      // [0] izquierda, [1] derecha
    readonly float[] bordeEmpujando = { 0f, 0f };

    // ---------------- cuentas de paneles ----------------
    public static int ParedDeVista(bool adentro, int k) { return adentro ? k : (k + 2) % 4; }
    public static int VistaDePared(bool adentro, int pared) { return adentro ? pared : (pared + 2) % 4; }
    // Para qué lado gira (en vistas) al doblar la esquina hacia "lado" (+1 derecha, -1 izquierda)
    public static int GiroPorBorde(bool adentro, int lado) { return lado * (adentro ? -1 : 1); }

    public float Borde(bool enAdentro)
    {
        return enAdentro ? medioAncho - grosorMuro * 0.5f : medioAncho + grosorMuro * 0.5f + salidaRepisa;
    }

    // Punto del faro (en coordenadas del Pivote) que en la vista k queda en la posición u de la pantalla,
    // a la altura y, separado "d" de la pared.
    public static Vector3 PuntoDePanel(bool enAdentro, int k, float u, float y, float d, float medioAncho, float grosorMuro)
    {
        float z = enAdentro ? (medioAncho - grosorMuro * 0.5f - d) : -(medioAncho + grosorMuro * 0.5f + d);
        return Quaternion.Euler(0f, -90f * k, 0f) * new Vector3(u, y, z);
    }
    Vector3 PuntoDePanel(bool enAdentro, int k, float u, float y, float d)
    {
        return PuntoDePanel(enAdentro, k, u, y, d, medioAncho, grosorMuro);
    }
    // Lo contrario: un punto del faro, visto desde la vista k (x = posición en pantalla)
    static Vector3 VerDesde(int k, Vector3 local) { return Quaternion.Euler(0f, 90f * k, 0f) * local; }

    // =====================================================================
    void Start()
    {
        if (pivote == null) pivote = transform.Find("Pivote");
        if (hope == null) hope = FindObjectOfType<PlayerController2D>();
        if (farolFinal == null) farolFinal = FindObjectOfType<Lantern>();
        if (pivote == null || hope == null)
        {
            Debug.LogWarning("FaroFez: falta el Pivote o Hope. Usá HOPE > Nivel 3 > Crear escena Nivel_3.");
            enabled = false;
            return;
        }
        rb = hope.GetComponent<Rigidbody2D>();
        colHope = hope.GetComponent<Collider2D>();
        // Cayendo desde muy alto Hope va rápido: con detección continua no atraviesa las repisas
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        camara = FindObjectOfType<CameraFollow>();
        capaPiso = LayerMask.NameToLayer("Piso");
        if (capaPiso < 0) capaPiso = 0;

        sinFriccion = new PhysicsMaterial2D("Faro sin friccion") { friction = 0f, bounciness = 0f };

        // 1) Leer el faro con el pivote derecho. Cada repisa y puerta tiene sus números
        //    (pared, lado, desde, hasta, altura): no se adivina nada mirando los cubos.
        pivote.localRotation = Quaternion.identity;
        Transform raiz = new GameObject("Colisiones2D_Faro").transform;
        piezas = pivote.GetComponentsInChildren<PiezaFaro>(true);
        foreach (PiezaFaro p in piezas)
        {
            if (p.tipo != PiezaFaro.Tipo.Repisa) continue;
            p.Ubicar(this);            // por las dudas: el dibujo queda exactamente donde dicen los números
            AgregarRepisa(p, raiz);
        }
        foreach (PuertaFaro p in pivote.GetComponentsInChildren<PuertaFaro>(true))
        {
            p.Ubicar(this);
            puertas.Add(new Puerta { p = p, pared = p.ParedIndice, u = p.posicion, y = p.altura });
        }

        // 2) Pisos, paredes invisibles, bordes de giro y flechas
        float yBase = pivote.position.y;
        pisoAdentro = Caja2D(raiz, "Piso adentro", 0f, yBase, 2f * Borde(true) + 2f, 1f, false);
        sueloAfuera = Caja2D(raiz, "Suelo afuera", 0f, yBase, 2f * Borde(false) + 2f, 1f, false);
        linternaAdentro = Caja2D(raiz, "Piso linterna", 0f, yBase + alturaLinterna, 2f * Borde(true), 1f, true);
        balconAfuera = Caja2D(raiz, "Balcon", 0f, yBase + alturaLinterna, 2f * Borde(false), 1f, true);
        paredIzq = Caja2D(raiz, "Pared invisible izq", 0f, yBase + alturaLinterna + 20f, 1f, alturaLinterna + 40f, false);
        paredDer = Caja2D(raiz, "Pared invisible der", 0f, yBase + alturaLinterna + 20f, 1f, alturaLinterna + 40f, false);
        paredIzq.gameObject.layer = paredDer.gameObject.layer = 0;   // que no cuenten como piso para el salto

        // Los bordes de giro de versiones anteriores ya no hacen falta: el giro se calcula
        // con la posición de Hope contra la pared invisible.
        foreach (BordeGiroFaro b in GetComponentsInChildren<BordeGiroFaro>(true)) b.gameObject.SetActive(false);
        CrearFlechas();

        // 3) Arrancar
        if (farolFinal != null)
        {
            farolFinal.segundosPrendidoLibre = 0f;      // que no se apague solo
            farolFinal.mostrarCartelEncender = false;   // sin carteles: solo las flechas
        }
        if (hazDeLuz != null) hazDeLuz.SetActive(false);
        IrAlInicio();
    }

    void IrAlInicio()
    {
        adentro = !empiezaAfuera;
        vista = vistaInicial;
        angulo = 90f * vista;
        pivote.localRotation = Quaternion.Euler(0f, angulo, 0f);
        AplicarModo();
        VisibilidadQuieto();
        if (camara != null) camara.zoom = adentro ? zoomAdentro : zoomAfuera;
        ultimaPos = hope.transform.position;
    }

    void AgregarRepisa(PiezaFaro p, Transform raiz)
    {
        if (p.ParedIndice < 0 || p.lado == PiezaFaro.Lado.Ambos) return;
        var r = new Repisa { pieza = p, adentro = p.EsAdentro, pared = p.ParedIndice };
        r.vista = VistaDePared(r.adentro, r.pared);
        r.a = Mathf.Min(p.desde, p.hasta);
        r.b = Mathf.Max(p.desde, p.hasta);
        r.top = p.altura;
        r.grosor = Mathf.Max(0.1f, p.grosor);
        // El collider es más grueso que el dibujo (hacia abajo) para que no se atraviese cayendo rápido
        r.col = Caja2D(raiz, "Col_" + p.name, (r.a + r.b) * 0.5f, pivote.position.y + r.top, r.b - r.a, Mathf.Max(1f, r.grosor), true);
        repisas.Add(r);
    }

    // Collider 2D de un piso o repisa. "arriba" es la altura donde se apoyan los pies.
    BoxCollider2D Caja2D(Transform raiz, string nombre, float u, float arriba, float ancho, float alto, bool deUnLado)
    {
        GameObject go = new GameObject(nombre);
        go.layer = capaPiso;
        go.transform.SetParent(raiz, false);
        go.transform.position = new Vector3(pivote.position.x + u, arriba - alto * 0.5f, 0f);
        BoxCollider2D c = go.AddComponent<BoxCollider2D>();
        c.size = new Vector2(Mathf.Max(0.05f, ancho), alto);
        c.sharedMaterial = sinFriccion;
        // Las de un solo lado (repisas, piso de la linterna) se prenden y apagan en
        // ActualizarRepisas() según dónde estén los pies de Hope.
        _ = deUnLado;
        return c;
    }

    // Una flecha curva en cada punta de repisa que sigue en el panel de al lado
    void CrearFlechas()
    {
        foreach (Repisa r in repisas)
        {
            if (r.pieza.rota) continue;
            float B = Borde(r.adentro);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                bool tocaBorde = lado > 0 ? r.b >= B - 0.25f : r.a <= -B + 0.25f;
                if (!tocaBorde) continue;
                int k2 = (r.vista + GiroPorBorde(r.adentro, lado) + 4) % 4;
                bool sigue = false;
                foreach (Repisa o in repisas)
                    if (o != r && o.adentro == r.adentro && o.vista == k2 && Mathf.Abs(o.top - r.top) < 0.05f &&
                        (lado > 0 ? o.a <= -B + 0.25f : o.b >= B - 0.25f)) { sigue = true; break; }
                if (!sigue) continue;

                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(go.GetComponent<Collider>());
                go.name = "Flecha " + r.pieza.name + (lado > 0 ? " der" : " izq");
                go.transform.SetParent(pivote, false);
                Vector3 baseLocal = PuntoDePanel(r.adentro, r.vista, lado * (B - 0.6f), r.top + 0.75f, 0.5f);
                go.transform.localPosition = baseLocal;
                go.transform.localRotation = Quaternion.Euler(0f, -90f * r.vista, 0f);   // de frente a la cámara en su vista
                go.transform.localScale = new Vector3(tamanoFlecha * lado, tamanoFlecha, 1f);
                if (materialFlecha != null) go.GetComponent<Renderer>().sharedMaterial = materialFlecha;
                flechas.Add(new Flecha { t = go.transform, r = r, baseLocal = baseLocal });
            }
        }
    }

    // =====================================================================
    void Update()
    {
        // las flechas flotan un poquito
        float bote = Mathf.Sin(Time.time * 4f) * 0.08f;
        foreach (Flecha f in flechas) f.t.localPosition = f.baseLocal + Vector3.up * bote;

        if (ganado)
        {
            if (hazDeLuz != null) hazDeLuz.transform.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
            return;
        }
        if (farolFinal != null && farolFinal.EstadoActual == Lantern.Estado.Lit) { Ganar(); return; }
        if (animando || hope.IsLocked) return;
        tiempoDesdeGiro += Time.deltaTime;

        if (Input.GetKeyDown(teclaPuerta) || Input.GetKeyDown(teclaPuertaAlternativa))
        {
            Puerta p = PuertaCerca();
            if (p != null)
            {
                bool m1 = !adentro;
                StartCoroutine(Transicion(m1, (vista + 2) % 4, m1 ? p.u : -p.u, 2, duracionPuerta, p.pared));
                return;
            }
        }
        RevisarBordes();
    }

    void FixedUpdate()
    {
        if (animando || ganado) return;
        // Si Hope apareció en otro lado (volvió al checkpoint), el faro vuelve a como empezó
        // Si Hope volvió al checkpoint (por ejemplo, al morir), el faro vuelve a como empezó
        Vector2 pos = hope.transform.position;
        if ((pos - ultimaPos).sqrMagnitude > 9f && hope.checkPoint != null &&
            ((Vector2)hope.checkPoint.position - pos).sqrMagnitude < 1f)
            IrAlInicio();
        ultimaPos = pos;
        ActualizarRepisas();
    }

    // ---------------- REPISAS DE UN SOLO LADO ----------------
    // Una repisa solo es sólida si los pies de Hope están por encima de ella.
    // Así se la atraviesa saltando desde abajo y se la pisa al caer desde arriba.
    //   - Para prenderse: los pies tienen que estar casi a la altura de arriba de la repisa.
    //   - Para apagarse: los pies tienen que bajar un poco más (así no se cae estando parada).
    void ActualizarRepisas()
    {
        float pies = colHope.bounds.min.y, y0 = pivote.position.y;
        foreach (Repisa r in repisas)
            r.col.enabled = r.adentro == adentro && r.vista == vista && PiesArriba(r.col.enabled, pies, y0 + r.top);
        linternaAdentro.enabled = adentro && PiesArriba(linternaAdentro.enabled, pies, y0 + alturaLinterna);
        balconAfuera.enabled = !adentro && PiesArriba(balconAfuera.enabled, pies, y0 + alturaLinterna);
    }
    static bool PiesArriba(bool estabaPrendida, float pies, float arriba)
    {
        return estabaPrendida ? pies >= arriba - 0.15f : pies >= arriba - 0.02f;
    }

    float UHope() { return hope.transform.position.x - pivote.position.x; }

    bool TocaPiso()
    {
        Bounds b = colHope.bounds;
        int n = Physics2D.OverlapBoxNonAlloc(new Vector2(b.center.x, b.min.y - 0.05f),
            new Vector2(b.size.x * 0.9f, 0.12f), 0f, golpes, 1 << capaPiso);
        for (int i = 0; i < n; i++) if (golpes[i] != null && golpes[i].enabled) return true;
        return false;
    }

    Puerta PuertaCerca()
    {
        if (!TocaPiso()) return null;
        int w = ParedDeVista(adentro, vista);
        float pies = colHope.bounds.min.y - pivote.position.y, u = UHope();
        foreach (Puerta p in puertas)
        {
            if (p.pared != w) continue;
            float uP = adentro ? p.u : -p.u;   // del otro lado de la pared, la puerta queda espejada
            if (Mathf.Abs(pies - p.y) < 0.3f && Mathf.Abs(u - uP) < distanciaPuerta) return p;
        }
        return null;
    }

    // ---------------- DOBLAR LA ESQUINA ----------------
    // Hope toca el borde cuando su costado llega a la pared invisible.
    // Si sigue empujando un ratito para ese lado, el faro gira.
    void RevisarBordes()
    {
        if (!girarAlLlegarAlBorde || tiempoDesdeGiro < 0.3f) return;
        float entrada = Input.GetAxisRaw("Horizontal");
        float B = Borde(adentro), x0 = pivote.position.x;
        Bounds hb = colHope.bounds;
        for (int lado = -1; lado <= 1; lado += 2)
        {
            int i = lado > 0 ? 1 : 0;
            bool toca = lado > 0 ? hb.max.x >= x0 + B - 0.08f : hb.min.x <= x0 - B + 0.08f;
            if (!toca) { bordeArmado[i] = true; bordeEmpujando[i] = 0f; continue; }
            if (!bordeArmado[i]) continue;
            bool empuja = entrada * lado > 0.5f;
            bordeEmpujando[i] = empuja ? bordeEmpujando[i] + Time.deltaTime : 0f;
            if (bordeEmpujando[i] < segundosEmpujando) continue;

            int dk = GiroPorBorde(adentro, lado);
            // Hope aparece al principio del panel de al lado, a la misma altura
            StartCoroutine(Transicion(adentro, (vista + dk + 4) % 4, -lado * (B - 0.5f), dk, duracionGiro, -1));
            return;
        }
    }

    // ---------------- GIRO (esquina o puerta) ----------------
    // dk = cuántas vistas gira (±1 en una esquina, 2 en una puerta). paredPuerta = -1 si es una esquina.
    IEnumerator Transicion(bool m1, int k1, float u1, int dk, float duracion, int paredPuerta)
    {
        animando = true;
        bool esPuerta = paredPuerta >= 0;
        bool m0 = adentro;
        int k0 = vista;
        float a0 = angulo, a1 = angulo + 90f * dk;
        // Dónde está Hope en el faro antes y después (para moverla junto con el giro)
        Vector3 p0 = PuntoDePanel(m0, k0, UHope(), 0f, 0.5f);
        Vector3 p1 = PuntoDePanel(m1, k1, u1, 0f, 0.5f);
        Vector2 velocidad = rb.velocity;
        rb.simulated = false;
        float z0 = camara != null ? camara.zoom : 0f, z1 = m1 ? zoomAdentro : zoomAfuera;

        if (esPuerta) VisibilidadPuerta(m0, paredPuerta);
        else VisibilidadEsquina(k0, k1);

        float t = 0f;
        bool segundaMitad = false;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.05f, duracion);
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            float a = Mathf.Lerp(a0, a1, e);
            pivote.localRotation = Quaternion.Euler(0f, a, 0f);
            Vector3 w = Quaternion.Euler(0f, a, 0f) * Vector3.Lerp(p0, p1, e);
            Vector3 hp = hope.transform.position;
            hope.transform.position = new Vector3(pivote.position.x + w.x, hp.y, hp.z);
            if (camara != null && esPuerta) camara.zoom = Mathf.Lerp(z0, z1, e);
            if (esPuerta && !segundaMitad && t >= 0.5f) { segundaMitad = true; VisibilidadPuerta(m1, paredPuerta); }
            yield return null;
        }

        adentro = m1;
        vista = k1;
        angulo = Mathf.Repeat(a1, 360f);
        pivote.localRotation = Quaternion.Euler(0f, angulo, 0f);
        Vector3 fin = hope.transform.position;
        hope.transform.position = new Vector3(pivote.position.x + u1, fin.y, fin.z);
        if (camara != null) camara.zoom = z1;
        AplicarModo();
        VisibilidadQuieto();

        Physics2D.SyncTransforms();
        rb.simulated = true;
        rb.position = hope.transform.position;
        rb.velocity = esPuerta ? Vector2.zero : velocidad;
        bordeArmado[0] = bordeArmado[1] = false;     // espera a que Hope se aleje del borde
        bordeEmpujando[0] = bordeEmpujando[1] = 0f;
        tiempoDesdeGiro = 0f;
        ultimaPos = hope.transform.position;
        animando = false;
    }

    // Prende los colliders del panel actual y acomoda pisos, paredes invisibles y bordes
    void AplicarModo()
    {
        // Al cambiar de panel, las repisas del panel nuevo arrancan apagadas y
        // ActualizarRepisas() prende las que están debajo de los pies de Hope.
        foreach (Repisa r in repisas) r.col.enabled = false;
        linternaAdentro.enabled = balconAfuera.enabled = false;
        pisoAdentro.enabled = adentro;
        sueloAfuera.enabled = !adentro;
        ActualizarRepisas();
        float B = Borde(adentro), x = pivote.position.x;
        MoverX(paredIzq.transform, x - B - 0.5f);
        MoverX(paredDer.transform, x + B + 0.5f);
    }
    static void MoverX(Transform t, float x) { Vector3 p = t.position; t.position = new Vector3(x, p.y, p.z); }

    // ---------------- QUÉ SE VE ----------------
    void VisibilidadQuieto()
    {
        var paredes = new HashSet<int> { ParedDeVista(adentro, vista) };
        var ocultos = new HashSet<int>();
        if (adentro) ocultos.Add((vista + 2) % 4);   // la pared de adelante
        Aplicar(adentro, paredes, ocultos, true);
    }
    void VisibilidadEsquina(int k0, int k1)
    {
        var paredes = new HashSet<int> { ParedDeVista(adentro, k0), ParedDeVista(adentro, k1) };
        var ocultos = new HashSet<int>();
        if (adentro) { ocultos.Add((k0 + 2) % 4); ocultos.Add((k1 + 2) % 4); }
        Aplicar(adentro, paredes, ocultos, false);
    }
    void VisibilidadPuerta(bool modo, int pared)
    {
        var paredes = new HashSet<int> { pared };
        var ocultos = new HashSet<int>();
        if (modo) for (int w = 0; w < 4; w++) if (w != pared) ocultos.Add(w);
        Aplicar(modo, paredes, ocultos, false);
    }
    void Aplicar(bool modo, HashSet<int> paredes, HashSet<int> ocultos, bool conFlechas)
    {
        foreach (PiezaFaro p in piezas)
        {
            switch (p.tipo)
            {
                case PiezaFaro.Tipo.Repisa:
                case PiezaFaro.Tipo.Decoracion:
                    bool deEsteLado = p.lado == PiezaFaro.Lado.Ambos || p.EsAdentro == modo;
                    p.Mostrar(deEsteLado && (p.ParedIndice < 0 || paredes.Contains(p.ParedIndice)));
                    break;
                case PiezaFaro.Tipo.Pared:
                    p.Mostrar(!ocultos.Contains(p.ParedIndice));
                    break;
                default:
                    p.Mostrar(p.lado == PiezaFaro.Lado.Ambos || p.EsAdentro == modo);
                    break;
            }
        }
        foreach (Puerta p in puertas)
            p.p.Mostrar(modo && paredes.Contains(p.pared), !modo && paredes.Contains(p.pared));
        foreach (Flecha f in flechas)
            f.t.gameObject.SetActive(conFlechas && f.r.adentro == modo && paredes.Contains(f.r.pared));
    }

    // ---------------- FINAL ----------------
    void Ganar()
    {
        ganado = true;
        if (hazDeLuz != null) hazDeLuz.SetActive(true);
        if (activarAlGanar != null)
            foreach (GameObject go in activarAlGanar) if (go != null) go.SetActive(true);
        Camera cam = Camera.main;
        if (cam != null) StartCoroutine(AclararCielo(cam));
        StartCoroutine(SalirDelNivel());
    }

    IEnumerator AclararCielo(Camera cam)
    {
        Color desde = cam.backgroundColor;
        for (float t = 0f; t < 1f; t += Time.deltaTime / 2.5f)
        {
            cam.backgroundColor = Color.Lerp(desde, fondoAlGanar, t);
            yield return null;
        }
        cam.backgroundColor = fondoAlGanar;
    }

    IEnumerator SalirDelNivel()
    {
        yield return new WaitForSeconds(segundosAntesDeSalir);
        if (!string.IsNullOrEmpty(escenaSiguiente) && Application.CanStreamedLevelBeLoaded(escenaSiguiente))
            SceneManager.LoadScene(escenaSiguiente);
    }
}
