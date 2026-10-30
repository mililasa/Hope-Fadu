# Foley - Pila cadaveres

Editar Resources/HopeCorpsePileSettings.asset. Volumen Contactos controla todos los apoyos. Capas Contacto separa Carne y piel, Peso, Humedad, Ropa, Compresion y Despegue. Cada capa tiene su volumen y cuatro archivos con volumen individual. Los valores 1 son el balance de la prueba aprobada; 0 silencia y 2 permite subir hasta 6 dB una capa o archivo. Volumen Respiraciones y los cinco volumenes de voces son independientes.

Las respiraciones se escuchan al acercarse, desde 6 unidades del borde del dibujo, con entrada de 1.1 s y salida de 1.3 s. Se alternan aleatoriamente, sin repetir la misma seguida, con un maximo de dos voces a la vez. Distancia Inicio, Distancia Plena, Fade Entrada/Salida e intervalos son ajustables.

Al entrar en Play se agrega HopeCorpsePileAudio a pila_cadaver_0. Los apoyos se activan por contactos reales con Piso8 dentro de los limites de la pila; el collider continuo que viene antes conserva tierra. El tramo de Piso9 que pasa por debajo tampoco usa carne. Los aterrizajes despues de saltar usan estos mismos apoyos; los saltos y el doble salto en el aire conservan sus sonidos. Una proteccion evita duplicar el primer paso y el aterrizaje. Al salir de la pila vuelven los pasos normales.

Los WAV de cada variante incluyen sus pequenos desplazamientos temporales. Sus seis capas se programan juntas en el reloj de audio. Todos los AudioSource se asignan al mixer Foley. No se modifican la escena ni los colliders de juego. Se conservan respaldos de los dos scripts de audio editados en el workspace.

Primera implementacion para probar en Play: acercarse, saltar sobre varios escalones, caminar arriba, salir, volver y reaparecer en checkpoint. Fuentes en Procedencia_Pila.json.
