import { useState, useEffect } from 'react'
import axios from 'axios'
import api from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Plus, Pencil, Trash2, Shield } from 'lucide-react'

interface Role {
  id: string
  name: string
  description: string
  isSystem: boolean
  createdAt: string
}

interface Permission {
  id: string
  code: string
  name: string
  module: string
}

interface PermissionGroup {
  module: string
  permissions: Permission[]
}

export function RolesPage() {
  const [roles, setRoles] = useState<Role[]>([])
  const [permissions, setPermissions] = useState<PermissionGroup[]>([])
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [selectedPerms, setSelectedPerms] = useState<string[]>([])
  const [error, setError] = useState('')

  const fetchRoles = async () => {
    setLoading(true)
    try {
      const { data } = await api.get('/roles')
      setRoles(data.data || [])
    } catch { /* ignore */ }
    setLoading(false)
  }

  const fetchPermissions = async () => {
    try {
      const { data } = await api.get('/permissions/modules')
      setPermissions(data.data || [])
    } catch { /* ignore */ }
  }

  useEffect(() => { fetchRoles() }, [])
  useEffect(() => { fetchPermissions() }, [])

  const openCreate = () => {
    setEditingId(null)
    setName('')
    setDescription('')
    setSelectedPerms([])
    setError('')
    setShowModal(true)
  }

  const openEdit = async (role: Role) => {
    setEditingId(role.id)
    setName(role.name)
    setDescription(role.description)
    setError('')
    try {
      const { data } = await api.get(`/roles/${role.id}`)
      setSelectedPerms(data.data.permissions?.filter((p: { granted: boolean }) => p.granted).map((p: { id: string }) => p.id) || [])
    } catch { setSelectedPerms([]) }
    setShowModal(true)
  }

  const save = async () => {
    if (!name.trim()) { setError('El nombre es requerido'); return }
    setError('')
    try {
      if (editingId) {
        await api.put(`/roles/${editingId}`, { name, description })
        await api.patch(`/roles/${editingId}/permissions`, {
          permissions: permissions.flatMap(g => g.permissions).map(p => ({
            permissionId: p.id,
            granted: selectedPerms.includes(p.id),
          }))
        })
      } else {
        await api.post('/roles', { name, description })
      }
      setShowModal(false)
      fetchRoles()
    } catch (err: unknown) {
      const msg = axios.isAxiosError(err) ? err.response?.data?.message : 'Error'
      setError(msg || 'Error')
    }
  }

  const deleteRole = async (role: Role) => {
    if (role.isSystem) { alert('No se puede eliminar un rol del sistema'); return }
    if (!confirm(`¿Eliminar rol "${role.name}"?`)) return
    await api.delete(`/roles/${role.id}`)
    fetchRoles()
  }

  const togglePerm = (permId: string) => {
    setSelectedPerms(prev => prev.includes(permId) ? prev.filter(x => x !== permId) : [...prev, permId])
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold tracking-tight">Roles</h1>
        <Button onClick={openCreate}><Plus className="mr-2 h-4 w-4" /> Nuevo Rol</Button>
      </div>

      {loading ? (
        <div className="flex justify-center py-12 text-muted-foreground">Cargando...</div>
      ) : (
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {roles.map((r) => (
          <Card key={r.id}>
            <CardHeader className="pb-3">
              <div className="flex items-start justify-between">
                <div>
                  <CardTitle className="flex items-center gap-2 text-lg">
                    <Shield className="h-4 w-4" />
                    {r.name}
                  </CardTitle>
                  <p className="mt-1 text-sm text-muted-foreground">{r.description}</p>
                </div>
                {r.isSystem && <Badge variant="secondary">Sistema</Badge>}
              </div>
            </CardHeader>
            <CardContent>
              <div className="flex justify-end gap-1">
                <Button variant="ghost" size="sm" onClick={() => openEdit(r)}><Pencil className="mr-1 h-4 w-4" /> Editar</Button>
                <Button variant="ghost" size="sm" onClick={() => deleteRole(r)} disabled={r.isSystem}>
                  <Trash2 className="mr-1 h-4 w-4 text-red-500" /> Eliminar
                </Button>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
      )}

      {showModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <Card className="w-full max-w-lg max-h-[80vh] overflow-y-auto">
            <CardHeader>
              <CardTitle>{editingId ? 'Editar Rol' : 'Nuevo Rol'}</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {error && <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}
              <div className="space-y-2">
                <Label htmlFor="name">Nombre</Label>
                <Input id="name" value={name} onChange={(e) => setName(e.target.value)} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="desc">Descripción</Label>
                <Input id="desc" value={description} onChange={(e) => setDescription(e.target.value)} />
              </div>
              {editingId && (
                <div className="space-y-2">
                  <Label>Permisos</Label>
                  {permissions.map((group) => (
                    <div key={group.module} className="space-y-1">
                      <p className="text-sm font-medium text-muted-foreground">{group.module}</p>
                      <div className="flex flex-wrap gap-1">
                        {group.permissions.map((p) => (
                          <Badge
                            key={p.id}
                            variant={selectedPerms.includes(p.id) ? 'default' : 'outline'}
                            className="cursor-pointer"
                            onClick={() => togglePerm(p.id)}
                          >
                            {p.name}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  ))}
                </div>
              )}
              <div className="flex justify-end gap-2 pt-2">
                <Button variant="outline" onClick={() => setShowModal(false)}>Cancelar</Button>
                <Button onClick={save}>{editingId ? 'Guardar' : 'Crear'}</Button>
              </div>
            </CardContent>
          </Card>
        </div>
      )}
    </div>
  )
}
