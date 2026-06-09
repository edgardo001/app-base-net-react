import { useState, useRef, useCallback } from 'react'
import { Button } from '@/components/ui/button'
import { X, Upload, Camera } from 'lucide-react'
import { WebcamCapture } from '@/components/ui/webcam-capture'

const ALLOWED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp']
const MAX_FILE_SIZE = 5 * 1024 * 1024 // 5MB

interface AvatarUploadProps {
  open: boolean
  onClose: () => void
  onUpload: (file: File) => Promise<void>
}

type Tab = 'upload' | 'webcam'

export function AvatarUpload({ open, onClose, onUpload }: AvatarUploadProps) {
  const [tab, setTab] = useState<Tab>('upload')
  const [dragOver, setDragOver] = useState(false)
  const [preview, setPreview] = useState<string | null>(null)
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [error, setError] = useState('')
  const [uploading, setUploading] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  const validateFile = (file: File): string | null => {
    const ext = '.' + file.name.split('.').pop()?.toLowerCase()
    if (!ALLOWED_EXTENSIONS.includes(ext)) {
      return `Tipo de archivo no permitido. Permitidos: ${ALLOWED_EXTENSIONS.join(', ')}`
    }
    if (file.size > MAX_FILE_SIZE) {
      return `El archivo excede el tamaño máximo de ${MAX_FILE_SIZE / 1024 / 1024}MB`
    }
    return null
  }

  const handleFile = useCallback((file: File) => {
    const err = validateFile(file)
    if (err) {
      setError(err)
      return
    }
    setError('')
    setSelectedFile(file)
    const reader = new FileReader()
    reader.onload = () => setPreview(reader.result as string)
    reader.readAsDataURL(file)
  }, [])

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setDragOver(false)
    const file = e.dataTransfer.files[0]
    if (file) handleFile(file)
  }, [handleFile])

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) handleFile(file)
  }

  const handleUpload = async () => {
    if (!selectedFile) return
    setUploading(true)
    try {
      await onUpload(selectedFile)
      setPreview(null)
      setSelectedFile(null)
      onClose()
    } catch {
      setError('Error al subir el archivo')
    } finally {
      setUploading(false)
    }
  }

  const handleWebcamCapture = (blob: Blob) => {
    const file = new File([blob], 'webcam-avatar.jpg', { type: 'image/jpeg' })
    handleFile(file)
    setTab('upload')
  }

  const handleClose = () => {
    setPreview(null)
    setSelectedFile(null)
    setError('')
    setTab('upload')
    onClose()
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="w-full max-w-md rounded-lg border bg-background p-6 shadow-lg">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold">Actualizar Avatar</h2>
          <Button variant="ghost" size="icon" onClick={handleClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        <div className="flex gap-1 mb-4 rounded-md bg-muted p-1">
          <button
            className={`flex-1 rounded-sm px-3 py-1.5 text-sm font-medium transition-colors ${
              tab === 'upload' ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
            }`}
            onClick={() => setTab('upload')}
          >
            <Upload className="mr-1 inline h-3.5 w-3.5" /> Subir
          </button>
          <button
            className={`flex-1 rounded-sm px-3 py-1.5 text-sm font-medium transition-colors ${
              tab === 'webcam' ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
            }`}
            onClick={() => setTab('webcam')}
          >
            <Camera className="mr-1 inline h-3.5 w-3.5" /> Cámara
          </button>
        </div>

        {error && (
          <div className="mb-4 rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</div>
        )}

        {tab === 'upload' ? (
          <div className="space-y-4">
            {preview ? (
              <div className="flex flex-col items-center gap-3">
                <img src={preview} alt="Preview" className="h-32 w-32 rounded-full object-cover" />
                <Button variant="outline" size="sm" onClick={() => { setPreview(null); setSelectedFile(null) }}>
                  Cambiar imagen
                </Button>
              </div>
            ) : (
              <div
                className={`flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-8 transition-colors cursor-pointer ${
                  dragOver ? 'border-primary bg-primary/5' : 'border-muted-foreground/25 hover:border-primary/50'
                }`}
                onDragOver={(e) => { e.preventDefault(); setDragOver(true) }}
                onDragLeave={() => setDragOver(false)}
                onDrop={handleDrop}
                onClick={() => inputRef.current?.click()}
              >
                <Upload className="mb-2 h-8 w-8 text-muted-foreground" />
                <p className="text-sm text-muted-foreground">
                  Arrastra una imagen o haz clic para seleccionar
                </p>
                <p className="mt-1 text-xs text-muted-foreground">
                  JPG, PNG o WebP — máximo 5MB
                </p>
                <input
                  ref={inputRef}
                  type="file"
                  accept={ALLOWED_EXTENSIONS.join(',')}
                  className="hidden"
                  onChange={handleInputChange}
                />
              </div>
            )}
          </div>
        ) : (
          <WebcamCapture onCapture={handleWebcamCapture} onCancel={() => setTab('upload')} />
        )}

        <div className="flex justify-end gap-2 mt-4">
          <Button variant="outline" onClick={handleClose} disabled={uploading}>Cancelar</Button>
          <Button onClick={handleUpload} disabled={!selectedFile || uploading}>
            {uploading ? 'Subiendo...' : 'Guardar'}
          </Button>
        </div>
      </div>
    </div>
  )
}
