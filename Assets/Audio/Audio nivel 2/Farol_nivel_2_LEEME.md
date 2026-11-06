# Farol de Nivel 2

Farol (5), al inicio. Copia exacta de los cinco WAV y valores actuales de nivel 1: Foley chispa/encendido/fuego (14/15/16) y Mecánicas motivo/segunda voz (24/25). Mismos scripts y mismas salidas de mixer: HopeMixerFoley y HopeMixerMecanicas, conectados al mixer general.

Los ajustes independientes se encuentran en Foley/Faroles/Resources/HopeLevel2LanternSettings y Mecánicas/Checkpoint Faroles/Resources/HopeLevel2CheckpointSettings.

El audio comienza cuando Lantern entra en Lighting tras aceptar E. Chispa inmediata; encendido y fuego a +0.1 s; motivo a +0.22 s; segunda voz a +1.0290774 s (primer farol). Fuego en loop con entrada de 0.15 s y distancia de nivel 1. Repetir E mientras está prendido no reinicia las voces. Apagar o reiniciar cancela reproducción y demoras.

Solo se reactiva este farol en Nivel_2. El resto del audio antiguo permanece deshabilitado. No modifica escenas, jugabilidad, duración del encendido, iluminación ni punto de reaparición. Los cinco ambientes continuos mantienen sus faders y su premix actuales.
