## Context

La página `/publico` es el landing default para usuarios no-admin (especialmente los que ingresan con Google OAuth). Actualmente solo tiene un Card de bienvenida. Se quiere añadir un minijuego 2D sin alterar el contenido existente.

El frontend usa React 19 + TypeScript + Tailwind v4. No hay experiencia previa con Canvas en el proyecto, por lo que el diseño debe ser autocontenido y no depender de librerías externas.

## Goals / Non-Goals

**Goals:**
- Componente React que renderiza un canvas 2D con el juego "moto en montaña"
- Física básica: gravedad, inclinación, aceleración, salto
- Obstáculos procedurales (rocas, huecos) generados al avanzar
- Puntaje 0-20, distancia medida en obstáculos superados
- Fuegos artificiales en canvas al alcanzar 20 puntos
- Mantener el contenido actual de `/publico` intacto

**Non-Goals:**
- No se persiste puntaje (sin backend, sin leaderboard)
- No hay niveles ni power-ups
- No hay sonido
- No hay modo multijugador
- No hay soporte táctil (solo teclado)

## Decisions

1. **Canvas API pura vs librería (Phaser, PixiJS)**: Se usa Canvas API nativa. El juego es simple (2 obstáculos simultáneos máximo), no justifica +500KB de dependencias. El render loop usa `requestAnimationFrame` con delta time.

2. **Componente React funcional vs clase**: Se usa `useRef` para el canvas y `useEffect` para el game loop. El estado del juego (posición, velocidad, puntaje) vive en closures dentro del hook, no en estado React, para evitar re-renders innecesarios.

3. **Física simplificada**: La moto tiene posición X/Y, velocidad X/Y, gravedad constante. El terreno es una serie de segmentos de línea (montaña) que se desplaza horizontalmente. La moto rota según la inclinación del terreno. Space aplica un impulso vertical hacia arriba.

4. **Sistema de puntaje**: Cada obstáculo superado exitosamente suma 1 punto. Al llegar a 20, el juego termina y se muestra "¡Felicidades!" con partículas de fuegos artificiales.

5. **Fuegos artificiales**: Sistema de partículas simple: cada "explosión" genera ~30 partículas con colores aleatorios, velocidad radial, gravedad y desvanecimiento. Se lanzan múltiples explosiones con intervalo.

6. **Layout**: El juego se coloca debajo del Card existente, centrado, con un ancho máximo de 800px. El Card actual no se modifica.

## Risks / Trade-offs

- **Rendimiento en máquinas lentas**: El game loop con requestAnimationFrame se pausa automáticamente al cambiar de pestaña. Solo hay ~2 obstáculos y ~100 partículas máximo → riesgo bajo
- **Accesibilidad**: El juego solo responde a teclado. No hay alternativa para usuarios que no pueden usar teclado. Aceptado por simplicidad del MVP
- **Colisiones imperfectas**: La detección usa AABB (axis-aligned bounding boxes) simplificado. Puede haber falsos positivos en bordes. Se acepta por tratarse de un juego casual
- **Consumo de batería en móviles**: El canvas renderiza a 60fps. Si el usuario está en laptop con batería, hay consumo moderado. Mitigación: pausar el juego si no hay foco en la ventana
