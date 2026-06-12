import { type FormEvent, useCallback, useEffect, useRef, useState } from 'react'
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
import { CaptchaWidget } from '@/components/auth/captcha-widget'
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
  const captchaTokenRef = useRef<string | null>(null)

  useEffect(() => {
    api.get('/features').then(({ data }) => {
      setForgotEnabled(data.forgotPasswordEnabled !== false)
    }).catch(() => {})
  }, [])

  const handleCaptchaToken = useCallback((token: string | null) => {
    captchaTokenRef.current = token
  }, [])

  const handleSubmit = async (e?: FormEvent) => {
    e?.preventDefault()
    setError('')
    setErrorType('unknown')
    setLoading(true)
    try {
      const expired = await login(email, password, captchaTokenRef.current ?? undefined)
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
            <CaptchaWidget onToken={handleCaptchaToken} />
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
          <div className="relative my-4">
            <div className="absolute inset-0 flex items-center">
              <span className="w-full border-t" />
            </div>
            <div className="relative flex justify-center text-xs uppercase">
              <span className="bg-card px-2 text-muted-foreground">o</span>
            </div>
          </div>
          <Button
            type="button"
            variant="outline"
            className="w-full"
            onClick={() => window.location.href = '/api/auth/google/login'}
          >
            <svg className="mr-2 h-4 w-4" viewBox="0 0 24 24">
              <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92a5.06 5.06 0 0 1-2.2 3.32v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.1z" fill="#4285F4"/>
              <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
              <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" fill="#FBBC05"/>
              <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335"/>
            </svg>
            Continuar con Google
          </Button>
          <Button
            type="button"
            variant="outline"
            className="w-full mt-2"
            onClick={() => window.location.href = '/api/auth/github/login'}
          >
            <svg className="mr-2 h-4 w-4" viewBox="0 0 24 24" fill="currentColor">
              <path d="M12 0C5.37 0 0 5.37 0 12c0 5.31 3.435 9.795 8.205 11.385.6.105.825-.255.825-.57 0-.285-.015-1.23-.015-2.235-3.015.555-3.795-.735-4.035-1.41-.135-.345-.72-1.41-1.23-1.695-.42-.225-1.02-.78-.015-.795.945-.015 1.62.87 1.845 1.23 1.08 1.815 2.805 1.305 3.495.99.105-.78.42-1.305.765-1.605-2.67-.3-5.46-1.335-5.46-5.925 0-1.305.465-2.385 1.23-3.225-.12-.3-.54-1.53.12-3.18 0 0 1.005-.315 3.3 1.23.96-.27 1.98-.405 3-.405s2.04.135 3 .405c2.295-1.56 3.3-1.23 3.3-1.23.66 1.65.24 2.88.12 3.18.765.84 1.23 1.905 1.23 3.225 0 4.605-2.805 5.625-5.475 5.925.435.375.81 1.095.81 2.22 0 1.605-.015 2.895-.015 3.3 0 .315.225.69.825.57A12.02 12.02 0 0 0 24 12c0-6.63-5.37-12-12-12z"/>
            </svg>
            Continuar con GitHub
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}
