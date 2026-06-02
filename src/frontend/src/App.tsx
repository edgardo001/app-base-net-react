import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from '@/pages/login'
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

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedRoute><Layout /></ProtectedRoute>}>
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/users" element={<UsersPage />} />
          <Route path="/roles" element={<RolesPage />} />
          <Route path="/permissions" element={<PermissionsPage />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="/admin" element={<AdminPage />} />
          <Route path="/tipo-a" element={<TipoAPage />} />
          <Route path="/tipo-b" element={<TipoBPage />} />
          <Route path="/tipo-c" element={<TipoCPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
