import { del, getOne, getPaged, postJson, putJson } from '../client'
import type { PaginationQuery, RoleDto } from '../../types/api'

const v1 = '/api/v1'

export const rolesApi = {
  list: (q?: PaginationQuery) => getPaged<RoleDto>(`${v1}/roles`, q),
  get: (id: number) => getOne<RoleDto>(`${v1}/roles/${id}`),
  create: (body: Partial<RoleDto>) => postJson<RoleDto>(`${v1}/roles`, body),
  update: (id: number, body: Partial<RoleDto>) => putJson<RoleDto>(`${v1}/roles/${id}`, body),
  remove: (id: number) => del(`${v1}/roles/${id}`),
}
