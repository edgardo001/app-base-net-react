import { NavLink } from 'react-router-dom'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/stores/auth-store'
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

interface NavItem {
  to: string
  label: string
  icon: React.ComponentType<{ className?: string }>
  requiredPermission?: string
}

const allNavItems: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/users', label: 'Usuarios', icon: Users, requiredPermission: 'users:list' },
  { to: '/roles', label: 'Roles', icon: Shield, requiredPermission: 'roles:list' },
  { to: '/permissions', label: 'Permisos', icon: KeyRound, requiredPermission: 'permissions:list' },
  { to: '/profile', label: 'Perfil', icon: UserCircle },
  { to: '/admin', label: 'Admin', icon: ShieldCheck, requiredPermission: 'admin:dashboard' },
  { to: '/tipo-a', label: 'Tipo A', icon: AArrowDown, requiredPermission: 'page-a:view' },
  { to: '/tipo-b', label: 'Tipo B', icon: Binary, requiredPermission: 'page-b:view' },
  { to: '/tipo-c', label: 'Tipo C', icon: BookType, requiredPermission: 'page-c:view' },
]

interface SidebarProps {
  collapsed: boolean
  mobileOpen: boolean
  onCloseMobile: () => void
}

function SidebarContent({ collapsed, onNavClick }: { collapsed: boolean; onNavClick?: () => void }) {
  const permissions = useAuthStore((s) => s.permissions)

  const visibleItems = allNavItems.filter(
    (item) => !item.requiredPermission || permissions.includes(item.requiredPermission),
  )

  return (
    <>
      <div className={cn(
        'mb-8 font-bold tracking-tight transition-all',
        collapsed ? 'text-center text-sm' : 'text-lg',
      )}>
        {collapsed ? 'UM' : 'UserMVP'}
      </div>
      <nav className="flex flex-1 flex-col gap-1">
        {visibleItems.map(({ to, label, icon: Icon }) => (
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
