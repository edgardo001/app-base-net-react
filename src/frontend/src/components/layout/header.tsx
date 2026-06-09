import { useState, useEffect } from 'react'
import { useAuthStore } from '@/stores/auth-store'
import { Button } from '@/components/ui/button'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { PanelLeftOpen, PanelLeftClose, Menu, X } from 'lucide-react'

function useIsDesktop() {
  const [isDesktop, setIsDesktop] = useState(() => window.innerWidth >= 768)
  useEffect(() => {
    const mql = window.matchMedia('(min-width: 768px)')
    const handler = (e: MediaQueryListEvent) => setIsDesktop(e.matches)
    mql.addEventListener('change', handler)
    return () => mql.removeEventListener('change', handler)
  }, [])
  return isDesktop
}

interface HeaderProps {
  onToggleCollapse: () => void
  onToggleMobile: () => void
  sidebarCollapsed: boolean
  mobileOpen: boolean
}

export function Header({ onToggleCollapse, onToggleMobile, sidebarCollapsed, mobileOpen }: HeaderProps) {
  const user = useAuthStore((s) => s.user)
  const logout = useAuthStore((s) => s.logout)
  const isDesktop = useIsDesktop()

  return (
    <header className="flex h-14 items-center justify-between border-b px-6">
      {isDesktop ? (
        <Button variant="ghost" size="icon" onClick={onToggleCollapse}>
          {sidebarCollapsed
            ? <PanelLeftOpen className="h-4 w-4" />
            : <PanelLeftClose className="h-4 w-4" />
          }
        </Button>
      ) : (
        <Button variant="ghost" size="icon" onClick={onToggleMobile}>
          {mobileOpen ? <X className="h-4 w-4" /> : <Menu className="h-4 w-4" />}
        </Button>
      )}
      <div className="flex items-center gap-4">
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
      </div>
    </header>
  )
}
