import axios, { type AxiosError } from 'axios'

// CSRF token generado al iniciar la app: UUID aleatorio usado como token stateless.
// El backend verifica este header en mutaciones (POST/PUT/PATCH/DELETE) como proteccion CSRF.
// No se necesita cookie ni session: el token es un secreto compartido via JS, inaccesible para otros origenes.
const csrfToken = crypto.randomUUID()

// Endpoints de autenticacion excluidos del CSRF (no tienen sesion establecida aun).
const csrfExcludedPaths = ['/auth/login', '/auth/forgot-password', '/auth/reset-password', '/auth/confirm-email', '/auth/refresh', '/auth/logout', '/health']

function isCsrfExcluded(url: string): boolean {
  return csrfExcludedPaths.some((path) => url.includes(path))
}

// Axios instance preconfigurada con baseURL para evitar repetir /api en cada componente.
// En desarrollo, Vite proxy redirige /api a localhost:5011.
// En produccion, /api apunta al mismo origen o al dominio del backend via nginx/Traefik.
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api',
  headers: { 'Content-Type': 'application/json' },
})

// Cola de reintentos para requests fallidas durante un refresh token en progreso.
// Sin esto, N requests simultaneas con 401 generarian N llamadas de refresh.
// La cola asegura una sola llamada de refresh; las demas esperan y se reejecutan con el nuevo token.
let isRefreshing = false
let failedQueue: Array<{
  resolve: (token: string) => void
  reject: (error: unknown) => void
}> = []

function processQueue(error: unknown, token: string | null = null) {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) {
      reject(error)
    } else {
      resolve(token!)
    }
  })
  failedQueue = []
}

// Request interceptor: inyecta access token JWT y CSRF token en mutaciones.
// El token se obtiene de localStorage (no de Zustand) para evitar dependencia circular.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  const method = config.method?.toUpperCase() ?? ''
  if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(method) && config.url && !isCsrfExcluded(config.url)) {
    config.headers['X-CSRF-TOKEN'] = csrfToken
  }
  return config
})

// Response interceptor: detecta 401 y ejecuta refresh token automaticamente.
// Usa axios.post (no api.post) para evitar ciclos infinitos con el interceptor mismo.
// IMPORTANTE: skip para endpoints de auth (login/forgot/reset) donde 401 es esperado.
api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config
    const url: string = original?.url || ''

    // No interceptor para endpoints de autenticacion (401 = credenciales invalidas, no token expirado)
    if (url.includes('/auth/login') || url.includes('/auth/forgot-password') || url.includes('/auth/reset-password')) {
      return Promise.reject(error)
    }

    if (error.response?.status === 401 && !original._retry) {
      if (isRefreshing) {
        // Ya hay un refresh en progreso: encolar esta request para reintentar despues.
        return new Promise<string>((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        }).then((token) => {
          original.headers.Authorization = `Bearer ${token}`
          return api(original)
        })
      }
      original._retry = true
      isRefreshing = true
      const refreshToken = localStorage.getItem('refreshToken')
      if (!refreshToken) {
        // Sin refresh token: limpiar estado y rechazar.
        isRefreshing = false
        localStorage.clear()
        window.location.href = '/login'
        return Promise.reject(error)
      }
      try {
        const { data } = await axios.post('/api/auth/refresh', { refreshToken })
        const newToken = data.data.accessToken
        localStorage.setItem('accessToken', newToken)
        localStorage.setItem('refreshToken', data.data.refreshToken)
        processQueue(null, newToken)
        original.headers.Authorization = `Bearer ${newToken}`
        return api(original)
      } catch (err) {
        // Refresh fallo (token expirado o revocado): limpiar y redirigir a login.
        processQueue(err, null)
        localStorage.clear()
        window.location.href = '/login'
        return Promise.reject(err)
      } finally {
        isRefreshing = false
      }
    }
    return Promise.reject(error)
  },
)

export default api

/**
 * Extrae un mensaje legible para el usuario desde un error de Axios.
 * Maneja errores de red (backend caído), respuestas con body, y códigos HTTP específicos.
 */
export function getErrorMessage(err: unknown, fallback: string): string {
  if (axios.isAxiosError(err)) {
    const axiosErr = err as AxiosError<{ message?: string; errors?: Array<{ message: string }> }>

    // Error de red: backend caído, CORS, DNS, timeout
    if (!axiosErr.response) {
      if (axiosErr.code === 'ECONNABORTED' || axiosErr.code === 'ERR_NETWORK') {
        return 'No se pudo conectar con el servidor. Verifica que el servicio esté disponible.'
      }
      return 'No se pudo conectar con el servidor.'
    }

    const data = axiosErr.response.data
    if (data?.message) return data.message
    if (data?.errors?.length) return data.errors.map(e => e.message).join(', ')

    // Códigos HTTP específicos sin body parseado
    switch (axiosErr.response.status) {
      case 401: return 'Correo o contraseña incorrectos.'
      case 403: return 'Acceso denegado. No tienes permisos para realizar esta acción.'
      case 423: return 'Cuenta bloqueada. Intenta de nuevo más tarde.'
      case 429: return 'Demasiadas solicitudes. Intenta de nuevo más tarde.'
      case 500: return 'Error interno del servidor.'
    }
  }
  return fallback
}
