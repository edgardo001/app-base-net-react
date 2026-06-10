import { create } from 'zustand'
import api from '@/lib/api'
import { extractRoles, extractPermissions } from '@/lib/jwt'

interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  avatarPath: string | null
}

interface AuthState {
  user: User | null
  permissions: string[]
  roles: string[]
  isAuthenticated: boolean
  passwordExpired: boolean
  login: (email: string, password: string) => Promise<boolean>
  logout: () => Promise<void>
  checkAuth: () => Promise<void>
}

// Store unico de autenticacion con Zustand. Sin providers ni wrappers necesarios.
// El estado se inicializa desde localStorage sincronicamente para evitar flash de contenido no autenticado.
// Selector-based subscriptions: componentes usan useAuthStore((s) => s.user) para re-render solo cuando user cambia.
export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  roles: extractRoles(localStorage.getItem('accessToken')),
  permissions: extractPermissions(localStorage.getItem('accessToken')),
  isAuthenticated: !!localStorage.getItem('accessToken'),
  passwordExpired: false,

  // login: retorna true si el password esta expirado (obliga a cambio de password).
  // Guarda tokens en localStorage para que el Axios interceptor (api.ts) los use en requests subsecuentes.
  login: async (email, password) => {
    const { data } = await api.post('/auth/login', { email, password })
    const { accessToken, refreshToken, user: u, permissions: perms, passwordExpired } = data.data
    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem('refreshToken', refreshToken)
    set({
      user: u,
      permissions: perms,
      roles: extractRoles(accessToken),
      isAuthenticated: true,
      passwordExpired: !!passwordExpired,
    })
    return !!passwordExpired
  },

  // logout: intenta revocar el refresh token en backend, luego limpia storage local.
  // Ignora errores de red (el token se limpia igual).
  logout: async () => {
    try {
      const refreshToken = localStorage.getItem('refreshToken')
      if (refreshToken) await api.post('/auth/logout', { refreshToken })
    } catch {
      // ignore logout errors
    }
    localStorage.clear()
    set({ user: null, permissions: [], roles: [], isAuthenticated: false })
  },

  // checkAuth: verifica si el token actual aun es valido consultando /profile.
  // Se llama al cargar la app (App.tsx) para restaurar sesion tras un refresh de pagina (F5).
  checkAuth: async () => {
    const token = localStorage.getItem('accessToken')
    if (!token) {
      set({ isAuthenticated: false })
      return
    }
    try {
      const { data } = await api.get('/profile')
      const user = data.data
      set({ user, roles: extractRoles(token), permissions: extractPermissions(token), isAuthenticated: true })
    } catch {
      set({ isAuthenticated: false })
    }
  },
}))
