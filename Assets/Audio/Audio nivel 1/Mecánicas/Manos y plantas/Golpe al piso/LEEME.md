# Golpe de la mano contra el piso

Descarga de aviso, barrido completo adaptado a la animacion, temblor de rocas durante el barrido e impacto de piedra/madera en el contacto.

Editar Resources/HopeHandImpactSettings.asset para ajustar los volumenes de cada capa y fraccionAviso. VolumenGeneral controla solo este evento. Todas las fuentes van al submixer Mecanicas. Durante Play, el componente HopeHandImpactAudio se agrega al objeto HandChaser y crea cuatro fuentes hijas con nombres de las capas.

Mano_Barrido_Completo_Original.wav conserva el barrido de 1.824 s. Mano_Barrido_Juego.wav recorre la fuente completa en 0.38 s para la animacion actual. El pitch de reproduccion se adapta al tiempo disponible hasta el contacto.

El impacto usa OnExtinguishHit, con proteccion para no sonar dos veces entre el evento de animacion y su temporizador de respaldo. Reiniciar la mano detiene las capas. El temblor continua al volver a perseguir. No se modifica la escena, la animacion ni el funcionamiento de los faroles.

Fuentes y tratamientos en Procedencia_Golpe_Mano.json. Falta ajustar jugando la sincronizacion perceptual y el balance.
