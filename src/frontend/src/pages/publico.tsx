import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useAuthStore } from '@/stores/auth-store'
import { MotoJuego } from '@/components/game/moto-juego'

export function PublicoPage() {
  const user = useAuthStore((s) => s.user)

  return (
    <div className="flex flex-1 flex-col items-center gap-8 p-8">
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

      <div className="w-full max-w-[800px] space-y-2">
        <h2 className="text-xl font-semibold text-center">Minijuego: Moto Mountain</h2>
        <MotoJuego />
      </div>
    </div>
  )
}
