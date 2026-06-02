import { useAuthStore } from '@/stores/auth-store'

export function ProfilePage() {
  const user = useAuthStore((s) => s.user)

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold tracking-tight">Mi Perfil</h1>
      <p className="text-muted-foreground">
        {user?.firstName} {user?.lastName} &mdash; {user?.email}
      </p>
    </div>
  )
}
