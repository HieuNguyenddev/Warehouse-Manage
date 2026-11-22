import { Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import type { ReactNode } from 'react'

export function RequireAdmin({ children }: { children: ReactNode }) {
  const { isAdmin } = useAuth()
  if (!isAdmin) return <Navigate to="/admin/login" replace />
  return children
}

export function RequireCustomer({ children }: { children: ReactNode }) {
  const { isCustomer } = useAuth()
  if (!isCustomer) return <Navigate to="/store/login" replace />
  return children
}
