import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { extractRoles, extractPermissions } from '@/lib/jwt'
import { useAuthStore } from '@/stores/auth-store'

export function OAuthCallbackPage() {
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)
  const processed = useRef(false)

  useEffect(() => {
    // React StrictMode double-invokes effects in dev mode; guard ensures
    // we only process the hash fragment once, even under double-invocation.
    if (processed.current) return
    processed.current = true

    const params = new URLSearchParams(window.location.hash.replace('#', '?'))
    const accessToken = params.get('accessToken')
    const refreshToken = params.get('refreshToken')

    const errorParam = new URLSearchParams(window.location.search).get('error')

    if (errorParam) {
      setError(errorParam)
      setTimeout(() => navigate('/login?error=google_auth_failed'), 2000)
      return
    }

    if (!accessToken || !refreshToken) {
      navigate('/login')
      return
    }

    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem('refreshToken', refreshToken)

    try {
      const payload = JSON.parse(atob(accessToken.split('.')[1]))
      useAuthStore.setState({
        user: {
          id: payload.sub,
          email: payload.email,
          firstName: payload.firstName || '',
          lastName: payload.lastName || '',
          avatarPath: null,
        },
        permissions: extractPermissions(accessToken),
        roles: extractRoles(accessToken),
        isAuthenticated: true,
      })
    } catch {
      navigate('/login')
      return
    }

    navigate('/publico', { replace: true })
  }, [navigate])

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <p className="text-destructive">Error de autenticación: {error}</p>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen items-center justify-center">
      <p className="text-muted-foreground">Completando inicio de sesión...</p>
    </div>
  )
}
