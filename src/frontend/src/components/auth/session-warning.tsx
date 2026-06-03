import { useEffect, useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/auth-store'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'
import axios from 'axios'

const WARNING_BEFORE = 30 // seconds

export function SessionWarning() {
  const navigate = useNavigate()
  const logout = useAuthStore((s) => s.logout)
  const [visible, setVisible] = useState(false)
  const [countdown, setCountdown] = useState(WARNING_BEFORE)

  const refreshSession = useCallback(async () => {
    const refreshToken = localStorage.getItem('refreshToken')
    if (!refreshToken) {
      setVisible(false)
      return
    }
    try {
      const { data } = await axios.post('/api/auth/refresh', { refreshToken })
      localStorage.setItem('accessToken', data.data.accessToken)
      localStorage.setItem('refreshToken', data.data.refreshToken)
      setVisible(false)
    } catch {
      logout()
      navigate('/login')
    }
  }, [logout, navigate])

  useEffect(() => {
    const token = localStorage.getItem('accessToken')
    if (!token) return

    // Parse JWT to get expiration
    try {
      const payload = JSON.parse(atob(token.split('.')[1]))
      const exp = payload.exp * 1000

      const timer = setInterval(() => {
        const now = Date.now()
        const remaining = Math.floor((exp - now) / 1000)

        if (remaining <= WARNING_BEFORE && remaining > 0) {
          setVisible(true)
          setCountdown(remaining)
        } else if (remaining <= 0) {
          setVisible(false)
          logout()
          navigate('/login')
        } else {
          setVisible(false)
        }
      }, 1000)

      return () => clearInterval(timer)
    } catch {
      // Invalid token
    }
  }, [logout, navigate])

  return visible ? (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="w-full max-w-sm">
        <CardHeader className="text-center">
          <CardTitle className="text-xl">Sesión próxima a expirar</CardTitle>
          <CardDescription>
            Tu sesión expirará en {countdown} segundos
          </CardDescription>
        </CardHeader>
        <CardContent className="text-center">
          <p className="text-sm text-muted-foreground">
            ¿Deseas continuar con la sesión activa?
          </p>
          <div className="mt-4 text-4xl font-bold text-primary">{countdown}</div>
        </CardContent>
        <CardFooter className="flex justify-center gap-3">
          <Button variant="outline" onClick={() => { logout(); navigate('/login') }}>
            Cerrar Sesión
          </Button>
          <Button onClick={refreshSession}>
            Continuar Sesión
          </Button>
        </CardFooter>
      </Card>
    </div>
  ) : null
}
