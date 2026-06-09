import { type FormEvent, useEffect, useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import axios from 'axios'
import api, { getErrorMessage } from '@/lib/api'
import { useAuthStore } from '@/stores/auth-store'
import { useTheme } from '@/hooks/use-theme'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { PasswordInput } from '@/components/ui/password-input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Moon, Sun, RefreshCw, WifiOff, AlertCircle } from 'lucide-react'

export function LoginPage() {
  const navigate = useNavigate()
  const login = useAuthStore((s) => s.login)
  const { theme, toggleTheme } = useTheme()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [errorType, setErrorType] = useState<'credential' | 'network' | 'locked' | 'unknown'>('unknown')
  const [loading, setLoading] = useState(false)
  const [forgotEnabled, setForgotEnabled] = useState(true)

  useEffect(() => {
    api.get('/features').then(({ data }) => {
      setForgotEnabled(data.forgotPasswordEnabled !== false)
    }).catch(() => {})
  }, [])

  const handleSubmit = async (e?: FormEvent) => {
    e?.preventDefault()
    setError('')
    setErrorType('unknown')
    setLoading(true)
    try {
      const expired = await login(email, password)
      navigate(expired ? '/change-password' : '/dashboard')
    } catch (err: unknown) {
      const msg = getErrorMessage(err, 'Error al iniciar sesión')
      setError(msg)
      if (axios.isAxiosError(err)) {
        if (!err.response) {
          setErrorType('network')
        } else if (err.response.status === 401) {
          setErrorType('credential')
        } else if (err.response.status === 423) {
          setErrorType('locked')
        }
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/50">
      <div className="fixed right-4 top-4">
        <Button variant="ghost" size="icon" onClick={toggleTheme} aria-label="Toggle theme">
          {theme === 'dark' ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
        </Button>
      </div>
      <Card className="w-full max-w-sm">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">UserMVP</CardTitle>
          <CardDescription>Ingresa con tu cuenta</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className={`rounded-md p-3 text-sm flex items-start gap-2 ${
                errorType === 'network' ? 'bg-yellow-500/10 text-yellow-700 dark:text-yellow-500' :
                errorType === 'locked' ? 'bg-orange-500/10 text-orange-700 dark:text-orange-500' :
                'bg-destructive/10 text-destructive'
              }`}>
                {errorType === 'network' ? <WifiOff className="mt-0.5 h-4 w-4 shrink-0" /> :
                 errorType === 'locked' ? <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" /> :
                 <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />}
                <div className="flex-1">
                  <p>{error}</p>
                  {errorType === 'network' && (
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      className="mt-2"
                      onClick={handleSubmit}
                    >
                      <RefreshCw className="mr-1 h-3 w-3" /> Reintentar
                    </Button>
                  )}
                </div>
              </div>
            )}
            <div className="space-y-2">
              <Label htmlFor="email">Usuario</Label>
              <Input
                id="email"
                type="text"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoFocus
                disabled={loading}
                placeholder="correo@ejemplo.com"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="password">Contraseña</Label>
              <PasswordInput
                id="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                disabled={loading}
              />
            </div>
            <Button type="submit" className="w-full" disabled={loading}>
              {loading ? 'Ingresando...' : 'Ingresar'}
            </Button>
            {forgotEnabled && (
              <div className="text-center text-sm">
                <Link to="/forgot-password" className="text-primary hover:underline">
                  ¿Olvidaste tu contraseña?
                </Link>
              </div>
            )}
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
