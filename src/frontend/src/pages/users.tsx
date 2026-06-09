import { useState, useEffect, useCallback } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import axios from 'axios'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Plus, Pencil, Search, ChevronLeft, ChevronRight, RefreshCw, Ban, CheckCircle, Send, KeyRound } from 'lucide-react'

interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  isActive: boolean
  emailConfirmed: boolean
  lastLoginAt: string | null
  createdAt: string
}

interface Role {
  id: string
  name: string
}

const userSchema = z.object({
  email: z.string().email('Email inválido'),
  firstName: z.string().min(1, 'Requerido').max(100),
  lastName: z.string().min(1, 'Requerido').max(100),
})

type UserForm = z.infer<typeof userSchema>

export function UsersPage() {
  const [users, setUsers] = useState<User[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [selectedRoles, setSelectedRoles] = useState<string[]>([])
  const [resendingId, setResendingId] = useState<string | null>(null)
  const [resendFeedback, setResendFeedback] = useState<{ id: string; type: 'ok' | 'err'; msg: string } | null>(null)
  const [resetId, setResetId] = useState<string | null>(null)
  const [resetFeedback, setResetFeedback] = useState<{ id: string; type: 'ok' | 'err'; msg: string } | null>(null)
  const pageSize = 10

  const { register, handleSubmit, reset, formState: { errors } } = useForm<UserForm>({
    resolver: zodResolver(userSchema),
  })

  const fetchUsers = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await api.get('/users', { params: { page, pageSize, search } })
      setUsers(data.items || [])
      setTotal(data.totalCount || 0)
    } catch { /* ignore */ }
    setLoading(false)
  }, [page, search])

  const fetchRoles = async () => {
    try {
      const { data } = await api.get('/roles')
      setRoles(data.data || [])
    } catch { /* ignore */ }
  }

  useEffect(() => { fetchUsers() }, [fetchUsers])
  useEffect(() => { fetchRoles() }, [])

  const openCreate = () => {
    setEditingId(null)
    setSelectedRoles([])
    reset({ email: '', firstName: '', lastName: '' })
    setShowModal(true)
  }

  const openEdit = async (user: User) => {
    setEditingId(user.id)
    try {
      const { data } = await api.get(`/users/${user.id}`)
      reset({ email: data.data.email, firstName: data.data.firstName, lastName: data.data.lastName })
      setSelectedRoles(data.data.roles?.map((r: { id: string }) => r.id) || [])
    } catch { /* ignore */ }
    setShowModal(true)
  }

  const onSubmit = async (form: UserForm) => {
    try {
      if (editingId) {
        await api.put(`/users/${editingId}`, { firstName: form.firstName, lastName: form.lastName, roleIds: selectedRoles })
      } else {
        await api.post('/users', { ...form, roleIds: selectedRoles })
      }
      setShowModal(false)
      fetchUsers()
    } catch (err: unknown) {
      const msg = axios.isAxiosError(err) ? err.response?.data?.message : 'Error'
      alert(msg || 'Error')
    }
  }

  const toggleActive = async (id: string, active: boolean) => {
    await api.patch(`/users/${id}/activate`, { active: !active })
    fetchUsers()
  }

  const resetPassword = async (id: string) => {
    if (!window.confirm('¿Estás seguro? Se generará una nueva contraseña temporal y se enviará al correo del usuario.')) return
    setResetId(id)
    setResetFeedback(null)
    try {
      await api.patch(`/users/${id}/reset-password`)
      setResetFeedback({ id, type: 'ok', msg: 'Contraseña restablecida — correo enviado' })
    } catch (err: unknown) {
      const msg = axios.isAxiosError(err)
        ? (err.response?.data?.message ?? 'Error al restablecer contraseña')
        : 'Error al restablecer contraseña'
      setResetFeedback({ id, type: 'err', msg })
    } finally {
      setResetId(null)
      setTimeout(() => setResetFeedback(null), 4000)
    }
  }

  const resendOnboardingEmail = async (id: string) => {
    setResendingId(id)
    setResendFeedback(null)
    try {
      await api.post(`/users/${id}/resend-onboarding-email`)
      setResendFeedback({ id, type: 'ok', msg: 'Correo de bienvenida reenviado' })
    } catch (err: unknown) {
      const msg = axios.isAxiosError(err)
        ? (err.response?.data?.message ?? 'No se pudo reenviar el correo')
        : 'No se pudo reenviar el correo'
      setResendFeedback({ id, type: 'err', msg })
    } finally {
      setResendingId(null)
      setTimeout(() => setResendFeedback(null), 4000)
    }
  }

  const totalPages = Math.ceil(total / pageSize)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold tracking-tight">Usuarios</h1>
        <Button onClick={openCreate}><Plus className="mr-2 h-4 w-4" /> Nuevo</Button>
      </div>

      <Card>
        <CardHeader className="pb-3">
          <div className="flex items-center gap-4">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Buscar por email o nombre..."
                value={search}
                onChange={(e) => { setSearch(e.target.value); setPage(1) }}
                className="pl-10"
              />
            </div>
            <Button variant="outline" size="icon" onClick={fetchUsers}><RefreshCw className="h-4 w-4" /></Button>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-4 py-3 text-left font-medium">Email</th>
                  <th className="px-4 py-3 text-left font-medium">Nombre</th>
                  <th className="px-4 py-3 text-center font-medium">Estado</th>
                  <th className="px-4 py-3 text-center font-medium">Confirmado</th>
                  <th className="px-4 py-3 text-left font-medium">Último Login</th>
                  <th className="px-4 py-3 text-left font-medium">Creado</th>
                  <th className="px-4 py-3 text-center font-medium">Acciones</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={7} className="px-4 py-8 text-center text-muted-foreground">Cargando...</td></tr>
                ) : users.length === 0 ? (
                  <tr><td colSpan={7} className="px-4 py-8 text-center text-muted-foreground">Sin resultados</td></tr>
                ) : users.map((u) => (
                  <tr key={u.id} className="border-b hover:bg-muted/50">
                    <td className="px-4 py-3">{u.email}</td>
                    <td className="px-4 py-3">{u.firstName} {u.lastName}</td>
                    <td className="px-4 py-3 text-center">
                      <Badge variant={u.isActive ? 'default' : 'secondary'}>{u.isActive ? 'Activo' : 'Inactivo'}</Badge>
                    </td>
                    <td className="px-4 py-3 text-center">
                      {u.emailConfirmed ? <CheckCircle className="inline h-4 w-4 text-green-500" /> : <Ban className="inline h-4 w-4 text-red-500" />}
                    </td>
                    <td className="px-4 py-3">{u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleDateString() : '—'}</td>
                    <td className="px-4 py-3">{new Date(u.createdAt).toLocaleDateString()}</td>
                    <td className="px-4 py-3">
                      <div className="flex flex-col items-center gap-1">
                        <div className="flex justify-center gap-1">
                          <Button variant="ghost" size="icon" onClick={() => openEdit(u)} title="Editar"><Pencil className="h-4 w-4" /></Button>
                          <Button variant="ghost" size="icon" onClick={() => toggleActive(u.id, u.isActive)} title={u.isActive ? 'Desactivar' : 'Activar'}>
                            {u.isActive ? <Ban className="h-4 w-4 text-red-500" /> : <CheckCircle className="h-4 w-4 text-green-500" />}
                          </Button>
                          {!u.emailConfirmed && (
                            <Button
                              variant="ghost"
                              size="icon"
                              onClick={() => resendOnboardingEmail(u.id)}
                              disabled={resendingId === u.id}
                              title="Reenviar correo de bienvenida"
                            >
                              {resendingId === u.id
                                ? <RefreshCw className="h-4 w-4 animate-spin" />
                                : <Send className="h-4 w-4 text-blue-500" />}
                            </Button>
                          )}
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => resetPassword(u.id)}
                            disabled={resetId === u.id}
                            title="Restablecer contraseña"
                          >
                            {resetId === u.id
                              ? <RefreshCw className="h-4 w-4 animate-spin" />
                              : <KeyRound className="h-4 w-4 text-orange-500" />}
                          </Button>
                        </div>
                        {resendFeedback?.id === u.id && (
                          <p className={`text-xs ${resendFeedback.type === 'ok' ? 'text-green-600' : 'text-red-500'}`}>
                            {resendFeedback.msg}
                          </p>
                        )}
                        {resetFeedback?.id === u.id && (
                          <p className={`text-xs ${resetFeedback.type === 'ok' ? 'text-green-600' : 'text-red-500'}`}>
                            {resetFeedback.msg}
                          </p>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(p => p - 1)}>
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <span className="text-sm text-muted-foreground">Página {page} de {totalPages}</span>
          <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}>
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      )}

      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <Card className="w-full max-w-md">
            <CardHeader>
              <CardTitle>{editingId ? 'Editar Usuario' : 'Nuevo Usuario'}</CardTitle>
            </CardHeader>
            <CardContent>
              <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="email">Email</Label>
                  <Input id="email" {...register('email')} disabled={!!editingId} />
                  {errors.email && <p className="text-sm text-red-500">{errors.email.message}</p>}
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label htmlFor="firstName">Nombre</Label>
                    <Input id="firstName" {...register('firstName')} />
                    {errors.firstName && <p className="text-sm text-red-500">{errors.firstName.message}</p>}
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="lastName">Apellido</Label>
                    <Input id="lastName" {...register('lastName')} />
                    {errors.lastName && <p className="text-sm text-red-500">{errors.lastName.message}</p>}
                  </div>
                </div>
                {!editingId && (
                  <p className="text-xs text-muted-foreground">
                    Se generará una contraseña temporal automáticamente y se enviará al correo junto con el enlace de confirmación.
                  </p>
                )}
                <div className="space-y-2">
                  <Label>Roles</Label>
                  <div className="flex flex-wrap gap-2">
                    {roles.map((r) => (
                      <Badge
                        key={r.id}
                        variant={selectedRoles.includes(r.id) ? 'default' : 'outline'}
                        className="cursor-pointer"
                        onClick={() => setSelectedRoles(prev =>
                          prev.includes(r.id) ? prev.filter(x => x !== r.id) : [...prev, r.id]
                        )}
                      >
                        {r.name}
                      </Badge>
                    ))}
                  </div>
                </div>
                <div className="flex justify-end gap-2 pt-2">
                  <Button variant="outline" type="button" onClick={() => setShowModal(false)}>Cancelar</Button>
                  <Button type="submit">{editingId ? 'Guardar' : 'Crear'}</Button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  )
}
