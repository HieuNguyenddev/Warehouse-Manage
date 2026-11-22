import type { ApiResponse, PagedResult, PaginationQuery } from '../types/api'

const TOKEN_KEY = 'wm_token'
const PORTAL_KEY = 'wm_portal'
const USER_KEY = 'wm_user'

export function getToken() {
  return localStorage.getItem(TOKEN_KEY)
}

export function getPortal() {
  return localStorage.getItem(PORTAL_KEY)
}

export function getStoredUser<T = unknown>() {
  const raw = localStorage.getItem(USER_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as T
  } catch {
    return null
  }
}

export function setSession(token: string, portal: string, user: unknown) {
  localStorage.setItem(TOKEN_KEY, token)
  localStorage.setItem(PORTAL_KEY, portal)
  localStorage.setItem(USER_KEY, JSON.stringify(user))
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(PORTAL_KEY)
  localStorage.removeItem(USER_KEY)
}

export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

function toQuery(params?: Record<string, unknown>) {
  if (!params) return ''
  const q = new URLSearchParams()
  Object.entries(params).forEach(([k, v]) => {
    if (v === undefined || v === null || v === '') return
    q.set(k, String(v))
  })
  const s = q.toString()
  return s ? `?${s}` : ''
}

/** Chỉ gắn JWT đúng cổng — tránh admin token gọi API Customer (và ngược lại) → 401/403 */
function shouldAttachToken(path: string, portal: string | null) {
  if (!portal) return false
  if (path.includes('/api/v1/auth/')) return false
  if (path.includes('/api/v1/media')) return false
  if (path.includes('/api/v1/store')) return portal === 'customer'
  return portal === 'admin'
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const headers = new Headers(options.headers)
  const token = getToken()
  const portal = getPortal()
  if (token && shouldAttachToken(path, portal)) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const isForm = typeof FormData !== 'undefined' && options.body instanceof FormData
  if (!isForm && options.body && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  const res = await fetch(path, { ...options, headers })
  if (res.status === 204) return undefined as T

  const json = (await res.json().catch(() => null)) as ApiResponse<T> | null
  if (!res.ok) {
    // Token hết hạn / không hợp lệ → xóa session và về trang đăng nhập
    if (res.status === 401) {
      const loginPath = portal === 'customer' ? '/store/login' : '/admin/login'
      clearSession()
      if (typeof window !== 'undefined' && !window.location.pathname.includes('/login')) {
        window.location.assign(loginPath)
      }
      throw new ApiError(401, json?.message || 'Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.')
    }
    throw new ApiError(res.status, json?.message || res.statusText || 'Request failed')
  }
  if (json && typeof json === 'object' && 'data' in json) {
    return json.data
  }
  return json as T
}

export function getPaged<T>(path: string, query?: PaginationQuery & Record<string, unknown> | Record<string, unknown>) {
  return apiRequest<PagedResult<T>>(`${path}${toQuery(query as Record<string, unknown> | undefined)}`)
}

export function getOne<T>(path: string, query?: Record<string, unknown>) {
  return apiRequest<T>(`${path}${toQuery(query)}`)
}

export function postJson<T>(path: string, body?: unknown) {
  return apiRequest<T>(path, {
    method: 'POST',
    body: body === undefined ? undefined : JSON.stringify(body),
  })
}

export function putJson<T>(path: string, body?: unknown) {
  return apiRequest<T>(path, {
    method: 'PUT',
    body: body === undefined ? undefined : JSON.stringify(body),
  })
}

export function del(path: string) {
  return apiRequest<void>(path, { method: 'DELETE' })
}

export function postForm<T>(path: string, form: FormData) {
  return apiRequest<T>(path, { method: 'POST', body: form })
}

export function mediaUrl(path?: string | null) {
  if (!path) return ''
  if (path.startsWith('http') || path.startsWith('blob:') || path.startsWith('data:')) return path
  return path.startsWith('/') ? path : `/${path}`
}
