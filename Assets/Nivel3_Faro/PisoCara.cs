using UnityEngine;

// ============================================================================
// PISOS DE UNA CARA, DIBUJADOS CON EDGE COLLIDER 2D
// Cada cara del faro, afuera y adentro, tiene su grupo (8 en total), adentro de
// "Pisos (Edge Collider)" en Faro_Nivel3. El faro prende SOLO el grupo de la
// cara que se está viendo; los demás quedan apagados.
//
// Cada grupo tiene dos hijos:
//   Se atraviesan desde abajo   plataformas: Hope las atraviesa saltando y las pisa
//                               al caer (tiene un Platform Effector 2D).
//   Sólidos                     se chocan de todos lados (el piso de abajo, paredes).
// En cualquiera de los dos podés agregar más Edge Collider 2D (Add Component), o
// en uno mismo agregar puntos. Para mover los puntos: botón "Edit Collider" del
// Edge Collider 2D, y arrastrás los puntos encima del dibujo.
//
// Para ver el dibujo de la cara mientras editás: en Faro_Nivel3, los botones
// "Ver cara para dibujar sus pisos" giran el faro a esa cara y muestran solo su
// dibujo y su grupo.
//
// Las coordenadas son las de la cara: x = 0 es el centro del faro, y = 0 es el
// suelo. El grupo está siempre en el pie del faro (no lo muevas: al dar Play se
// vuelve a poner ahí).
// ============================================================================
public class PisoCara : MonoBehaviour
{
    public enum CaraDibujo { Frente, Derecha, Atras, Izquierda }

    [Tooltip("De qué dibujo son estos pisos (como en los nombres de los PNG).")]
    public CaraDibujo cara = CaraDibujo.Frente;
    public PiezaFaro.Lado lado = PiezaFaro.Lado.Afuera;

    public bool EsAdentro { get { return lado == PiezaFaro.Lado.Adentro; } }

    // Pared del faro de esa cara (0 Norte, 1 Oeste, 2 Sur, 3 Este): frente = Sur, derecha = Este, atrás = Norte, izquierda = Oeste
    public int ParedIndice
    {
        get
        {
            switch (cara)
            {
                case CaraDibujo.Frente: return 2;
                case CaraDibujo.Derecha: return 3;
                case CaraDibujo.Atras: return 0;
                default: return 1;
            }
        }
    }

    public static CaraDibujo DeParedIndice(int pared)
    {
        switch (pared)
        {
            case 2: return CaraDibujo.Frente;
            case 3: return CaraDibujo.Derecha;
            case 0: return CaraDibujo.Atras;
            default: return CaraDibujo.Izquierda;
        }
    }

    // Las líneas se ven siempre en la ventana Scene (celeste = se atraviesan desde abajo, naranja = sólidos)
    void OnDrawGizmos()
    {
        foreach (EdgeCollider2D e in GetComponentsInChildren<EdgeCollider2D>())
        {
            bool unSentido = e.GetComponent<PlatformEffector2D>() != null;
            Gizmos.color = unSentido ? new Color(0.3f, 0.9f, 1f, 1f) : new Color(1f, 0.6f, 0.2f, 1f);
            Vector2[] p = e.points;
            for (int i = 0; i + 1 < p.Length; i++)
                Gizmos.DrawLine(e.transform.TransformPoint(p[i] + e.offset), e.transform.TransformPoint(p[i + 1] + e.offset));
            for (int i = 0; i < p.Length; i++)
                Gizmos.DrawWireSphere(e.transform.TransformPoint(p[i] + e.offset), 0.06f);
        }
    }
}
