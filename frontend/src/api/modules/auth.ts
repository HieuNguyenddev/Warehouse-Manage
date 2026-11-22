import { postJson } from '../client'
import type { LoginResponse } from '../../types/api'

const v1 = '/api/v1'

export const authApi = {
  adminLogin: (username: string, password: string) =>
    postJson<LoginResponse>(`${v1}/auth/admin/login`, { username, password }),
  customerLogin: (username: string, password: string) =>
    postJson<LoginResponse>(`${v1}/auth/customer/login`, { username, password }),
}
