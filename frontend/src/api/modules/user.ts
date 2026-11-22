import { del, getOne, getPaged, postJson, putJson } from '../client'
import type { PaginationQuery, UserDto } from '../../types/api'

const v1 = '/api/v1'

export const usersApi = {
  list: (q?: PaginationQuery) => getPaged<UserDto>(`${v1}/users`, q),
  get: (id: number) => getOne<UserDto>(`${v1}/users/${id}`),
  create: (body: Record<string, unknown>) => postJson<UserDto>(`${v1}/users`, body),
  update: (id: number, body: Record<string, unknown>) => putJson<UserDto>(`${v1}/users/${id}`, body),
  remove: (id: number) => del(`${v1}/users/${id}`),
}
