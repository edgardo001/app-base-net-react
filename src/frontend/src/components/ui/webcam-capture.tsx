import { useState, useRef, useEffect, useCallback } from 'react'
import { Button } from '@/components/ui/button'
import { Camera, RefreshCw } from 'lucide-react'

interface WebcamCaptureProps {
  onCapture: (blob: Blob) => void
  onCancel: () => void
}

export function WebcamCapture({ onCapture, onCancel }: WebcamCaptureProps) {
  const videoRef = useRef<HTMLVideoElement>(null)
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const [stream, setStream] = useState<MediaStream | null>(null)
  const [captured, setCaptured] = useState<string | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    let currentStream: MediaStream | null = null
    navigator.mediaDevices.getUserMedia({ video: { width: 512, height: 512, facingMode: 'user' } })
      .then((s) => {
        if (active) {
          currentStream = s
          setStream(s)
          if (videoRef.current) videoRef.current.srcObject = s
        }
      })
      .catch(() => {
        if (active) setError('No se pudo acceder a la cámara')
      })
    return () => {
      active = false
      currentStream?.getTracks().forEach((t: MediaStreamTrack) => t.stop())
    }
  }, [])

  const capture = useCallback(() => {
    const video = videoRef.current
    const canvas = canvasRef.current
    if (!video || !canvas) return
    canvas.width = video.videoWidth
    canvas.height = video.videoHeight
    const ctx = canvas.getContext('2d')
    if (!ctx) return
    ctx.drawImage(video, 0, 0)
    const dataUrl = canvas.toDataURL('image/jpeg', 0.9)
    setCaptured(dataUrl)
    stream?.getTracks().forEach(t => t.stop())
  }, [stream])

  const retake = async () => {
    setCaptured(null)
    try {
      const s = await navigator.mediaDevices.getUserMedia({ video: { width: 512, height: 512, facingMode: 'user' } })
      setStream(s)
      if (videoRef.current) videoRef.current.srcObject = s
    } catch {
      setError('No se pudo reiniciar la cámara')
    }
  }

  const confirmCapture = () => {
    if (!captured) return
    fetch(captured)
      .then(r => r.blob())
      .then(blob => onCapture(blob))
  }

  if (error) {
    return (
      <div className="flex flex-col items-center gap-3 py-6">
        <p className="text-sm text-destructive">{error}</p>
        <Button variant="outline" size="sm" onClick={onCancel}>Volver a Subir</Button>
      </div>
    )
  }

  return (
    <div className="flex flex-col items-center gap-3">
      <div className="relative overflow-hidden rounded-full">
        {captured ? (
          <img src={captured} alt="Captura" className="h-40 w-40 object-cover" />
        ) : (
          <video
            ref={videoRef}
            autoPlay
            playsInline
            muted
            className="h-40 w-40 object-cover scale-x-[-1]"
          />
        )}
        <canvas ref={canvasRef} className="hidden" />
      </div>
      <div className="flex gap-2">
        {captured ? (
          <>
            <Button variant="outline" size="sm" onClick={retake}>
              <RefreshCw className="mr-1 h-3.5 w-3.5" /> Volver a tomar
            </Button>
            <Button size="sm" onClick={confirmCapture}>Usar esta foto</Button>
          </>
        ) : (
          <Button size="sm" onClick={capture}>
            <Camera className="mr-1 h-3.5 w-3.5" /> Capturar
          </Button>
        )}
      </div>
    </div>
  )
}
