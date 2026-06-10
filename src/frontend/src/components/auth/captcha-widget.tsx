import { useEffect, useState } from 'react'
import { Turnstile, type TurnstileProps } from '@marsidev/react-turnstile'
import api from '@/lib/api'

interface CaptchaWidgetProps {
  onToken: (token: string | null) => void
}

export function CaptchaWidget({ onToken }: CaptchaWidgetProps) {
  const [config, setConfig] = useState<{ enabled: boolean; siteKey: string | null }>({ enabled: false, siteKey: null })
  const [loaded, setLoaded] = useState(false)

  useEffect(() => {
    api.get('/features').then(({ data }) => {
      setConfig({ enabled: data.captchaEnabled === true, siteKey: data.captchaSiteKey ?? null })
    }).catch(() => setConfig({ enabled: false, siteKey: null }))
  }, [])

  if (!config.enabled || !config.siteKey) return null

  // Render once when the key is known
  if (!loaded) {
    setLoaded(true)
  }

  const handleSuccess: TurnstileProps['onSuccess'] = (token) => {
    onToken(token)
  }

  const handleExpire: TurnstileProps['onExpire'] = () => {
    onToken(null)
  }

  return (
    <div className="flex justify-center">
      <Turnstile
        siteKey={config.siteKey}
        onSuccess={handleSuccess}
        onExpire={handleExpire}
        options={{ theme: 'auto' }}
      />
    </div>
  )
}
