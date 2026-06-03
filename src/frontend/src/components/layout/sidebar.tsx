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

export function Sidebar({ collapsed }: { collapsed: boolean }) {
  return (
    <aside className={cn(
      'flex flex-col border-r bg-sidebar p-4 transition-all duration-300',
      collapsed ? 'w-16' : 'w-64',
    )}>
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
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
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
    </aside>
  )
}
