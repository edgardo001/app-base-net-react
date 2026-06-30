## ADDED Requirements

### Requirement: Componente de juego en canvas
El sistema SHALL proporcionar un componente React (`MotoJuego`) que renderice un canvas 2D con un juego de moto esquivando obstáculos en una montaña. El componente SHALL aceptar un `className` opcional para styling.

#### Scenario: Renderizado inicial del canvas
- **WHEN** el componente `MotoJuego` se monta en el DOM
- **THEN** se renderiza un elemento `<canvas>` con dimensiones mínimas de 800x400px
- **THEN** el canvas muestra el terreno montañoso, la moto en posición inicial y sin obstáculos visibles

#### Scenario: Componente se desmonta
- **WHEN** el componente se desmonta
- **THEN** el game loop se detiene y no hay fugas de memoria

### Requirement: Controles por teclado
El juego SHALL responder a las siguientes teclas:
- `ArrowLeft` / `a`: Inclinar la moto hacia atrás
- `ArrowRight` / `d`: Inclinar la moto hacia adelante
- `ArrowUp` / `w`: Acelerar
- `ArrowDown` / `s`: Frenar
- `Space`: Saltar (impulso vertical)

#### Scenario: Tecla Space aplica salto
- **WHEN** el usuario presiona Space
- **THEN** la moto recibe un impulso vertical hacia arriba si está sobre el terreno

#### Scenario: Tecla ArrowUp acelera
- **WHEN** el usuario mantiene presionada ArrowUp
- **THEN** la velocidad horizontal de la moto aumenta progresivamente hasta un límite máximo

#### Scenario: Tecla ArrowDown frena
- **WHEN** el usuario mantiene presionada ArrowDown
- **THEN** la velocidad horizontal de la moto disminuye progresivamente hasta 0

#### Scenario: Teclas ArrowLeft/ArrowRight inclinan
- **WHEN** el usuario presiona ArrowLeft
- **THEN** la moto se inclina hacia atrás rotando visualmente
- **WHEN** el usuario presiona ArrowRight
- **THEN** la moto se inclina hacia adelante rotando visualmente

### Requirement: Física y movimiento
El juego SHALL simular gravedad, velocidad e inclinación. La moto tiene posición (x, y), velocidad (vx, vy), rotación y gravedad constante. El terreno se desplaza horizontalmente simulando avance.

#### Scenario: Gravedad afecta a la moto
- **WHEN** la moto está en el aire (después de un salto)
- **THEN** la velocidad vertical aumenta por la gravedad en cada frame, haciendo que la moto caiga

#### Scenario: Moto sigue el terreno
- **WHEN** la moto está sobre el terreno
- **THEN** su posición Y se ajusta a la altura del terreno en su posición X

#### Scenario: Avance del escenario
- **WHEN** el usuario acelera
- **THEN** el terreno y los obstáculos se desplazan hacia la izquierda, simulando avance

### Requirement: Obstáculos procedurales
El juego SHALL generar obstáculos (rocas, huecos) de forma procedural a medida que el jugador avanza. Máximo 2 obstáculos visibles simultáneamente.

#### Scenario: Generación de obstáculos
- **WHEN** el jugador avanza y el obstáculo anterior sale de la pantalla por la izquierda
- **THEN** se genera un nuevo obstáculo al borde derecho de la pantalla
- **THEN** el tipo (roca/hueco) y posición se determinan aleatoriamente

#### Scenario: Colisión con obstáculo
- **WHEN** la moto choca contra una roca o cae en un hueco
- **THEN** el juego termina y se muestra "Game Over" con la puntuación obtenida

### Requirement: Sistema de puntaje
El juego SHALL llevar un puntaje que incrementa al superar obstáculos. Rango: 0-20 puntos.

#### Scenario: Obstáculo superado
- **WHEN** la moto pasa exitosamente sobre o alrededor de un obstáculo
- **THEN** el puntaje aumenta en 1

#### Scenario: Puntaje máximo alcanzado
- **WHEN** el puntaje llega a 20
- **THEN** el juego termina en estado de victoria
- **THEN** se muestra mensaje "¡Felicidades! Has completado el juego"
- **THEN** se activa la animación de fuegos artificiales

#### Scenario: Game Over muestra puntaje
- **WHEN** el juego termina por colisión
- **THEN** se muestra "Game Over" junto con el puntaje final
- **THEN** se muestra un botón "Reintentar" para reiniciar el juego

### Requirement: Fuegos artificiales
Al completar los 20 puntos, el juego SHALL mostrar una animación de fuegos artificiales en el canvas.

#### Scenario: Partículas de fuegos artificiales
- **WHEN** el jugador alcanza 20 puntos
- **THEN** se generan múltiples explosiones de partículas de colores (rojo, azul, amarillo, verde, morado, naranja)
- **THEN** cada explosión tiene ~30 partículas que se expanden radialmente, caen por gravedad y se desvanecen
- **THEN** nuevas explosiones continúan apareciendo cada ~500ms mientras dure la celebración

### Requirement: Integración en página /publico
El componente `MotoJuego` SHALL integrarse en `PublicoPage` sin eliminar ni modificar el contenido existente.

#### Scenario: Juego aparece debajo del Card
- **WHEN** un usuario navega a `/publico`
- **THEN** ve el Card de bienvenida existente
- **THEN** debajo del Card ve el canvas del juego centrado

#### Scenario: Sin dependencias externas
- **WHEN** se importa `MotoJuego` en `publico.tsx`
- **THEN** no se requiere instalar ningún paquete npm adicional
