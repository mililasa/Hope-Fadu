using UnityEngine;

[DisallowMultipleComponent]
public sealed class HopeWoodCrateSurface : MonoBehaviour
{
    static readonly Vector2[] bordeCajas =
    {
        new Vector2(1080, 335), new Vector2(1103, 306),
        new Vector2(1131, 276), new Vector2(1147, 246),
        new Vector2(1167, 257), new Vector2(1175, 242),
        new Vector2(1247, 258), new Vector2(1234, 280),
        new Vector2(1203, 283), new Vector2(1202, 302),
        new Vector2(1221, 308), new Vector2(1226, 327)
    };

    public bool ContieneApoyo(Vector3 pie)
    {
        Vector2 local = transform.InverseTransformPoint(pie);
        Vector2 pixel = new Vector2(local.x * 32f + 2380f, 240f - local.y * 32f);
        if (pixel.x < 1078f || pixel.x > 1251f) return false;

        const float tolerancia = 27f;
        for (int i = 1; i < bordeCajas.Length; i++)
        {
            Vector2 inicio = bordeCajas[i - 1];
            Vector2 tramo = bordeCajas[i] - inicio;
            float avance = Mathf.Clamp01(Vector2.Dot(pixel - inicio, tramo) / tramo.sqrMagnitude);
            if ((pixel - inicio - avance * tramo).sqrMagnitude <= tolerancia * tolerancia)
                return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        for (int i = 1; i < bordeCajas.Length; i++)
            Gizmos.DrawLine(PuntoMundo(bordeCajas[i - 1]), PuntoMundo(bordeCajas[i]));
    }

    Vector3 PuntoMundo(Vector2 pixel)
    {
        return transform.TransformPoint(new Vector3((pixel.x - 2380f) / 32f, (240f - pixel.y) / 32f, 0));
    }
}
