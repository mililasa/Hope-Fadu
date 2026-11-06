using UnityEngine;

// Farol (6): al pasarlo, las muertes reaparecen aca.
// A la muerte numero 10 deja de ser checkpoint y vuelve el del principio.
public class FarolCheckpoint : MonoBehaviour
{
    public int muertesParaPerderlo = 10;
    public Vector2 offsetReaparicion = new Vector2(0f, -1f);

    bool pasado;
    bool perdido;
    int muertes;

    public void RegistrarPaso(float xJugador)
    {
        if (pasado || perdido) return;
        if (xJugador > transform.position.x)
            pasado = true;
    }

    public bool ConsumirParaReaparecer(out Vector3 punto)
    {
        punto = transform.position + (Vector3)offsetReaparicion;
        if (!pasado || perdido)
            return false;

        muertes++;
        if (muertes >= muertesParaPerderlo)
        {
            perdido = true;
            return false;
        }

        return true;
    }
}
