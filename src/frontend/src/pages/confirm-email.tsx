import { useEffect, useState } from 'react'
import { useSearchParams, Link } from 'react-router-dom'
import { buttonVariants } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import api, { getErrorMessage } from '@/lib/api'

type Status = 'pending' | 'success' | 'error'

export function ConfirmEmailPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') || ''
  const [status, setStatus] = useState<Status>('pending')
  const [message, setMessage] = useState('Confirmando tu correo electrónico...')

  useEffect(() => {
    if (!token) {
      setStatus('error')
      setMessage('Enlace inválido. El token de confirmación no fue proporcionado.')
      return
    }
    let cancelled = false
    api
      .post('/auth/confirm-email', { token })
      .then(() => {
        if (cancelled) return
        setStatus('success')
        setMessage('Tu correo electrónico ha sido confirmado. Ya puedes iniciar sesión.')
      })
      .catch((err: unknown) => {
        if (cancelled) return
        setStatus('error')
        setMessage(getErrorMessage(err, 'No se pudo confirmar el correo. El enlace puede haber expirado.'))
      })
    return () => {
      cancelled = true
    }
  }, [token])

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/50">
      <Card className="w-full max-w-sm">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">
            {status === 'pending' && 'Confirmando correo'}
            {status === 'success' && 'Correo confirmado'}
            {status === 'error' && 'Enlace inválido o expirado'}
          </CardTitle>
          <CardDescription>{message}</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col items-center gap-3">
          {status === 'success' && (
            <Link to="/login" className={buttonVariants({ className: 'w-full' })}>
              Iniciar sesión
            </Link>
          )}
          {status === 'error' && (
            <>
              <p className="text-sm text-muted-foreground text-center">
                Si tu enlace ha expirado, contacta al administrador para generar uno nuevo.
              </p>
              <Link
                to="/login"
                className={buttonVariants({ variant: 'outline', className: 'w-full' })}
              >
                Ir al inicio de sesión
              </Link>
            </>
          )}
          {status === 'pending' && (
            <div
              className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent"
              aria-label="Confirmando"
            />
          )}
        </CardContent>
      </Card>
    </div>
  )
}
