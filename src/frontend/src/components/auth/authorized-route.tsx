import { Navigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/auth-store'

interface AuthorizedRouteProps {
  children: React.ReactNode
  requiredPermission?: string
  fallback?: string
}

export function AuthorizedRoute({ children, requiredPermission, fallback = '/dashboard' }: AuthorizedRouteProps) {
  const permissions = useAuthStore((s) => s.permissions)

  if (!requiredPermission) return <>{children}</>
  if (permissions.includes(requiredPermission)) return <>{children}</>
  return <Navigate to={fallback} replace />
}
