using UnityEngine;

// ============================================================================
// LÁMINA DEL FARO (tus dibujos)
// Muestra uno de tus PNG pegado a una cara del faro. Cada pared tiene 4 láminas:
//   Torre afuera, Plataformas afuera, Torre adentro, Plataformas adentro.
// Y hay una más, el Fondo (cielo, suelo, avión), que no gira.
//
// Cómo se ubica el dibujo: tres números de Faro_Nivel3 > "Dibujo" dicen dónde
// cae el faro en tu PNG (en píxeles):
//   Pixeles Por Unidad   cuántos píxeles mide 1 unidad de Unity (Hope mide ~1)
//   Centro X             columna del centro del faro
//   Suelo Y              fila donde apoya el faro (contando desde arriba)
// Con eso, la lámina "Torre" recorta solo la forma del faro (el trapecio) y la
// lámina "Plataformas" pone el PNG entero encima, en el mismo lugar.
//
// Para cambiar un dibujo: reemplazá el PNG (mismo nombre) o arrastrá otra
// imagen a "Dibujo" en el Inspector de la lámina.
//
// ADENTRO Y AFUERA: "Cara" es el frente del faro al que pertenece el dibujo.
//   Afuera: el dibujo va sobre esa cara, mirando hacia afuera.
//   Adentro: es lo que se ve al entrar por esa cara (como si se sacara la pared
//   de adelante): el dibujo va en la pared de enfrente, del lado de adentro.
//   Por eso adentro y afuera de la misma cara quedan en el mismo lugar de la
//   pantalla, como en tus dos PNG.
// El Fondo con Lado = Afuera solo se ve afuera (adentro no hay cielo).
// ============================================================================
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LaminaFaro : MonoBehaviour
{
    public enum Capa
    {
        Torre,        // recorta la forma del faro de tu PNG
        Plataformas,  // el PNG entero (transparente) sobre la torre
        Fondo         // el PNG entero, lejos, sin girar
    }

    public Capa capa = Capa.Torre;
    public PiezaFaro.Cara pared = PiezaFaro.Cara.Sur;
    public PiezaFaro.Lado lado = PiezaFaro.Lado.Afuera;
    [Tooltip("El PNG. Si lo dejás vacío se usa el que tenga el material.")]
    public Texture2D dibujo;
    [Tooltip("Cuánto se separa de la pared hacia la cámara (para que las plataformas queden delante de la torre).")]
    public float separacion = 0f;
    [Tooltip("Cuántos cortes tiene la lámina (más = el giro de adentro se ve más suave).")]
    [Range(1, 32)] public int cortes = 12;

    public bool EsAdentro { get { return lado == PiezaFaro.Lado.Adentro; } }
    public int ParedIndice { get { return pared == PiezaFaro.Cara.Ninguna ? -1 : (int)pared; } }
    // La pared donde está pegado el dibujo: afuera la misma cara, adentro la de enfrente
    public int ParedFisica { get { int w = ParedIndice; return w < 0 ? -1 : (EsAdentro ? (w + 2) % 4 : w); } }

    MeshRenderer mr;
    Mesh malla;

    public void Mostrar(bool visible)
    {
        if (mr == null) mr = GetComponent<MeshRenderer>();
        if (mr.enabled != visible) mr.enabled = visible;
    }

    void OnEnable() { Armar(GetComponentInParent<FaroFez>()); }

    void OnDestroy()
    {
        if (malla == null) return;
        if (Application.isPlaying) Destroy(malla); else DestroyImmediate(malla);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += () => { if (this != null) Armar(GetComponentInParent<FaroFez>()); };
    }
#endif

    // Arma la malla (los vértices y qué parte del PNG va en cada uno)
    public void Armar(FaroFez f)
    {
        if (f == null) return;
        mr = GetComponent<MeshRenderer>();
        Texture tex = dibujo;
        if (tex == null && mr.sharedMaterial != null) tex = mr.sharedMaterial.mainTexture;
        if (dibujo != null && mr.sharedMaterial != null && mr.sharedMaterial.mainTexture != dibujo)
        {
            // cada lámina con su propio dibujo, sin tocar el material compartido
            var bloque = new MaterialPropertyBlock();
            mr.GetPropertyBlock(bloque);
            bloque.SetTexture("_MainTex", dibujo);
            mr.SetPropertyBlock(bloque);
        }
        float anchoPx = tex != null ? tex.width : 1200f, altoPx = tex != null ? tex.height : 2520f;
        float ppu = Mathf.Max(1f, f.pixelesPorUnidad);

        // Coordenadas de tu PNG (en unidades) respecto del pie del faro
        float uIzq = -f.centroDibujoX / ppu, uDer = (anchoPx - f.centroDibujoX) / ppu;
        float yAbajo = -(altoPx - f.sueloDibujoY) / ppu, yArriba = f.sueloDibujoY / ppu;

        if (malla == null)
        {
            malla = new Mesh { name = "Lamina " + name };
            malla.hideFlags = HideFlags.DontSave;     // se vuelve a armar sola al abrir la escena
        }
        malla.Clear();
        var v = new System.Collections.Generic.List<Vector3>();
        var uv = new System.Collections.Generic.List<Vector2>();
        var tri = new System.Collections.Generic.List<int>();
        int n = Mathf.Max(1, cortes);

        if (capa == Capa.Fondo)
        {
            // El fondo va lejos, detrás del faro, y no gira. Es más grande que el PNG:
            // los bordes del dibujo se estiran (la textura está en "Clamp").
            transform.localPosition = new Vector3(0f, 0f, f.pivote != null ? f.pivote.localPosition.z + f.distanciaFondo : 40f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            // Tiene un AGUJERO con la forma del faro: el faro dibujado en el fondo no se ve
            // (si no, aparecería detrás del faro de verdad cuando gira).
            float extra = 30f;
            float L = uIzq - extra, Rr = uDer + extra, B = yAbajo - extra, T = yArriba + extra, H = f.alturaLinterna;
            float m0 = f.MedioAnchoEn(0f) - 0.02f, mH = f.MedioAnchoEn(H) - 0.02f;   // un poquito más chico: sin rayitas en el borde
            System.Action<Vector2, Vector2, Vector2, Vector2> cuadro = (p0, p1, p2, p3) =>
            {
                // p0 abajo-izq, p1 arriba-izq, p2 arriba-der, p3 abajo-der
                int i = v.Count;
                foreach (Vector2 q in new[] { p0, p1, p2, p3 })
                {
                    v.Add(new Vector3(q.x, q.y, 0f));
                    uv.Add(AUV(q.x, q.y, f, anchoPx, altoPx, ppu));
                }
                tri.Add(i); tri.Add(i + 1); tri.Add(i + 2);
                tri.Add(i); tri.Add(i + 2); tri.Add(i + 3);
            };
            cuadro(new Vector2(L, B), new Vector2(L, 0f), new Vector2(Rr, 0f), new Vector2(Rr, B));        // suelo y abajo
            cuadro(new Vector2(L, 0f), new Vector2(L, H), new Vector2(-mH, H), new Vector2(-m0, 0f));     // a la izquierda del faro
            cuadro(new Vector2(m0, 0f), new Vector2(mH, H), new Vector2(Rr, H), new Vector2(Rr, 0f));     // a la derecha del faro
            cuadro(new Vector2(L, H), new Vector2(L, T), new Vector2(Rr, T), new Vector2(Rr, H));         // cielo de arriba
        }
        else
        {
            // Pegada a su pared: los vértices están en coordenadas del Pivote
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            int pared = Mathf.Max(0, ParedFisica);
            int k = FaroFez.VistaDePared(EsAdentro, pared);
            bool torre = capa == Capa.Torre;
            float y0 = torre ? 0f : yAbajo, y1 = torre ? f.alturaLinterna : yArriba;
            System.Func<float, float, Vector2> enDibujo = (a, b) =>
            {
                float y = Mathf.Lerp(y0, y1, b);
                float u = torre ? Mathf.Lerp(-f.MedioAnchoEn(y), f.MedioAnchoEn(y), a) : Mathf.Lerp(uIzq, uDer, a);
                return new Vector2(u, y);
            };
            Grilla(v, uv, tri, n, n, (a, b) =>
            {
                Vector2 p = enDibujo(a, b);
                return f.PuntoDePanel(EsAdentro, k, p.x, p.y, separacion);
            }, (a, b) =>
            {
                Vector2 p = enDibujo(a, b);
                return AUV(p.x, p.y, f, anchoPx, altoPx, ppu);
            });
        }

        var blanco = new Color[v.Count];
        for (int i = 0; i < blanco.Length; i++) blanco[i] = Color.white;
        malla.SetVertices(v);
        malla.colors = blanco;
        malla.SetUVs(0, uv);
        malla.SetTriangles(tri, 0);
        malla.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = malla;
    }

    // Punto del faro (u, y en unidades) → lugar en tu PNG (0 a 1)
    static Vector2 AUV(float u, float y, FaroFez f, float anchoPx, float altoPx, float ppu)
    {
        float px = f.centroDibujoX + u * ppu;          // columna (desde la izquierda)
        float py = f.sueloDibujoY - y * ppu;           // fila (desde arriba)
        return new Vector2(px / anchoPx, 1f - py / altoPx);
    }

    // Una grilla de nx por ny cuadraditos. Las caras se ven de los dos lados (el shader no las descarta)
    // pero igual se arman mirando a la cámara.
    static void Grilla(System.Collections.Generic.List<Vector3> v, System.Collections.Generic.List<Vector2> uv,
                       System.Collections.Generic.List<int> tri, int nx, int ny,
                       System.Func<float, float, Vector3> pos, System.Func<float, float, Vector2> coord)
    {
        int primero = v.Count;
        for (int j = 0; j <= ny; j++)
            for (int i = 0; i <= nx; i++)
            {
                float a = (float)i / nx, b = (float)j / ny;
                v.Add(pos(a, b));
                uv.Add(coord(a, b));
            }
        for (int j = 0; j < ny; j++)
            for (int i = 0; i < nx; i++)
            {
                int i00 = primero + j * (nx + 1) + i, i10 = i00 + 1, i01 = i00 + nx + 1, i11 = i01 + 1;
                tri.Add(i00); tri.Add(i01); tri.Add(i11);
                tri.Add(i00); tri.Add(i11); tri.Add(i10);
            }
    }
}
