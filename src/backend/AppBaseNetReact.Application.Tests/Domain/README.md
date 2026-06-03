# Domain — Tests de Entidades de Dominio

Pruebas unitarias para el comportamiento de las entidades del dominio (User, Role, RefreshToken, Permission, etc.).

Ejemplos:
- `User_Create_ValidInput_ReturnsUser`
- `User_IncrementFailedAccess_ExceedsMax_LocksAccount`
- `RefreshToken_Revoke_SetsRevokedAt`
- `Role_AddPermission_Duplicate_Ignores`
