import { useEffect, useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/auth-store'

export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  const checkAuth = useAuthStore((s) => s.checkAuth)
  const [checking, setChecking] = useState(true)

  useEffect(() => {
    checkAuth().finally(() => setChecking(false))
  }, [checkAuth])

  if (checking) return null
  if (!isAuthenticated) return <Navigate to="/login" replace />
  return <>{children}</>
}
