# Musica de Hope — octubre

Configuracion persistente: Resources/HopeMusicSettings.asset. Seleccionar en Unity para ajustar volumenes, anticipacion y fundido.

Main comienza al iniciar, usa el loop original de 25,6 s del pulso de Hope. Paso de nivel usa Dark Place (SkyleTheFrench, CC0; https://opengameart.org/content/dark-place-loop), desde 34,049 s, punto elegido en Reaper V2. Conservamos la pieza completa para sostener la musica si el jugador se detiene en la entrada; no reproducimos solamente los 4,324 s del montaje. Ambos canales son 2D. Ganancias iniciales tomadas de las pistas de Reaper; no se aplica la amplificacion de su master al juego.

La distancia de anticipacion es velocidad de caminar por 2 segundos (5 unidades con velocidad 2,5). Se mide hasta la puerta dibujada al extremo derecho de Foreground (pixel aproximado 4698,410 de escenario_frente.png; posicion mundial inicial 134,2245 / -3,1925); es una estimacion, no una prediccion de llegada si salta o se detiene. Fundido cruzado lineal de 2 s. Al alejarse mas de un margen de 1 unidad, vuelve Main suavemente. Al activar el final, se sostiene Paso; al volver al checkpoint, vuelve Main. La pausa por timeScale congela el fundido pero no detiene la reproduccion de audio.

La logica actual de juego muestra NIVEL2 durante 2 s y vuelve al checkpoint. Esta integracion no cambia esa logica ni carga otra escena. Cuando exista el siguiente nivel, revisar la continuidad musical entre escenas.

Probar Play: inicio, aproximacion a la cueva, retroceso, detencion cerca de la entrada, entrada y reinicio tras caer. La musica ignora el marcador NIVEL2 antiguo situado a mitad del nivel. Si cambia el dibujo de la puerta, ajustar Punto Puerta Local en la configuracion. No se movieron colliders ni el marcador de jugabilidad. En Play se crean dos fuentes bajo Pp; se eliminan al salir. No hace falta guardar la escena para esta integracion.
