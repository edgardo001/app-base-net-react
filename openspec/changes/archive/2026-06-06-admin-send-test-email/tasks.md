## 1. Backend — Template y Endpoint

- [x] 1.1 Crear template `test-email.html` como recurso incrustado en `Infrastructure/Email/Templates/`
- [x] 1.2 Agregar configuración del template en `appsettings.json` dentro de `Email.Templates`
- [x] 1.3 Agregar endpoint `POST /api/admin/test-email` en `AdminController.cs` que recibe `{ to: string }`, renderiza el template y envía el correo
- [x] 1.4 Agregar auditoría (`TestEmailSent`) en el endpoint

## 2. Frontend — UI en /admin

- [x] 2.1 Agregar sección "Correo de Prueba" en `admin.tsx` con un `Card` que contenga un input de email y botón "Enviar"
- [x] 2.2 Integrar `sonner` toast para mostrar resultado (éxito/error)

## 3. Verificación

- [x] 3.1 Verificar que `dotnet build` compila sin errores
- [x] 3.2 Verificar que `npm run build` compila sin errores
