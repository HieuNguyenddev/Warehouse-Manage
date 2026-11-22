import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import { authApi } from '../api/endpoints'
import { clearSession, getPortal, getStoredUser, getToken, setSession } from '../api/client'
import type { LoginResponse, UserDto } from '../types/api'

type Portal = 'admin' | 'customer'

interface AuthState {
  token: string | null
  portal: Portal | null
  user: UserDto | null
  customerId: number | null
  loginAdmin: (username: string, password: string) => Promise<LoginResponse>
  loginCustomer: (username: string, password: string) => Promise<LoginResponse>
  logout: () => void
  isAdmin: boolean
  isCustomer: boolean
}

const AuthContext = createContext<AuthState | null>(null)

function applyLogin(res: LoginResponse) {
  setSession(res.token, res.portal, res.user)
  return res
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => getToken())
  const [portal, setPortal] = useState<Portal | null>(() => (getPortal() as Portal | null))
  const [user, setUser] = useState<UserDto | null>(() => getStoredUser<UserDto>())
  const [customerId, setCustomerId] = useState<number | null>(null)

  const loginAdmin = useCallback(async (username: string, password: string) => {
    const res = await authApi.adminLogin(username, password)
    applyLogin(res)
    setToken(res.token)
    setPortal(res.portal as Portal)
    setUser(res.user)
    setCustomerId(res.customerId ?? null)
    return res
  }, [])

  const loginCustomer = useCallback(async (username: string, password: string) => {
    const res = await authApi.customerLogin(username, password)
    applyLogin(res)
    setToken(res.token)
    setPortal(res.portal as Portal)
    setUser(res.user)
    setCustomerId(res.customerId ?? null)
    return res
  }, [])

  const logout = useCallback(() => {
    clearSession()
    setToken(null)
    setPortal(null)
    setUser(null)
    setCustomerId(null)
  }, [])

  const value = useMemo(
    () => ({
      token,
      portal,
      user,
      customerId,
      loginAdmin,
      loginCustomer,
      logout,
      isAdmin: !!token && portal === 'admin',
      isCustomer: !!token && portal === 'customer',
    }),
    [token, portal, user, customerId, loginAdmin, loginCustomer, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
