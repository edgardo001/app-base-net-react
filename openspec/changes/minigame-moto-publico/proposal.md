## Why

La página `/publico` actualmente solo muestra un mensaje de bienvenida estático. Para aumentar el engagement y ofrecer una experiencia lúdica a los usuarios no-admin, se propone añadir un minijuego arcade directamente en la página. Esto transforma `/publico` en un destino entretenido sin necesidad de permisos adicionales ni infraestructura nueva.

## What Changes

- Se añade un minijuego 2D tipo "Gravity Defied" (moto esquivando obstáculos en una montaña) en la página `/publico`
- El contenido actual de bienvenida se conserva y el juego se muestra debajo del Card existente
- Controles: flechas direccionales (← → para inclinar, ↑ para acelerar, ↓ para frenar) y Space para saltar
- Puntaje máximo de 20 puntos; al alcanzarlo se muestra felicitación con fuegos artificiales animados
- El juego es un componente JavaScript standalone con Canvas API — sin dependencias externas

## Capabilities

### New Capabilities
- `moto-juego-canvas`: Componente de juego 2D en canvas con física de moto, obstáculos generados proceduralmente, detección de colisiones, sistema de puntaje (0-20) y animación de fuegos artificiales al completar

### Modified Capabilities
- *(ninguna — cambio puramente aditivo)*

## Impact

- **Frontend**: Se crea `src/frontend/src/components/game/moto-juego.tsx` y se modifica `src/frontend/src/pages/publico.tsx` para incluir el componente
- **Dependencias**: Ninguna nueva — solo Canvas API nativa del browser
- **Rendimiento**: Impacto mínimo; el canvas se pausa al salir de la página
- **No afecta**: Backend, base de datos, rutas, permisos, ni otros componentes
