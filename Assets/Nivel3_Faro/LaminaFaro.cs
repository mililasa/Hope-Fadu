using UnityEngine;

// ============================================================================
// LÁMINA DEL FARO (tus dibujos)
// Muestra uno de los PNG pegado a una cara del faro. Cada cara tiene sus láminas:
//   Torre afuera, Plataformas afuera, (Reja), Torre adentro, Plataformas adentro.
// Y están las del Fondo: el cielo y lo que rodea a cada cara (suelo, avión), que no giran.
//
// Las láminas NO son un script en cada objeto: la lista está en Faro_Nivel3,
// en "Laminas Dibujo" (cada una dice qué es, de qué cara y qué objeto la dibuja).
// FaroFez arma todos los dibujos al dar Play y cuando cambiás algo en el Inspector.
// (Antes eran un script por objeto y a veces Unity los perdía al dar Play.)
//
// Cómo se ubica el dibujo: tres números de Faro_Nivel3 > "Dibujo" dicen dónde
// cae el faro en el PNG (en píxeles):
//   Pixeles Por Unidad   cuántos píxeles mide 1 unidad de Unity
//   Centro X             columna del centro del faro
//   Suelo Y              fila donde apoya el faro (contando desde arriba)
// Con eso, la lámina "Torre" recorta solo la forma del faro (el trapecio) y la
// lámina "Plataformas" pone el PNG entero encima, en el mismo lugar.
//
// ADENTRO Y AFUERA: "Cara" es el frente del faro al que pertenece el dibujo.
//   Afuera: el dibujo va sobre esa cara, mirando hacia afuera.
//   Adentro: es lo que se ve al entrar por esa cara, en el mismo lugar de la
//   pantalla que afuera (como en los PNG).
// ============================================================================
[System.Serializable]
public class LaminaFaro
{
    public enum Capa
    {
        Torre,        // recorta la forma del faro del PNG
        Plataformas,  // el PNG entero (transparente) sobre la torre
        Fondo         // lejos, sin girar: el cielo, o lo que rodea a una cara (suelo, avión)
    }

    public string nombre = "Lámina";
    [Tooltip("El objeto que la dibuja (tiene Mesh Filter y Mesh Renderer).")]
    public MeshRenderer render;
    public Capa capa = Capa.Torre;
    public PiezaFaro.Cara pared = PiezaFaro.Cara.Sur;
    public PiezaFaro.Lado lado = PiezaFaro.Lado.Afuera;
    [Tooltip("El PNG. Si lo dejás vacío se usa el que tenga el material.")]
    public Texture2D dibujo;
    [Tooltip("Cuánto se separa de la pared hacia la cámara (para que las plataformas queden delante de la torre).")]
    public float separacion = 0f;
    [Tooltip("Cuántos cortes tiene la lámina (más = el giro con profundidad se ve más suave).")]
    [Range(1, 32)] public int cortes = 12;
    [Tooltip("Solo Fondo: tiene un agujero con la forma del faro (para lo que rodea a una cara). Destildado = el PNG entero (el cielo).")]
    public bool agujero = true;
    [Tooltip("Se dibuja DELANTE de Hope (por ejemplo, una reja). Igual queda debajo de la oscuridad.")]
    public bool delante = false;

    public bool EsAdentro { get { return lado == PiezaFaro.Lado.Adentro; } }
    public int ParedIndice { get { return pared == PiezaFaro.Cara.Ninguna ? -1 : (int)pared; } }
    // La pared donde está pegado el dibujo: afuera, la de su cara; adentro, la que queda al
    // fondo cuando se ve el interior de esa cara (así adentro las caras siguen el mismo orden que afuera)
    public int ParedFisica { get { int w = ParedIndice; return w < 0 ? -1 : (EsAdentro ? FaroFez.ParedAdentroDeCara(w) : w); } }

    public void Mostrar(bool visible)
    {
        if (render != null && render.enabled != visible) render.enabled = visible;
    }

    // Arma la malla (los vértices y qué parte del PNG va en cada uno)
    public void Armar(FaroFez f)
    {
        if (f == null || render == null) return;
        Transform transform = render.transform;
        MeshFilter mf = render.GetComponent<MeshFilter>();
        if (mf == null) mf = render.gameObject.AddComponent<MeshFilter>();

        Texture tex = dibujo;
        if (tex == null && render.sharedMaterial != null && render.sharedMaterial.HasProperty("_MainTex")) tex = render.sharedMaterial.mainTexture;
        if (dibujo != null && render.sharedMaterial != null && render.sharedMaterial.mainTexture != dibujo)
        {
            // cada lámina con su propio dibujo, sin tocar el material compartido
            var bloque = new MaterialPropertyBlock();
            render.GetPropertyBlock(bloque);
            bloque.SetTexture("_MainTex", dibujo);
            render.SetPropertyBlock(bloque);
        }
        float anchoPx = tex != null ? tex.width : 1200f, altoPx = tex != null ? tex.height : 2520f;
        float ppu = Mathf.Max(1f, f.pixelesPorUnidad);

        // Coordenadas del PNG (en unidades) respecto del pie del faro
        float uIzq = -f.centroDibujoX / ppu, uDer = (anchoPx - f.centroDibujoX) / ppu;
        float yAbajo = -(altoPx - f.sueloDibujoY) / ppu, yArriba = f.sueloDibujoY / ppu;

        // La malla se guarda con la escena; si ya hay una nuestra, se reusa (no se acumulan)
        Mesh malla = mf.sharedMesh;
        if (malla == null || !malla.name.StartsWith("Lamina "))
            malla = new Mesh { name = "Lamina " + nombre };
        malla.Clear();
        var v = new System.Collections.Generic.List<Vector3>();
        var uv = new System.Collections.Generic.List<Vector2>();
        var tri = new System.Collections.Generic.List<int>();
        int n = Mathf.Max(1, cortes);

        if (capa == Capa.Fondo)
        {
            // El fondo va lejos, detrás del faro, y no gira. Es más grande que el PNG:
            // los bordes del dibujo se estiran (la textura está en "Clamp").
            // El cielo (sin agujero) va un poco más atrás que lo que rodea a cada cara
            float z = (f.pivote != null ? f.pivote.localPosition.z + f.distanciaFondo : 40f) + (agujero ? 0f : 5f);
            transform.localPosition = new Vector3(0f, 0f, z);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            // Con AGUJERO con la forma del faro: el faro dibujado en el fondo no se ve
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
            if (!agujero)
                cuadro(new Vector2(L, B), new Vector2(L, T), new Vector2(Rr, T), new Vector2(Rr, B));     // el PNG entero
            else
            {
                cuadro(new Vector2(L, B), new Vector2(L, 0f), new Vector2(Rr, 0f), new Vector2(Rr, B));        // suelo y abajo
                cuadro(new Vector2(L, 0f), new Vector2(L, H), new Vector2(-mH, H), new Vector2(-m0, 0f));     // a la izquierda del faro
                cuadro(new Vector2(m0, 0f), new Vector2(mH, H), new Vector2(Rr, H), new Vector2(Rr, 0f));     // a la derecha del faro
                cuadro(new Vector2(L, H), new Vector2(L, T), new Vector2(Rr, T), new Vector2(Rr, H));         // arriba del faro
            }
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
        mf.sharedMesh = malla;

        // Delante de Hope: mismo orden de dibujo que su sprite, uno más arriba
        if (delante && f.hope != null)
        {
            SpriteRenderer sr = f.hope.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) { render.sortingLayerID = sr.sortingLayerID; render.sortingOrder = sr.sortingOrder + 1; }
        }
    }

    // Punto del faro (u, y en unidades) → lugar en el PNG (0 a 1)
    static Vector2 AUV(float u, float y, FaroFez f, float anchoPx, float altoPx, float ppu)
    {
        float px = f.centroDibujoX + u * ppu;          // columna (desde la izquierda)
        float py = f.sueloDibujoY - y * ppu;           // fila (desde arriba)
        return new Vector2(px / anchoPx, 1f - py / altoPx);
    }

    // Una grilla de nx por ny cuadraditos (el shader dibuja las dos caras).
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
