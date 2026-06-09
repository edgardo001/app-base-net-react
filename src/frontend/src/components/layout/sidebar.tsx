import { NavLink } from 'react-router-dom'
import { cn } from '@/lib/utils'
import {
  LayoutDashboard,
  Users,
  Shield,
  KeyRound,
  UserCircle,
  ShieldCheck,
  AArrowDown,
  Binary,
  BookType,
} from 'lucide-react'

const navItems = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/users', label: 'Usuarios', icon: Users },
  { to: '/roles', label: 'Roles', icon: Shield },
  { to: '/permissions', label: 'Permisos', icon: KeyRound },
  { to: '/profile', label: 'Perfil', icon: UserCircle },
  { to: '/admin', label: 'Admin', icon: ShieldCheck },
  { to: '/tipo-a', label: 'Tipo A', icon: AArrowDown },
  { to: '/tipo-b', label: 'Tipo B', icon: Binary },
  { to: '/tipo-c', label: 'Tipo C', icon: BookType },
]

interface SidebarProps {
  collapsed: boolean
  mobileOpen: boolean
  onCloseMobile: () => void
}

function SidebarContent({ collapsed, onNavClick }: { collapsed: boolean; onNavClick?: () => void }) {
  return (
    <>
      <div className={cn(
        'mb-8 font-bold tracking-tight transition-all',
        collapsed ? 'text-center text-sm' : 'text-lg',
      )}>
        {collapsed ? 'UM' : 'UserMVP'}
      </div>
      <nav className="flex flex-1 flex-col gap-1">
        {navItems.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            title={collapsed ? label : undefined}
            onClick={onNavClick}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-lg py-2 text-sm font-medium transition-colors',
                collapsed ? 'justify-center px-0' : 'px-3',
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground',
              )
            }
          >
            <Icon className="h-4 w-4 shrink-0" />
            {!collapsed && label}
          </NavLink>
        ))}
      </nav>
    </>
  )
}

export function Sidebar({ collapsed, mobileOpen, onCloseMobile }: SidebarProps) {
  return (
    <>
      {/* Desktop: sidebar inline */}
      <aside className={cn(
        'hidden flex-col border-r bg-sidebar md:flex',
        collapsed ? 'w-16' : 'w-64',
        'transition-all duration-300',
      )}>
        <div className="flex-1 px-4 pt-4 pb-4">
          <SidebarContent collapsed={collapsed} />
        </div>
      </aside>

      {/* Móvil: backdrop + drawer overlay */}
      {mobileOpen && (
        <div className="fixed inset-0 z-40 md:hidden">
          <div
            className="absolute inset-0 bg-black/50 transition-opacity"
            onClick={onCloseMobile}
          />
          <aside className="relative flex h-full w-64 flex-col border-r bg-sidebar p-4">
            <SidebarContent collapsed={false} onNavClick={onCloseMobile} />
          </aside>
        </div>
      )}
    </>
  )
}
