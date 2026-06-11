## Why

La plataforma solo ofrece autenticación por email/contraseña. Agregar login con Google OAuth2 reduce la fricción de registro, permite auto-registro inmediato y atrae usuarios que prefieren no crear una cuenta tradicional. Es el primer paso para expandir la plataforma más allá del acceso corporativo.

## What Changes

- Nuevo flujo de autenticación OAuth2 con Google (Authorization Code Flow)
- Auto-registro de usuarios al primer login con Google (cuenta creada automáticamente)
- Vinculación automática con cuenta existente si el email ya está registrado
- Nuevo rol `public` con permiso `page-public:view`
- Nueva página `/publico` con mensaje de bienvenida post-registro
- Instrucciones de configuración de Google Cloud en `README.md`
- No se reemplaza ni modifica el flujo de login existente (email/password)

## Capabilities

### New Capabilities
- `google-oauth`: Autenticación OAuth2 con Google, incluyendo Authorization Code Flow, verificación de ID token, auto-registro y vinculación de cuentas
- `public-role`: Rol "public" con permiso `page-public:view` para usuarios que ingresan vía Google OAuth
- `public-page`: Página `/publico` con contenido informativo de bienvenida, accesible solo para usuarios autenticados vía Google

### Modified Capabilities
<!-- Ninguna - no se modifican capabilities existentes -->

## Impact

- **Domain**: Nuevo campo `ExternalLogins` en entidad `User` para soportar múltiples proveedores OAuth
- **Application**: Nuevo `GoogleLoginCommand` + handler, nuevo `PublicRoleSeed`, nuevo permiso `page-public:view`
- **Infrastructure**: Nuevo `GoogleAuthService` (verificación de ID token + validación de authorization code), seed data para rol `public`
- **WebApi**: Nuevo endpoint `POST /api/auth/google/login` (inicia flujo), `GET /api/auth/google/callback` (callback OAuth)
- **Frontend**: Botón "Sign in with Google" en login, nueva página `/publico`, ruta protegida para public-role
- **Dependencias**: Paquete NuGet `Google.Apis.Auth` para verificación de ID tokens; Google Identity Services library en frontend
- **Configuración**: Nuevas variables de entorno `Authentication__Google__ClientId`, `Authentication__Google__ClientSecret`
- **README.md**: Sección de configuración de Google Cloud Console
