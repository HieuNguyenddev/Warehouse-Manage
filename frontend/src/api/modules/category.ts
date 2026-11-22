import { del, getOne, getPaged, postJson, putJson } from '../client'
import type { CategoryDto, PaginationQuery } from '../../types/api'

const v1 = '/api/v1'

export const categoriesApi = {
  list: (q?: PaginationQuery) => getPaged<CategoryDto>(`${v1}/categories`, q),
  get: (id: number) => getOne<CategoryDto>(`${v1}/categories/${id}`),
  create: (body: Record<string, unknown>) => postJson<CategoryDto>(`${v1}/categories`, body),
  update: (id: number, body: Record<string, unknown>) => putJson<CategoryDto>(`${v1}/categories/${id}`, body),
  remove: (id: number) => del(`${v1}/categories/${id}`),
}
