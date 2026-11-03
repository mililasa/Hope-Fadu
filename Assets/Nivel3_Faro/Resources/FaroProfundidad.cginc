// Profundidad en los giros del faro.
// FaroFez.cs manda estos valores a todos los shaders a la vez (Shader.SetGlobal...).
//
// Lo que hace: una perspectiva "a la altura de los ojos de Hope". Lo que está más lejos
// de la cámara se achica hacia un punto: en X, el centro del faro; en Y, los pies de Hope.
//   - Adentro, el rincón está lejos: se achica y las líneas de los pisos forman una V.
//   - Afuera, la esquina está cerca: se agranda.
// Como el punto está a la altura de los pies de Hope, el piso donde está parada no se mueve.
// Además oscurece un poco lo que está lejos: el rincón queda en sombra.
//
// Con _FaroFuerza = 0 (todo el tiempo, menos durante un giro) no cambia nada.

#ifndef FARO_PROFUNDIDAD_INCLUDED
#define FARO_PROFUNDIDAD_INCLUDED

float4 _FaroCentro;   // x, z: centro del faro (el Pivote). y: altura de los pies de Hope
float  _FaroDist;     // "distanciaRincon": menos = más perspectiva
float  _FaroFuerza;   // 0 = nada, 1 = efecto completo (a mitad del giro)
float  _FaroSombra;   // cuánto se oscurece el rincón (0 a 1)
float  _FaroRadio;    // hasta qué distancia llega la sombra

float4 FaroPosicion(float4 vertice, out float luz)
{
    float3 p = mul(unity_ObjectToWorld, vertice).xyz;
    float atras = p.z - _FaroCentro.z;                       // + = más lejos de la cámara
    float escala = _FaroDist / max(0.5, _FaroDist + atras);  // lejos = más chico
    float e = lerp(1.0, escala, _FaroFuerza);
    p.x = _FaroCentro.x + (p.x - _FaroCentro.x) * e;
    p.y = _FaroCentro.y + (p.y - _FaroCentro.y) * e;
    luz = 1.0 - _FaroSombra * _FaroFuerza * saturate(atras / max(0.01, _FaroRadio));
    return mul(UNITY_MATRIX_VP, float4(p, 1.0));
}

#endif
