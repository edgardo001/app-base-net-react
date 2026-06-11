## 1. Componente de juego — MotoJuego

- [x] 1.1 Crear archivo `src/frontend/src/components/game/moto-juego.tsx` con estructura React + useRef + useEffect
- [x] 1.2 Implementar game loop con requestAnimationFrame y delta time
- [x] 1.3 Dibujar terreno montañoso con segmentos de línea y gradiente de cielo/fondo
- [x] 1.4 Dibujar la moto como figura geométrica simple (triángulo/rectángulo rotado)
- [x] 1.5 Implementar física: gravedad, posición, velocidad, rotación
- [x] 1.6 Implementar controls: keydown/keyup para ArrowLeft/Right/Up/Down y Space
- [x] 1.7 Implementar detección de colisiones AABB con obstáculos

## 2. Obstáculos y sistema de puntaje

- [x] 2.1 Implementar generación procedural de obstáculos (rocas rectangulares y huecos en el terreno)
- [x] 2.2 Implementar desplazamiento de obstáculos con el terreno
- [x] 2.3 Implementar sistema de puntaje: +1 por obstáculo superado, máx 20
- [x] 2.4 Mostrar puntaje en HUD dentro del canvas
- [x] 2.5 Implementar estado de Game Over con mensaje y botón "Reintentar"
- [x] 2.6 Implementar estado de Victoria al llegar a 20 puntos

## 3. Fuegos artificiales

- [x] 3.1 Implementar sistema de partículas para explosiones
- [x] 3.2 Generar múltiples explosiones automáticas al activar la celebración
- [x] 3.3 Asegurar que las partículas se desvanezcan y caigan con gravedad

## 4. Integración en /publico

- [x] 4.1 Importar y renderizar `MotoJuego` en `publico.tsx` debajo del Card existente
- [x] 4.2 Verificar que el contenido actual (Card de bienvenida) se mantiene intacto
- [x] 4.3 Ejecutar `npm run build` en frontend para verificar TypeScript y build

## 5. Verificación final

- [x] 5.1 Probar juego en navegador: controles, físicas, colisiones
- [x] 5.2 Verificar que al llegar a 20 puntos se muestra celebración con fuegos artificiales
- [x] 5.3 Verificar Game Over y botón Reintentar
- [x] 5.4 Verificar que no hay regresiones en la página /publico
