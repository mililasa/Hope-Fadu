using UnityEngine;

// ============================================================================
// BORDE DE GIRO
// Zona invisible (trigger) pegada a un costado del faro. Cuando Hope camina
// contra ella un ratito, FaroFez gira el faro solo y Hope sigue "a la vuelta
// de la esquina".
//
// Hay dos: "Borde giro izquierda" y "Borde giro derecha", hijos de
// Faro_Nivel3 (NO de Pivote, porque no tienen que girar con el faro).
// En la ventana Scene se ven como rectángulos celestes. Podés moverlos o
// cambiarles el tamaño del Box Collider 2D.
// ============================================================================
[RequireComponent(typeof(BoxCollider2D))]
public class BordeGiroFaro : MonoBehaviour
{
    public enum Lado { Izquierda = -1, Derecha = 1 }

    [Tooltip("De qué costado está. Derecha = Hope tiene que caminar hacia la derecha para activarlo.")]
    public Lado lado = Lado.Derecha;

    // Los usa FaroFez
    [System.NonSerialized] public bool armado = true;      // false = ya giró, espera que Hope se aleje
    [System.NonSerialized] public float empujando;         // segundos que Hope lleva empujando contra el borde

    BoxCollider2D col;
    public BoxCollider2D Col
    {
        get { if (col == null) col = GetComponent<BoxCollider2D>(); return col; }
    }

    void Awake()
    {
        Col.isTrigger = true;   // no frena a Hope: la frena la pared invisible que está atrás
    }

    void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    // Crea un borde (lo usan el constructor y FaroFez si no encuentra ninguno)
    public static BordeGiroFaro Crear(Transform padre, Lado lado, float xMundo, float yCentro, float alto)
    {
        GameObject go = new GameObject(lado == Lado.Derecha ? "Borde giro derecha" : "Borde giro izquierda");
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3(xMundo, yCentro, 0f);
        BoxCollider2D c = go.AddComponent<BoxCollider2D>();
        c.isTrigger = true;
        c.size = new Vector2(0.5f, alto);
        BordeGiroFaro b = go.AddComponent<BordeGiroFaro>();
        b.lado = lado;
        return b;
    }

    void OnDrawGizmos()
    {
        BoxCollider2D c = Col;
        if (c == null) return;
        Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.8f);
        Vector3 centro = transform.TransformPoint(c.offset);
        Vector3 tam = Vector3.Scale(c.size, transform.lossyScale);
        Gizmos.DrawWireCube(centro, tam);
        // flechita que indica hacia dónde gira
        Vector3 f = centro + Vector3.up * 2f;
        Gizmos.DrawLine(f, f + Vector3.right * 0.8f * (int)lado);
    }
}
