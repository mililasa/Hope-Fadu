// Profundidad en los giros del faro.
// FaroFez.cs manda estos valores a todos los shaders a la vez (Shader.SetGlobal...).
//
// La cámara es ortográfica: sin ayuda, una esquina de adentro y una de afuera se ven
// IGUALES. Durante el giro, esto agrega perspectiva:
//   - Lo que está a la distancia de la pared de siempre, o más cerca, no cambia: la torre no se
//     abre ni se achica entera (no rebota), sigue con su forma inclinada.
//   - Lo que está más lejos se achica hacia el centro del faro y hacia los pies de Hope:
//     el rincón de adentro se aleja, las repisas forman una V y se oscurece un poco.
//   - Como se achica hacia los pies de Hope, el piso donde está parada queda derecho.
// Con _FaroAlto = 0 solo se achica de costado (en X).
//
// Con _FaroFuerza = 0 (todo el tiempo, menos durante un giro) no cambia nada.

#ifndef FARO_PROFUNDIDAD_INCLUDED
#define FARO_PROFUNDIDAD_INCLUDED

float4 _FaroCentro;   // x, z: centro del faro (el Pivote). y: altura de los pies de Hope. w: altura del suelo del faro
float4 _FaroMedidas;  // x: medio ancho abajo, y: medio ancho arriba, z: altura del techo, w: medio grosor del muro
float  _FaroLado;     // +1 = el giro es adentro (la pared está lejos), -1 = afuera (la pared está cerca)
float  _FaroAlto;     // 0 = solo de costado, 1 = también en Y
float  _FaroDist;     // "perspectivaRincon", en medios anchos: menos = el rincón se aleja más
float  _FaroFuerza;   // 0 = nada, 1 = efecto completo (a mitad del giro)
float  _FaroSombra;   // cuánto se oscurece el rincón (0 a 1)
float  _FaroRadio;    // cuánto más lejos que la pared tiene que estar algo para quedar en sombra del todo

float4 FaroPosicion(float4 vertice, out float luz)
{
    float3 p = mul(unity_ObjectToWorld, vertice).xyz;
    float atras = p.z - _FaroCentro.z;                       // + = más lejos de la cámara
    // Todo se mide en "medios anchos" a la altura del punto (el faro es más angosto arriba):
    // así la perspectiva es igual abajo y arriba, y la forma inclinada se respeta.
    float t = saturate((p.y - _FaroCentro.w) / max(0.01, _FaroMedidas.z));
    float medio = max(0.01, lerp(_FaroMedidas.x, _FaroMedidas.y, t));
    float z = atras / medio;                                 // 1 = a la distancia de la pared de siempre (adentro)
    float d = max(0.05, _FaroDist);
    // Lo que está a la distancia de la pared de siempre (o más cerca) queda igual: la torre no se abre.
    // Lo que está más lejos (el rincón) se achica hacia el centro del faro y hacia los pies de Hope.
    float escala = min(1.0, max(0.3 * d, d + _FaroLado) / max(0.3 * d, d + z));
    float e = lerp(1.0, escala, _FaroFuerza);
    p.x = _FaroCentro.x + (p.x - _FaroCentro.x) * e;
    if (_FaroAlto > 0.5) p.y = _FaroCentro.y + (p.y - _FaroCentro.y) * e;
    float pared = _FaroLado * medio - _FaroMedidas.w;
    luz = 1.0 - _FaroSombra * _FaroFuerza * saturate((atras - pared) / max(0.01, _FaroRadio));
    return mul(UNITY_MATRIX_VP, float4(p, 1.0));
}

#endif
