import { del, getPaged, postJson, putJson } from '../client'
import type { PaginationQuery, WarehouseDto } from '../../types/api'

const v1 = '/api/v1'

export const warehousesApi = {
  list: (q?: PaginationQuery) => getPaged<WarehouseDto>(`${v1}/warehouses`, q),
  create: (body: Record<string, unknown>) => postJson<WarehouseDto>(`${v1}/warehouses`, body),
  update: (id: number, body: Record<string, unknown>) => putJson<WarehouseDto>(`${v1}/warehouses/${id}`, body),
  remove: (id: number) => del(`${v1}/warehouses/${id}`),
}
