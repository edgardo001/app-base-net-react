// Decodifica el payload de un JWT firmado por JwtService (.NET).
// El servidor escribe el rol del usuario como un claim con el URI
// largo de ClaimTypes.Role porque MapInboundClaims = false y
// DefaultInboundClaimTypeMap.Clear() estan configurados en DI
// (DependencyInjection.cs). El nombre corto "role" NO se usa.
//
// No se valida la firma aqui: el navegador ya lo valida al hacer
// la primera peticion al backend. Esto es solo para extraer el rol
// de la sesion actual sin un round-trip extra al servidor.

const ROLE_CLAIM_URI = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

function base64UrlDecode(input: string): string {
  // JWT usa base64url (sin padding, con - y _ en lugar de + y /)
  const padded = input.replace(/-/g, '+').replace(/_/g, '/').padEnd(input.length + ((4 - (input.length % 4)) % 4), '=')
  return atob(padded)
}

export interface JwtPayload {
  sub?: string
  email?: string
  exp?: number
  iat?: number
  permission?: string[]
  // El claim de rol puede aparecer como string (un rol) o string[] (varios)
  [key: string]: string | string[] | number | undefined
}

export function decodeJwt(token: string): JwtPayload | null {
  try {
    const parts = token.split('.')
    if (parts.length !== 3) return null
    const payload = JSON.parse(base64UrlDecode(parts[1])) as JwtPayload
    return payload
  } catch {
    return null
  }
}

export function extractRoles(token: string | null | undefined): string[] {
  if (!token) return []
  const payload = decodeJwt(token)
  if (!payload) return []
  const claim = payload[ROLE_CLAIM_URI]
  if (Array.isArray(claim)) return claim.filter((r): r is string => typeof r === 'string')
  if (typeof claim === 'string') return [claim]
  return []
}

export function extractPermissions(token: string | null | undefined): string[] {
  if (!token) return []
  const payload = decodeJwt(token)
  if (!payload) return []
  const claim = payload['permission']
  if (Array.isArray(claim)) return claim.filter((p): p is string => typeof p === 'string')
  if (typeof claim === 'string') return [claim]
  return []
}

export const ROLE_CLAIM_NAMES = {
  // El nombre exacto que el backend usa para emitir el claim de rol
  ClaimTypesRole: ROLE_CLAIM_URI,
  // Nombres canonicos de los dos roles que pueden probar correos
  Admin: 'Admin',
  SuperAdmin: 'SuperAdmin',
} as const
