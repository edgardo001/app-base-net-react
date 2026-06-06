import { useState, useEffect } from 'react'
import api from '@/lib/api'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Activity, ShieldAlert, RefreshCw, Mail, Loader2 } from 'lucide-react'
import { toast } from 'sonner'
import { useAuthStore } from '@/stores/auth-store'
import { ROLE_CLAIM_NAMES } from '@/lib/jwt'

interface AuditEntry {
  action: string
  entityType: string
  entityId: string | null
  details: string | null
  userId: string | null
  createdAt: string
}

export function AdminPage() {
  const roles = useAuthStore((s) => s.roles)
  const canTestEmail = roles.includes(ROLE_CLAIM_NAMES.Admin) || roles.includes(ROLE_CLAIM_NAMES.SuperAdmin)
  const [auditLog, setAuditLog] = useState<AuditEntry[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [loading, setLoading] = useState(true)
  const [testEmail, setTestEmail] = useState('')
  const [sending, setSending] = useState(false)

  const fetchAudit = async () => {
    setLoading(true)
    try {
      const { data } = await api.get('/admin/audit-log', { params: { page, pageSize: 20 } })
      setAuditLog(data.data?.items || [])
      setTotal(data.data?.totalCount || 0)
    } catch { /* ignore */ }
    setLoading(false)
  }

  useEffect(() => { fetchAudit() }, [page])

  const revokeAll = async () => {
    if (!confirm('¿Revocar todos los tokens de sesión? Todos los usuarios serán desconectados.')) return
    try {
      await api.post('/admin/revoke-all-tokens')
      toast.success('Todas las sesiones han sido revocadas')
      fetchAudit()
    } catch { /* ignore */ }
  }

  const sendTestEmail = async () => {
    if (!testEmail.trim()) {
      toast.error('Ingresa un correo electrónico')
      return
    }
    setSending(true)
    try {
      await api.post('/admin/test-email', { to: testEmail.trim() })
      toast.success('Correo de prueba enviado correctamente')
      setTestEmail('')
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Error al enviar correo de prueba'
      toast.error(msg)
    }
    setSending(false)
  }

  const totalPages = Math.ceil(total / 20)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold tracking-tight">Administración</h1>
        <Button variant="destructive" onClick={revokeAll}>
          <ShieldAlert className="mr-2 h-4 w-4" /> Revocar Todas las Sesiones
        </Button>
      </div>

      {canTestEmail && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Mail className="h-5 w-5" /> Correo de Prueba
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex gap-3">
              <Input
                type="email"
                placeholder="correo@ejemplo.com"
                value={testEmail}
                onChange={(e) => setTestEmail(e.target.value)}
                disabled={sending}
                className="max-w-sm"
              />
              <Button onClick={sendTestEmail} disabled={sending}>
                {sending ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <Mail className="mr-2 h-4 w-4" />}
                Enviar
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-2">
              <Activity className="h-5 w-5" /> Auditoría
            </CardTitle>
            <Button variant="outline" size="sm" onClick={fetchAudit}>
              <RefreshCw className="mr-1 h-4 w-4" /> Actualizar
            </Button>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-4 py-3 text-left font-medium">Acción</th>
                  <th className="px-4 py-3 text-left font-medium">Tipo</th>
                  <th className="px-4 py-3 text-left font-medium">Detalle</th>
                  <th className="px-4 py-3 text-right font-medium">Fecha</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={4} className="px-4 py-8 text-center text-muted-foreground">Cargando...</td></tr>
                ) : auditLog.length === 0 ? (
                  <tr><td colSpan={4} className="px-4 py-8 text-center text-muted-foreground">Sin registros</td></tr>
                ) : auditLog.map((entry, i) => (
                  <tr key={i} className="border-b hover:bg-muted/50">
                    <td className="px-4 py-3"><Badge variant="outline" className="font-mono text-xs">{entry.action}</Badge></td>
                    <td className="px-4 py-3">{entry.entityType}</td>
                    <td className="px-4 py-3 text-muted-foreground">{entry.details || '—'}</td>
                    <td className="px-4 py-3 text-right text-muted-foreground">{new Date(entry.createdAt).toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>Anterior</Button>
          <span className="text-sm text-muted-foreground">Página {page} de {totalPages}</span>
          <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}>Siguiente</Button>
        </div>
      )}
    </div>
  )
}
