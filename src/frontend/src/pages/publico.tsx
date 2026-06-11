import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useAuthStore } from '@/stores/auth-store'

export function PublicoPage() {
  const user = useAuthStore((s) => s.user)

  return (
    <div className="flex flex-1 items-center justify-center p-8">
      <Card className="max-w-xl text-center">
        <CardHeader>
          <CardTitle className="text-3xl">¡Bienvenido{user ? `, ${user.firstName}` : ''}!</CardTitle>
          <CardDescription className="text-base mt-2 leading-relaxed">
            Hola, gracias por registrarte en mi plataforma, no haremos nada raro con tus datos,
            ya que esta es solo una app de aprendizaje, tal vez en algún futuro verás algo muy
            interesante en este lugar, pero de momento solo tienes acceso a esta página.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">
            {user?.email && `Registrado como: ${user.email}`}
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
