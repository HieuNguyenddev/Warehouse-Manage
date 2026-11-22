import { del, getPaged, postJson, putJson } from '../client'
import type { PaginationQuery, SupplierDto } from '../../types/api'

const v1 = '/api/v1'

export const suppliersApi = {
  list: (q?: PaginationQuery) => getPaged<SupplierDto>(`${v1}/suppliers`, q),
  create: (body: Record<string, unknown>) => postJson<SupplierDto>(`${v1}/suppliers`, body),
  update: (id: number, body: Record<string, unknown>) => putJson<SupplierDto>(`${v1}/suppliers/${id}`, body),
  remove: (id: number) => del(`${v1}/suppliers/${id}`),
}
