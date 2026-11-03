using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[DisallowMultipleComponent]
public sealed class HopeHangingBodiesSway : MonoBehaviour
{
    [SerializeField, Range(0f, 4f)] float amplitud = 1.6f;

    readonly List<Sprite> recortes = new List<Sprite>();
    readonly Transform[] sogas = new Transform[3];
    SpriteRenderer dibujo;
    Sprite original;

    void Start()
    {
        dibujo = GetComponent<SpriteRenderer>();
        original = dibujo.sprite;
        if (original == null) return;

        dibujo.sprite = Recortar(Fondo);
        for (int i = 0; i < sogas.Length; i++)
        {
            var soga = new GameObject("Soga " + (i + 1)).transform;
            soga.SetParent(transform, false);
            Vector2 escala = original.rect.size / new Vector2(4760f, 480f);
            Vector2 pixel = new Vector2(Anclajes[i] * escala.x, original.rect.height);
            soga.localPosition = (pixel - original.pivot) / original.pixelsPerUnit;
            sogas[i] = soga;

            var capa = new GameObject("Cuerpo").AddComponent<SpriteRenderer>();
            capa.transform.SetParent(soga, false);
            capa.transform.localPosition = -soga.localPosition;
            capa.sprite = Recortar(Cuerpos[i]);
            capa.sharedMaterial = dibujo.sharedMaterial;
            capa.color = dibujo.color;
            capa.sortingLayerID = dibujo.sortingLayerID;
            capa.sortingOrder = dibujo.sortingOrder;
            capa.maskInteraction = dibujo.maskInteraction;
        }
    }

    Sprite Recortar(RectInt[] rectangulos)
    {
        Sprite sprite = Instantiate(original);
        var vertices = new Vector2[rectangulos.Length * 4];
        var indices = new ushort[rectangulos.Length * 6];
        Vector2 escala = original.rect.size / new Vector2(4760f, 480f);
        for (int i = 0; i < rectangulos.Length; i++)
        {
            RectInt r = rectangulos[i];
            float izquierda = r.xMin * escala.x, derecha = r.xMax * escala.x;
            float arriba = (480 - r.yMin) * escala.y, abajo = (480 - r.yMax) * escala.y;
            int v = i * 4, t = i * 6;
            vertices[v] = new Vector2(izquierda, abajo);
            vertices[v + 1] = new Vector2(izquierda, arriba);
            vertices[v + 2] = new Vector2(derecha, arriba);
            vertices[v + 3] = new Vector2(derecha, abajo);
            indices[t] = (ushort)v;
            indices[t + 1] = (ushort)(v + 1);
            indices[t + 2] = (ushort)(v + 2);
            indices[t + 3] = (ushort)v;
            indices[t + 4] = (ushort)(v + 2);
            indices[t + 5] = (ushort)(v + 3);
        }
        sprite.OverrideGeometry(vertices, indices);
        recortes.Add(sprite);
        return sprite;
    }

    void LateUpdate()
    {
        float tiempo = Time.time;
        float viento = 0.74f + 0.26f * Mathf.Sin(tiempo * .35f);
        for (int i = 0; i < sogas.Length; i++)
        {
            if (sogas[i] == null) continue;
            float fase = tiempo * (1.16f + i * .11f) + i * 1.7f;
            float angulo = amplitud * viento * Mathf.Sin(fase);
            sogas[i].localRotation = Quaternion.Euler(0f, 0f, angulo);
        }
    }

    void OnDestroy()
    {
        foreach (Sprite recorte in recortes) Destroy(recorte);
    }

    static readonly int[] Anclajes = { 339, 547, 747 };
    static readonly RectInt[] Fondo = new RectInt[]
        {
            new RectInt(0, 66, 328, 7),
            new RectInt(0, 99, 328, 3),
            new RectInt(0, 56, 329, 10),
            new RectInt(0, 73, 329, 10),
            new RectInt(0, 84, 329, 4),
            new RectInt(0, 96, 329, 3),
            new RectInt(0, 102, 329, 1),
            new RectInt(0, 53, 330, 3),
            new RectInt(0, 83, 330, 1),
            new RectInt(0, 88, 330, 8),
            new RectInt(0, 103, 330, 2),
            new RectInt(0, 50, 331, 3),
            new RectInt(0, 105, 331, 3),
            new RectInt(0, 46, 332, 4),
            new RectInt(0, 108, 332, 2),
            new RectInt(0, 44, 333, 2),
            new RectInt(0, 43, 334, 1),
            new RectInt(0, 42, 335, 1),
            new RectInt(0, 41, 336, 1),
            new RectInt(0, 39, 337, 1),
            new RectInt(0, 38, 338, 1),
            new RectInt(0, 110, 338, 1),
            new RectInt(0, 0, 339, 38),
            new RectInt(0, 40, 339, 1),
            new RectInt(0, 111, 339, 6),
            new RectInt(0, 117, 351, 8),
            new RectInt(0, 137, 537, 1),
            new RectInt(0, 136, 538, 1),
            new RectInt(0, 134, 539, 2),
            new RectInt(0, 133, 540, 1),
            new RectInt(0, 131, 541, 2),
            new RectInt(0, 125, 542, 6),
            new RectInt(0, 138, 745, 1),
            new RectInt(0, 139, 746, 7),
            new RectInt(0, 146, 747, 1),
            new RectInt(0, 147, 4760, 333),
            new RectInt(340, 0, 207, 1),
            new RectInt(350, 1, 197, 1),
            new RectInt(351, 2, 196, 12),
            new RectInt(352, 110, 188, 5),
            new RectInt(352, 115, 189, 8),
            new RectInt(352, 123, 190, 2),
            new RectInt(352, 14, 195, 29),
            new RectInt(353, 87, 179, 2),
            new RectInt(353, 82, 180, 5),
            new RectInt(353, 89, 180, 4),
            new RectInt(353, 74, 181, 8),
            new RectInt(353, 65, 182, 9),
            new RectInt(353, 56, 183, 9),
            new RectInt(353, 55, 184, 1),
            new RectInt(353, 93, 184, 6),
            new RectInt(353, 54, 185, 1),
            new RectInt(353, 99, 185, 1),
            new RectInt(353, 100, 186, 2),
            new RectInt(353, 53, 187, 1),
            new RectInt(353, 102, 187, 8),
            new RectInt(353, 52, 189, 1),
            new RectInt(353, 50, 190, 2),
            new RectInt(353, 48, 191, 2),
            new RectInt(353, 47, 192, 1),
            new RectInt(353, 46, 193, 1),
            new RectInt(353, 43, 194, 3),
            new RectInt(539, 137, 206, 1),
            new RectInt(541, 136, 204, 1),
            new RectInt(542, 135, 203, 1),
            new RectInt(548, 0, 199, 45),
            new RectInt(551, 45, 196, 1),
            new RectInt(552, 46, 195, 1),
            new RectInt(553, 47, 194, 1),
            new RectInt(554, 115, 183, 2),
            new RectInt(554, 117, 184, 2),
            new RectInt(554, 119, 191, 7),
            new RectInt(554, 48, 193, 2),
            new RectInt(555, 103, 179, 7),
            new RectInt(555, 110, 180, 3),
            new RectInt(555, 113, 181, 2),
            new RectInt(555, 126, 190, 3),
            new RectInt(555, 50, 192, 2),
            new RectInt(556, 100, 178, 3),
            new RectInt(556, 129, 189, 2),
            new RectInt(556, 52, 191, 1),
            new RectInt(557, 131, 188, 1),
            new RectInt(557, 53, 190, 3),
            new RectInt(558, 87, 177, 8),
            new RectInt(558, 98, 177, 2),
            new RectInt(558, 132, 187, 1),
            new RectInt(558, 56, 189, 2),
            new RectInt(559, 84, 175, 1),
            new RectInt(559, 80, 176, 4),
            new RectInt(559, 85, 176, 2),
            new RectInt(559, 95, 176, 3),
            new RectInt(559, 69, 178, 3),
            new RectInt(559, 67, 179, 2),
            new RectInt(559, 66, 180, 1),
            new RectInt(559, 65, 181, 1),
            new RectInt(559, 133, 186, 1),
            new RectInt(559, 58, 188, 7),
            new RectInt(560, 78, 175, 2),
            new RectInt(560, 77, 176, 1),
            new RectInt(560, 72, 177, 1),
            new RectInt(560, 75, 177, 2),
            new RectInt(560, 73, 178, 2),
            new RectInt(560, 134, 185, 1),
            new RectInt(748, 0, 4012, 68),
            new RectInt(748, 70, 4012, 1),
            new RectInt(748, 141, 4012, 6),
            new RectInt(749, 68, 4011, 1),
            new RectInt(749, 140, 4011, 1),
            new RectInt(750, 69, 4010, 1),
            new RectInt(751, 71, 4009, 1),
            new RectInt(752, 72, 4008, 1),
            new RectInt(753, 73, 4007, 1),
            new RectInt(754, 74, 4006, 2),
            new RectInt(755, 76, 4005, 4),
            new RectInt(755, 138, 4005, 2),
            new RectInt(756, 80, 4004, 3),
            new RectInt(756, 135, 4004, 3),
            new RectInt(757, 83, 4003, 3),
            new RectInt(757, 113, 4003, 1),
            new RectInt(757, 118, 4003, 8),
            new RectInt(757, 133, 4003, 2),
            new RectInt(758, 86, 4002, 10),
            new RectInt(758, 103, 4002, 10),
            new RectInt(758, 114, 4002, 4),
            new RectInt(758, 126, 4002, 3),
            new RectInt(758, 132, 4002, 1),
            new RectInt(759, 96, 4001, 7),
            new RectInt(759, 129, 4001, 3),
        };
    static readonly RectInt[][] Cuerpos =
    {
        new RectInt[]
        {
            new RectInt(328, 66, 25, 7),
            new RectInt(328, 99, 25, 3),
            new RectInt(329, 56, 24, 10),
            new RectInt(329, 73, 24, 10),
            new RectInt(329, 84, 24, 4),
            new RectInt(329, 96, 24, 3),
            new RectInt(329, 102, 24, 1),
            new RectInt(330, 53, 23, 3),
            new RectInt(330, 83, 23, 1),
            new RectInt(330, 88, 23, 8),
            new RectInt(330, 103, 23, 2),
            new RectInt(331, 50, 22, 3),
            new RectInt(331, 105, 22, 3),
            new RectInt(332, 46, 21, 4),
            new RectInt(332, 108, 21, 2),
            new RectInt(333, 44, 20, 2),
            new RectInt(334, 43, 19, 1),
            new RectInt(335, 42, 17, 1),
            new RectInt(336, 41, 16, 1),
            new RectInt(337, 39, 15, 1),
            new RectInt(338, 38, 14, 1),
            new RectInt(338, 110, 14, 1),
            new RectInt(339, 0, 1, 1),
            new RectInt(339, 1, 11, 1),
            new RectInt(339, 2, 12, 12),
            new RectInt(339, 14, 13, 24),
            new RectInt(339, 40, 13, 1),
            new RectInt(339, 111, 13, 6),
            new RectInt(351, 117, 1, 8),
        },
        new RectInt[]
        {
            new RectInt(532, 87, 26, 2),
            new RectInt(533, 89, 25, 4),
            new RectInt(533, 82, 26, 5),
            new RectInt(534, 80, 25, 2),
            new RectInt(534, 74, 26, 6),
            new RectInt(535, 65, 24, 7),
            new RectInt(535, 72, 25, 2),
            new RectInt(536, 56, 22, 2),
            new RectInt(536, 58, 23, 7),
            new RectInt(537, 137, 2, 1),
            new RectInt(537, 55, 20, 1),
            new RectInt(537, 93, 21, 2),
            new RectInt(537, 98, 21, 1),
            new RectInt(537, 95, 22, 3),
            new RectInt(538, 136, 3, 1),
            new RectInt(538, 54, 19, 1),
            new RectInt(538, 99, 20, 1),
            new RectInt(539, 135, 3, 1),
            new RectInt(539, 100, 17, 2),
            new RectInt(539, 134, 21, 1),
            new RectInt(540, 103, 15, 12),
            new RectInt(540, 102, 16, 1),
            new RectInt(540, 53, 17, 1),
            new RectInt(540, 133, 19, 1),
            new RectInt(541, 115, 13, 8),
            new RectInt(541, 131, 16, 1),
            new RectInt(541, 132, 17, 1),
            new RectInt(542, 123, 12, 3),
            new RectInt(542, 126, 13, 3),
            new RectInt(542, 52, 14, 1),
            new RectInt(542, 129, 14, 2),
            new RectInt(543, 50, 12, 2),
            new RectInt(544, 48, 10, 2),
            new RectInt(545, 47, 8, 1),
            new RectInt(546, 46, 6, 1),
            new RectInt(547, 0, 1, 45),
            new RectInt(547, 45, 4, 1),
        },
        new RectInt[]
        {
            new RectInt(734, 84, 23, 1),
            new RectInt(734, 103, 24, 7),
            new RectInt(734, 100, 25, 3),
            new RectInt(735, 78, 20, 2),
            new RectInt(735, 80, 21, 3),
            new RectInt(735, 83, 22, 1),
            new RectInt(735, 85, 22, 1),
            new RectInt(735, 86, 23, 10),
            new RectInt(735, 110, 23, 3),
            new RectInt(735, 96, 24, 4),
            new RectInt(736, 77, 19, 1),
            new RectInt(736, 113, 21, 1),
            new RectInt(736, 114, 22, 1),
            new RectInt(737, 70, 11, 1),
            new RectInt(737, 69, 13, 1),
            new RectInt(737, 71, 14, 1),
            new RectInt(737, 72, 15, 1),
            new RectInt(737, 75, 17, 1),
            new RectInt(737, 76, 18, 1),
            new RectInt(737, 115, 21, 2),
            new RectInt(738, 67, 10, 1),
            new RectInt(738, 68, 11, 1),
            new RectInt(738, 73, 15, 1),
            new RectInt(738, 74, 16, 1),
            new RectInt(738, 118, 19, 1),
            new RectInt(738, 117, 20, 1),
            new RectInt(739, 66, 9, 1),
            new RectInt(740, 65, 8, 1),
            new RectInt(745, 138, 10, 1),
            new RectInt(745, 135, 11, 3),
            new RectInt(745, 119, 12, 7),
            new RectInt(745, 133, 12, 2),
            new RectInt(745, 126, 13, 3),
            new RectInt(745, 132, 13, 1),
            new RectInt(745, 129, 14, 3),
            new RectInt(746, 141, 2, 5),
            new RectInt(746, 140, 3, 1),
            new RectInt(746, 139, 9, 1),
            new RectInt(747, 0, 1, 65),
            new RectInt(747, 146, 1, 1),
        }
    };
}
