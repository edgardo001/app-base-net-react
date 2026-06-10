import { Toaster } from 'sonner'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from '@/pages/login'
import { ChangePasswordPage } from '@/pages/change-password'
import { ForgotPasswordPage } from '@/pages/forgot-password'
import { ResetPasswordPage } from '@/pages/reset-password'
import { ConfirmEmailPage } from '@/pages/confirm-email'
import { DashboardPage } from '@/pages/dashboard'
import { UsersPage } from '@/pages/users'
import { RolesPage } from '@/pages/roles'
import { PermissionsPage } from '@/pages/permissions'
import { ProfilePage } from '@/pages/profile'
import { AdminPage } from '@/pages/admin'
import { TipoAPage } from '@/pages/tipo-a'
import { TipoBPage } from '@/pages/tipo-b'
import { TipoCPage } from '@/pages/tipo-c'
import { Layout } from '@/components/layout/layout'
import { ProtectedRoute } from '@/components/auth/protected-route'
import { AuthorizedRoute } from '@/components/auth/authorized-route'

export default function App() {
  return (
    <BrowserRouter>
      <Toaster richColors />
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/change-password" element={<ChangePasswordPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/confirm-email" element={<ConfirmEmailPage />} />
        <Route element={<ProtectedRoute><Layout /></ProtectedRoute>}>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/users" element={<AuthorizedRoute requiredPermission="users:list"><UsersPage /></AuthorizedRoute>} />
          <Route path="/roles" element={<AuthorizedRoute requiredPermission="roles:list"><RolesPage /></AuthorizedRoute>} />
          <Route path="/permissions" element={<AuthorizedRoute requiredPermission="permissions:list"><PermissionsPage /></AuthorizedRoute>} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/admin" element={<AuthorizedRoute requiredPermission="admin:dashboard"><AdminPage /></AuthorizedRoute>} />
          <Route path="/tipo-a" element={<AuthorizedRoute requiredPermission="page-a:view"><TipoAPage /></AuthorizedRoute>} />
          <Route path="/tipo-b" element={<AuthorizedRoute requiredPermission="page-b:view"><TipoBPage /></AuthorizedRoute>} />
          <Route path="/tipo-c" element={<AuthorizedRoute requiredPermission="page-c:view"><TipoCPage /></AuthorizedRoute>} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
