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

export function Sidebar() {
  return (
    <aside className="flex w-64 flex-col border-r bg-sidebar p-4">
      <div className="mb-8 text-lg font-bold tracking-tight">UserMVP</div>
      <nav className="flex flex-1 flex-col gap-1">
        {navItems.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground',
              )
            }
          >
            <Icon className="h-4 w-4" />
            {label}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}
