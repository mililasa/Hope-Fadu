using UnityEngine;

// ============================================================================
// TEXTURA REPETIDA
// Repite la textura según el tamaño del objeto, para que los ladrillos y las
// tablas no se estiren cuando agrandás una repisa o una pared.
// Funciona también en el editor: estirá la repisa y la textura se acomoda.
// ============================================================================
[ExecuteAlways]
public class TexturaRepetida : MonoBehaviour
{
    [Tooltip("Cuántas unidades ocupa una repetición de la textura (ancho, alto). Alto 0 = no se repite en alto.")]
    public Vector2 baldosa = new Vector2(2f, 2f);

    MaterialPropertyBlock bloque;
    Vector3 ultimaEscala;

    void OnEnable() { Aplicar(); }

    void Update()
    {
        if (!Application.isPlaying && transform.lossyScale != ultimaEscala) Aplicar();
    }

    public void Aplicar()
    {
        Renderer r = GetComponent<Renderer>();
        if (r == null) return;
        if (bloque == null) bloque = new MaterialPropertyBlock();
        Vector3 s = transform.lossyScale;
        ultimaEscala = s;
        // En un quad el ancho es X; en un cubo, el lado más largo entre X y Z
        float ancho = GetComponent<MeshFilter>() != null && GetComponent<MeshFilter>().sharedMesh != null &&
                      GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Quad")
            ? Mathf.Abs(s.x) : Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
        float rx = baldosa.x > 0.01f ? ancho / baldosa.x : 1f;
        float ry = baldosa.y > 0.01f ? Mathf.Abs(s.y) / baldosa.y : 1f;
        r.GetPropertyBlock(bloque);
        bloque.SetVector("_MainTex_ST", new Vector4(Mathf.Max(0.05f, rx), Mathf.Max(0.05f, ry), 0f, 0f));
        r.SetPropertyBlock(bloque);
    }
}
