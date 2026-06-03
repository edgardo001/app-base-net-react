## Context

Frontend completo para el sistema de gestión de usuarios. SPA con React 19 + Vite + TypeScript + Tailwind CSS v4. UI components con @base-ui/react (shadcn/ui-compatible). Estado global con Zustand. HTTP con Axios.

El diseño sigue las especificaciones del planInicial.ia.md: layout con sidebar colapsable, 9 páginas, auth store, API client con interceptor de refresh, y session warning modal.

## Goals / Non-Goals

**Goals:**
- SPA funcional con todas las páginas del planInicial.ia.md
- Zustand store para estado de autenticación
- Axios con interceptors para manejo de tokens
- Layout responsive con sidebar + header
- Validación de formularios con react-hook-form + Zod
- Modal de expiración de sesión
- Protección de rutas

**Non-Goals:**
- Avatar upload (pendiente)
- Webcam capture (pendiente)
- Dark mode toggle (pendiente)
- Toast notifications (pendiente)
- Permissions page full implementation (stub actual)
- Tipo-A/B/C pages full implementation (stubs actuales)

## Decisions

### Zustand vs React Context
**Decisión:** Zustand para auth store.
**Alternativa:** React Context + useReducer.
**Razón:** Zustand evita providers anidados, tiene selectores eficientes (sin re-renders innecesarios), y persiste fuera del árbol React. La inicialización síncrona desde localStorage evita el flash de contenido no autenticado.

### Axios vs fetch
**Decisión:** Axios con interceptors.
**Alternativa:** fetch nativo + wrapper.
**Razón:** Axios tiene interceptors built-in para request/response, manejo de errores más limpio, y la cola de refresh concurrente es trivial de implementar con Axios. fetch requeriría un wrapper personalizado para lograr el mismo comportamiento.

### @base-ui/react vs Radix
**Decisión:** @base-ui/react (la nueva versión de Radix UI).
**Alternativa:** @radix-ui/react-primitives.
**Razón:** @base-ui/react es el sucesor de Radix UI por el mismo equipo, con mejor accesibilidad built-in y API más moderna. Los componentes shadcn/ui se adaptan fácilmente.

### react-hook-form + Zod vs formularios manuales
**Decisión:** react-hook-form con Zod resolver para formularios complejos.
**Alternativa:** useState + validación manual.
**Razón:** react-hook-form maneja registration, validation, errors, y dirty state. Zod schema valida tanto en runtime como en TypeScript. Login usa estado simple (no necesita react-hook-form).

### Modal inline vs portal
**Decisión:** Modal como div fixed inline en el DOM.
**Alternativa:** Portal a document.body.
**Razón:** Simplicidad. Para el MVP, un modal inline con z-50 es suficiente. Se puede migrar a portal si surgen problemas de stacking.

## Risks / Trade-offs

- **Riesgo: Session warning no refresca timer** → Tras refrescar el token, el countdown sigue basado en el exp del token anterior. Mitigación: reparar el componente para que re-parse el nuevo JWT después del refresh.
- **Riesgo: Sin toast notifications** → errores y confirmaciones usan alert() y confirm(). Mitigación: agregar sonner o similar en próximo cambio.
- **Riesgo: Sin lazy loading** → Todas las páginas se cargan en el bundle inicial. Mitigación: agregar React.lazy() en próximo cambio.
- **Riesgo: Sin manejo de errores de red en login** → Si el backend no responde, el error genérico "Error al iniciar sesión" se muestra. Mitigación: detectar err.code === 'ERR_NETWORK' y mostrar mensaje específico.
