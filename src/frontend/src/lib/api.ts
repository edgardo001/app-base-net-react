import axios from 'axios'

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

// Request interceptor: inyecta el access token JWT en cada request.
// El token se obtiene de localStorage (no de Zustand) para evitar dependencia circular.
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Response interceptor: detecta 401 y ejecuta refresh token automaticamente.
// Usa axios.post (no api.post) para evitar ciclos infinitos con el interceptor mismo.
api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config
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
      if (refreshToken) {
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
    }
    return Promise.reject(error)
  },
)

export default api
