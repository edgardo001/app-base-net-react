import { useState, useEffect } from 'react'
import api from '@/lib/api'
import { useAuthStore } from '@/stores/auth-store'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Users, UserCheck, UserPlus, Activity } from 'lucide-react'

interface DashboardData {
  totalUsers: number
  activeUsers: number
  newUsersLast7Days: number
  inactiveUsers: number
}

interface AuditEntry {
  action: string
  entityType: string
  details: string | null
  createdBy: string | null
  createdAt: string
}

export function DashboardPage() {
  const user = useAuthStore((s) => s.user)
  const [data, setData] = useState<DashboardData | null>(null)
  const [auditLog, setAuditLog] = useState<AuditEntry[]>([])

  useEffect(() => {
    const fetchDashboard = async () => {
      try {
        const { data: res } = await api.get('/admin/dashboard')
        setData(res.data)
      } catch { /* ignore */ }
    }
    const fetchAudit = async () => {
      try {
        const { data: res } = await api.get('/admin/audit-log')
        setAuditLog(res.data?.items || [])
      } catch { /* ignore */ }
    }
    fetchDashboard()
    fetchAudit()
  }, [])

  const cards = [
    { title: 'Total Usuarios', value: data?.totalUsers ?? '—', icon: Users, color: 'text-blue-600' },
    { title: 'Usuarios Activos', value: data?.activeUsers ?? '—', icon: UserCheck, color: 'text-green-600' },
    { title: 'Nuevos (7 días)', value: data?.newUsersLast7Days ?? '—', icon: UserPlus, color: 'text-purple-600' },
    { title: 'Usuarios Inactivos', value: data?.inactiveUsers ?? '—', icon: Activity, color: 'text-orange-600' },
  ]

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">
          Bienvenido, {user?.firstName} {user?.lastName}
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {cards.map((c) => (
          <Card key={c.title}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">{c.title}</CardTitle>
              <c.icon className={`h-5 w-5 ${c.color}`} />
            </CardHeader>
            <CardContent>
              <p className="text-3xl font-bold">{c.value}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Actividad Reciente</CardTitle>
        </CardHeader>
        <CardContent>
          {auditLog.length === 0 ? (
            <p className="text-sm text-muted-foreground">Sin actividad registrada</p>
          ) : (
            <div className="space-y-2">
              {auditLog.slice(0, 10).map((entry, i) => (
                <div key={i} className="flex items-center justify-between rounded-md bg-muted/50 px-3 py-2 text-sm">
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className="font-mono text-xs">{entry.action}</Badge>
                    <span className="text-muted-foreground">{entry.entityType}</span>
                    {entry.details && <span className="text-muted-foreground">— {entry.details}</span>}
                  </div>
                  <span className="text-xs text-muted-foreground">{new Date(entry.createdAt).toLocaleString()}</span>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
