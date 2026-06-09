import { useState, useEffect } from 'react'
import api, { getErrorMessage } from '@/lib/api'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { RefreshCw, Lock } from 'lucide-react'

interface Permission {
  id: string
  code: string
  name: string
  description: string
}

interface PermissionModule {
  module: string
  permissions: Permission[]
}

interface Role {
  id: string
  name: string
  description: string
  isSystem: boolean
  permissions?: { id: string; code: string; granted: boolean }[]
}

export function PermissionsPage() {
  const [modules, setModules] = useState<PermissionModule[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const fetchData = async () => {
    setLoading(true)
    setError('')
    try {
      const [modRes, rolesRes] = await Promise.all([
        api.get('/permissions/modules'),
        api.get('/roles'),
      ])
      setModules(modRes.data.data || [])
      setRoles(rolesRes.data.data || [])
    } catch (err: unknown) {
      setError(getErrorMessage(err, 'Error al cargar permisos'))
    }
    setLoading(false)
  }

  useEffect(() => { fetchData() }, [])

  const getRolesWithPermission = (permId: string) => {
    return roles.filter(r =>
      r.permissions?.some(p => p.id === permId && p.granted)
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold tracking-tight">Permisos</h1>
        <Button variant="outline" onClick={fetchData} disabled={loading}>
          <RefreshCw className={`mr-2 h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
          Actualizar
        </Button>
      </div>

      {loading ? (
        <div className="flex justify-center py-12 text-muted-foreground">Cargando...</div>
      ) : error ? (
        <div className="rounded-md bg-destructive/10 p-4 text-sm text-destructive flex items-center justify-between">
          <span>{error}</span>
          <Button variant="outline" size="sm" onClick={fetchData}>
            <RefreshCw className="mr-1 h-4 w-4" /> Reintentar
          </Button>
        </div>
      ) : (
        <div className="space-y-4">
          {modules.map((mod) => (
            <Card key={mod.module}>
              <CardHeader className="pb-3">
                <CardTitle className="flex items-center gap-2 text-lg">
                  <Lock className="h-4 w-4" />
                  {mod.module}
                </CardTitle>
                <p className="text-sm text-muted-foreground">
                  {mod.permissions.length} permiso{mod.permissions.length !== 1 ? 's' : ''}
                </p>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {mod.permissions.map((perm) => {
                    const assignedRoles = getRolesWithPermission(perm.id)
                    return (
                      <div key={perm.id} className="flex items-start justify-between gap-4 rounded-md border p-3">
                        <div className="space-y-1">
                          <div className="flex items-center gap-2">
                            <span className="font-medium">{perm.name}</span>
                            <Badge variant="outline" className="text-xs">{perm.code}</Badge>
                          </div>
                          {perm.description && (
                            <p className="text-sm text-muted-foreground">{perm.description}</p>
                          )}
                        </div>
                        <div className="flex flex-wrap gap-1">
                          {assignedRoles.length > 0 ? (
                            assignedRoles.map((r) => (
                              <Badge key={r.id} variant="secondary">{r.name}</Badge>
                            ))
                          ) : (
                            <Badge variant="outline" className="text-muted-foreground">Sin asignar</Badge>
                          )}
                        </div>
                      </div>
                    )
                  })}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
