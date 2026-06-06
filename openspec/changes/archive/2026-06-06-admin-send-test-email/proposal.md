## Why

Los administradores necesitan poder verificar que la configuración SMTP del sistema funciona correctamente sin tener que esperar a un evento real (creación de usuario, recuperación de contraseña, etc.). Actualmente no hay forma de enviar un correo de prueba desde la interfaz de administración.

## What Changes

- **Nuevo endpoint** `POST /api/admin/test-email` que recibe un destinatario y envía un correo de prueba
- **Nuevo template de email** `test-email.html` para el correo de prueba
- **Nuevo formulario en la página `/admin`** con un campo de texto para el destinatario y un botón "Enviar Correo de Prueba"
- Toast de éxito/error en el frontend

## Capabilities

### New Capabilities
- `admin-test-email`: Capacidad para que un SuperAdmin envíe un correo de prueba para verificar la configuración SMTP

### Modified Capabilities


## Impact

- **Backend**: `AdminController.cs` — nuevo endpoint `POST /api/admin/test-email`
- **Infrastructure**: Nuevo template `test-email.html` en `Email/Templates/`
- **Frontend**: `admin.tsx` — nuevo formulario con campo de texto y botón
