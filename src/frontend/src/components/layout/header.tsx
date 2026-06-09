import { useAuthStore } from '@/stores/auth-store'
import { Button } from '@/components/ui/button'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'

export function Header() {
  const user = useAuthStore((s) => s.user)
  const logout = useAuthStore((s) => s.logout)

  return (
    <header className="flex h-14 items-center justify-end gap-4 border-b px-6">
      <div className="flex items-center gap-2 text-sm">
        <Avatar className="h-8 w-8">
          <AvatarFallback>
            {user?.firstName?.charAt(0)}{user?.lastName?.charAt(0)}
          </AvatarFallback>
        </Avatar>
        <span className="font-medium">{user?.firstName} {user?.lastName}</span>
      </div>
      <Button variant="outline" size="sm" onClick={logout}>
        Salir
      </Button>
    </header>
  )
}
