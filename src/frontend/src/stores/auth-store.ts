import { create } from 'zustand'
import api from '@/lib/api'

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
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  checkAuth: () => Promise<void>
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  permissions: [],
  isAuthenticated: !!localStorage.getItem('accessToken'),

  login: async (email, password) => {
    const { data } = await api.post('/auth/login', { email, password })
    const { accessToken, refreshToken, user: u, permissions: perms } = data.data
    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem('refreshToken', refreshToken)
    set({ user: u, permissions: perms, isAuthenticated: true })
  },

  logout: async () => {
    try {
      const refreshToken = localStorage.getItem('refreshToken')
      if (refreshToken) await api.post('/auth/logout', { refreshToken })
    } catch {
      // ignore logout errors
    }
    localStorage.clear()
    set({ user: null, permissions: [], isAuthenticated: false })
  },

  checkAuth: async () => {
    const token = localStorage.getItem('accessToken')
    if (!token) {
      set({ isAuthenticated: false })
      return
    }
    try {
      const { data } = await api.get('/profile')
      const user = data.data
      set({ user, isAuthenticated: true })
    } catch {
      localStorage.clear()
      set({ user: null, isAuthenticated: false })
    }
  },
}))
