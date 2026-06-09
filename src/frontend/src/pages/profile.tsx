import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import api, { getErrorMessage } from '@/lib/api'
import { useAuthStore } from '@/stores/auth-store'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { PasswordInput } from '@/components/ui/password-input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { User, KeyRound, Clock } from 'lucide-react'

const profileSchema = z.object({
  firstName: z.string().min(1, 'Requerido').max(100),
  lastName: z.string().min(1, 'Requerido').max(100),
})

const passwordSchema = z.object({
  currentPassword: z.string().min(1, 'Requerido'),
  newPassword: z.string().min(6, 'Mínimo 6 caracteres'),
  confirmPassword: z.string().min(1, 'Requerido'),
}).refine((d) => d.newPassword === d.confirmPassword, {
  message: 'Las contraseñas no coinciden',
  path: ['confirmPassword'],
})

type ProfileForm = z.infer<typeof profileSchema>
type PasswordForm = z.infer<typeof passwordSchema>

interface Activity {
  action: string
  entityType: string
  details: string | null
  createdAt: string
}

export function ProfilePage() {
  const user = useAuthStore((s) => s.user)
  const [activities, setActivities] = useState<Activity[]>([])
  const [profileMsg, setProfileMsg] = useState('')
  const [pwdMsg, setPwdMsg] = useState('')
  const [pwdError, setPwdError] = useState('')
  const [activityError, setActivityError] = useState('')

  const profileForm = useForm<ProfileForm>({
    resolver: zodResolver(profileSchema),
    defaultValues: { firstName: user?.firstName || '', lastName: user?.lastName || '' },
  })

  const passwordForm = useForm<PasswordForm>({
    resolver: zodResolver(passwordSchema),
  })

  useEffect(() => {
    const fetchActivity = async () => {
      try {
        const { data } = await api.get('/profile/activity')
        setActivities(data.data || [])
      } catch (err: unknown) {
        setActivityError(getErrorMessage(err, 'Error al cargar actividad'))
      }
    }
    fetchActivity()
  }, [])

  const updateProfile = async (form: ProfileForm) => {
    setProfileMsg('')
    try {
      await api.put('/profile', form)
      setProfileMsg('Perfil actualizado')
      useAuthStore.setState({ user: { ...user!, ...form } })
    } catch (err: unknown) {
      setProfileMsg(getErrorMessage(err, 'Error al actualizar perfil'))
    }
  }

  const changePassword = async (form: PasswordForm) => {
    setPwdError('')
    setPwdMsg('')
    try {
      await api.post('/auth/change-password', form)
      setPwdMsg('Contraseña cambiada exitosamente')
      passwordForm.reset()
    } catch (err: unknown) {
      setPwdError(getErrorMessage(err, 'Error al cambiar contraseña'))
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-bold tracking-tight">Mi Perfil</h1>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2"><User className="h-5 w-5" /> Información Personal</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center gap-4">
              <Avatar className="h-16 w-16">
                {user?.avatarPath && <AvatarImage src={user.avatarPath} />}
                <AvatarFallback className="text-lg">{user?.firstName?.charAt(0)}{user?.lastName?.charAt(0)}</AvatarFallback>
              </Avatar>
              <div>
                <p className="font-medium">{user?.firstName} {user?.lastName}</p>
                <p className="text-sm text-muted-foreground">{user?.email}</p>
              </div>
            </div>
            {profileMsg && <div className="rounded-md bg-primary/10 p-3 text-sm text-primary">{profileMsg}</div>}
            <form onSubmit={profileForm.handleSubmit(updateProfile)} className="space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label htmlFor="pf">Nombre</Label>
                  <Input id="pf" {...profileForm.register('firstName')} />
                </div>
                <div className="space-y-1">
                  <Label htmlFor="pl">Apellido</Label>
                  <Input id="pl" {...profileForm.register('lastName')} />
                </div>
              </div>
              <Button type="submit">Guardar Cambios</Button>
            </form>
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2"><KeyRound className="h-5 w-5" /> Cambiar Contraseña</CardTitle>
            </CardHeader>
            <CardContent>
              {pwdMsg && <div className="mb-3 rounded-md bg-primary/10 p-3 text-sm text-primary">{pwdMsg}</div>}
              {pwdError && <div className="mb-3 rounded-md bg-destructive/10 p-3 text-sm text-destructive">{pwdError}</div>}
              <form onSubmit={passwordForm.handleSubmit(changePassword)} className="space-y-3">
                <div className="space-y-1">
                  <Label htmlFor="cp">Contraseña actual</Label>
                  <PasswordInput id="cp" {...passwordForm.register('currentPassword')} />
                </div>
                <div className="space-y-1">
                  <Label htmlFor="np">Nueva contraseña</Label>
                  <PasswordInput id="np" {...passwordForm.register('newPassword')} />
                  {passwordForm.formState.errors.newPassword && (
                    <p className="text-sm text-red-500">{passwordForm.formState.errors.newPassword.message}</p>
                  )}
                </div>
                <div className="space-y-1">
                  <Label htmlFor="cfp">Confirmar contraseña</Label>
                  <PasswordInput id="cfp" {...passwordForm.register('confirmPassword')} />
                  {passwordForm.formState.errors.confirmPassword && (
                    <p className="text-sm text-red-500">{passwordForm.formState.errors.confirmPassword.message}</p>
                  )}
                </div>
                <Button type="submit">Cambiar Contraseña</Button>
              </form>
            </CardContent>
          </Card>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2"><Clock className="h-5 w-5" /> Actividad Reciente</CardTitle>
        </CardHeader>
        <CardContent>
          {activityError ? (
            <p className="text-sm text-destructive">{activityError}</p>
          ) : activities.length === 0 ? (
            <p className="text-sm text-muted-foreground">Sin actividad registrada</p>
          ) : (
            <div className="space-y-2">
              {activities.map((a, i) => (
                <div key={i} className="flex items-center justify-between rounded-md bg-muted/50 px-3 py-2 text-sm">
                  <div>
                    <span className="font-medium">{a.action}</span>
                    {a.details && <span className="text-muted-foreground"> — {a.details}</span>}
                  </div>
                  <Badge variant="outline">{new Date(a.createdAt).toLocaleString()}</Badge>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
