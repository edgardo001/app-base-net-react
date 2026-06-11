import { useState, useEffect, useCallback } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import api, { getErrorMessage } from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Plus, Pencil, Search, ChevronLeft, ChevronRight, RefreshCw, Ban, CheckCircle, Send, KeyRound, ArrowUpDown, ArrowUp, ArrowDown, Camera, Download, Upload, Loader2, FileText, X } from 'lucide-react'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { AvatarUpload } from '@/components/ui/avatar-upload'

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
  const [sortBy, setSortBy] = useState<string>('createdAt')
  const [sortDesc, setSortDesc] = useState(true)
  const [filterActive, setFilterActive] = useState<'all' | 'active' | 'inactive'>('all')
  const [filterConfirmed, setFilterConfirmed] = useState<'all' | 'confirmed' | 'pending'>('all')
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [selectedRoles, setSelectedRoles] = useState<string[]>([])
  const [resendingId, setResendingId] = useState<string | null>(null)
  const [resendFeedback, setResendFeedback] = useState<{ id: string; type: 'ok' | 'err'; msg: string } | null>(null)
  const [resetId, setResetId] = useState<string | null>(null)
  const [resetFeedback, setResetFeedback] = useState<{ id: string; type: 'ok' | 'err'; msg: string } | null>(null)
  const [error, setError] = useState('')
  const [showAvatarModal, setShowAvatarModal] = useState(false)
  const [showImportModal, setShowImportModal] = useState(false)
  const [importFile, setImportFile] = useState<File | null>(null)
  const [importing, setImporting] = useState(false)
  const [importResult, setImportResult] = useState<{ created: number; errors: Array<{ rowNumber: number; message: string }> } | null>(null)
  const pageSize = 10

  const { register, handleSubmit, reset, formState: { errors } } = useForm<UserForm>({
    resolver: zodResolver(userSchema),
  })

  const fetchUsers = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const { data } = await api.get('/users', { params: { page, pageSize, search, sortBy, sortDesc } })
      let items = data.items || []
      if (filterActive !== 'all') {
        items = items.filter((u: User) => filterActive === 'active' ? u.isActive : !u.isActive)
      }
      if (filterConfirmed !== 'all') {
        items = items.filter((u: User) => filterConfirmed === 'confirmed' ? u.emailConfirmed : !u.emailConfirmed)
      }
      setUsers(items)
      setTotal(data.totalCount || 0)
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'Error al cargar usuarios'))
      setUsers([])
      setTotal(0)
    }
    setLoading(false)
  }, [page, search, sortBy, sortDesc, filterActive, filterConfirmed])

  const fetchRoles = async () => {
    try {
      const { data } = await api.get('/roles')
      setRoles(data.data?.items || data.data || [])
    } catch { /* roles are secondary — ignore */ }
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
    } catch {
      setSelectedRoles([])
    }
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
      alert(getErrorMessage(err, 'Error al guardar usuario'))
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
      setResetFeedback({ id, type: 'err', msg: getErrorMessage(err, 'Error al restablecer contraseña') })
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
      setResendFeedback({ id, type: 'err', msg: getErrorMessage(err, 'No se pudo reenviar el correo') })
    } finally {
      setResendingId(null)
      setTimeout(() => setResendFeedback(null), 4000)
    }
  }

  const totalPages = Math.ceil(total / pageSize)

  const handleAvatarUpload = async (file: File) => {
    if (!editingId) return
    const formData = new FormData()
    formData.append('file', file)
    await api.post(`/users/${editingId}/avatar`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
  }

  const handleExport = async () => {
    const params = new URLSearchParams()
    if (search) params.set('search', search)
    if (sortBy) params.set('sortBy', sortBy)
    params.set('sortDesc', String(sortDesc))
    window.open(`/api/users/export?${params.toString()}`, '_blank')
  }

  const handleImport = async () => {
    if (!importFile) return
    setImporting(true)
    setImportResult(null)
    try {
      const formData = new FormData()
      formData.append('file', importFile)
      const { data } = await api.post('/users/import', formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      setImportResult({ created: data.data.createdCount, errors: data.data.errorRows || [] })
      fetchUsers()
    } catch (err: unknown) {
      setImportResult({ created: 0, errors: [{ rowNumber: 0, message: getErrorMessage(err, 'Error al importar') }] })
    } finally {
      setImporting(false)
    }
  }

  const handleSort = (column: string) => {
    if (sortBy === column) {
      setSortDesc(!sortDesc)
    } else {
      setSortBy(column)
      setSortDesc(false)
    }
  }

  const SortIcon = ({ column }: { column: string }) => {
    if (sortBy !== column) return <ArrowUpDown className="ml-1 h-3 w-3 opacity-50" />
    return sortDesc
      ? <ArrowDown className="ml-1 h-3 w-3" />
      : <ArrowUp className="ml-1 h-3 w-3" />
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold tracking-tight">Usuarios</h1>
        <div className="flex gap-2">
          <Button variant="outline" onClick={handleExport}><Download className="mr-2 h-4 w-4" /> Exportar</Button>
          <Button variant="outline" onClick={() => { setImportFile(null); setImportResult(null); setShowImportModal(true) }}><Upload className="mr-2 h-4 w-4" /> Importar</Button>
          <Button onClick={openCreate}><Plus className="mr-2 h-4 w-4" /> Nuevo</Button>
        </div>
      </div>

      <Card>
        <CardHeader className="pb-3">
          <div className="flex flex-wrap items-center gap-4">
            <div className="relative flex-1 min-w-[200px] max-w-sm">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Buscar por email o nombre..."
                value={search}
                onChange={(e) => { setSearch(e.target.value); setPage(1) }}
                className="pl-10"
              />
            </div>
            <select
              value={filterActive}
              onChange={(e) => { setFilterActive(e.target.value as typeof filterActive); setPage(1) }}
              className="rounded-md border border-input bg-background px-3 py-2 text-sm"
            >
              <option value="all">Todos los estados</option>
              <option value="active">Activos</option>
              <option value="inactive">Inactivos</option>
            </select>
            <select
              value={filterConfirmed}
              onChange={(e) => { setFilterConfirmed(e.target.value as typeof filterConfirmed); setPage(1) }}
              className="rounded-md border border-input bg-background px-3 py-2 text-sm"
            >
              <option value="all">Todos los emails</option>
              <option value="confirmed">Confirmados</option>
              <option value="pending">Pendientes</option>
            </select>
            <Button variant="outline" size="icon" onClick={fetchUsers}><RefreshCw className="h-4 w-4" /></Button>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          {error && (
            <div className="mx-4 mt-4 rounded-md bg-destructive/10 p-3 text-sm text-destructive flex items-center justify-between">
              <span>{error}</span>
              <Button variant="outline" size="sm" onClick={fetchUsers}>
                <RefreshCw className="mr-1 h-4 w-4" /> Reintentar
              </Button>
            </div>
          )}
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="px-4 py-3 text-left font-medium cursor-pointer hover:bg-muted/80" onClick={() => handleSort('email')}>
                    Email <SortIcon column="email" />
                  </th>
                  <th className="px-4 py-3 text-left font-medium cursor-pointer hover:bg-muted/80" onClick={() => handleSort('firstName')}>
                    Nombre <SortIcon column="firstName" />
                  </th>
                  <th className="px-4 py-3 text-center font-medium cursor-pointer hover:bg-muted/80" onClick={() => handleSort('isActive')}>
                    Estado <SortIcon column="isActive" />
                  </th>
                  <th className="px-4 py-3 text-center font-medium cursor-pointer hover:bg-muted/80" onClick={() => handleSort('emailConfirmed')}>
                    Confirmado <SortIcon column="emailConfirmed" />
                  </th>
                  <th className="px-4 py-3 text-left font-medium cursor-pointer hover:bg-muted/80" onClick={() => handleSort('lastLoginAt')}>
                    Último Login <SortIcon column="lastLoginAt" />
                  </th>
                  <th className="px-4 py-3 text-left font-medium cursor-pointer hover:bg-muted/80" onClick={() => handleSort('createdAt')}>
                    Creado <SortIcon column="createdAt" />
                  </th>
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
                {editingId && (
                  <div className="flex items-center gap-3">
                    <Avatar className="h-12 w-12">
                      <AvatarImage src={`/api/users/${editingId}/avatar`} />
                      <AvatarFallback className="text-sm">?</AvatarFallback>
                    </Avatar>
                    <Button type="button" variant="outline" size="sm" onClick={() => setShowAvatarModal(true)}>
                      <Camera className="mr-1 h-3.5 w-3.5" /> Cambiar avatar
                    </Button>
                  </div>
                )}
                <div className="flex justify-end gap-2 pt-2">
                  <Button variant="outline" type="button" onClick={() => setShowModal(false)}>Cancelar</Button>
                  <Button type="submit">{editingId ? 'Guardar' : 'Crear'}</Button>
                </div>
              </form>
            </CardContent>
          </Card>
        </div>
      )}
      {showImportModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <Card className="w-full max-w-md">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>Importar Usuarios</CardTitle>
                <Button variant="ghost" size="icon" onClick={() => setShowImportModal(false)}><X className="h-4 w-4" /></Button>
              </div>
            </CardHeader>
            <CardContent className="space-y-4">
              {importResult ? (
                <div className="space-y-3">
                  <div className="rounded-md bg-green-50 p-3 text-sm text-green-700">
                    {importResult.created} usuario(s) creado(s)
                  </div>
                  {importResult.errors.length > 0 && (
                    <div>
                      <p className="mb-1 text-sm font-medium text-red-600">Errores ({importResult.errors.length}):</p>
                      <div className="max-h-40 space-y-1 overflow-y-auto">
                        {importResult.errors.map((e, i) => (
                          <p key={i} className="text-xs text-red-500">
                            {e.rowNumber > 0 ? `Fila ${e.rowNumber}: ` : ''}{e.message}
                          </p>
                        ))}
                      </div>
                    </div>
                  )}
                  <Button className="w-full" onClick={() => setShowImportModal(false)}>Cerrar</Button>
                </div>
              ) : (
                <>
                  <div
                    className="flex cursor-pointer flex-col items-center gap-2 rounded-md border-2 border-dashed p-8 text-center hover:bg-muted/50"
                    onClick={() => document.getElementById('csv-input')?.click()}
                    onDragOver={(e) => e.preventDefault()}
                    onDrop={(e) => {
                      e.preventDefault()
                      const file = e.dataTransfer.files[0]
                      if (file) setImportFile(file)
                    }}
                  >
                    {importFile ? (
                      <>
                        <FileText className="h-8 w-8 text-primary" />
                        <p className="text-sm font-medium">{importFile.name}</p>
                        <p className="text-xs text-muted-foreground">{(importFile.size / 1024).toFixed(1)} KB</p>
                      </>
                    ) : (
                      <>
                        <Upload className="h-8 w-8 text-muted-foreground" />
                        <p className="text-sm text-muted-foreground">Arrastra un archivo CSV aquí o haz clic para seleccionar</p>
                        <p className="text-xs text-muted-foreground">Formato: Email, FirstName, LastName (máx 10MB)</p>
                      </>
                    )}
                    <input
                      id="csv-input"
                      type="file"
                      accept=".csv"
                      className="hidden"
                      onChange={(e) => { const f = e.target.files?.[0]; if (f) setImportFile(f) }}
                    />
                  </div>
                  <Button className="w-full" onClick={handleImport} disabled={!importFile || importing}>
                    {importing ? <><Loader2 className="mr-2 h-4 w-4 animate-spin" /> Importando...</> : <>Importar</>}
                  </Button>
                </>
              )}
            </CardContent>
          </Card>
        </div>
      )}
      <AvatarUpload
        open={showAvatarModal}
        onClose={() => setShowAvatarModal(false)}
        onUpload={handleAvatarUpload}
      />
    </div>
  )
}
