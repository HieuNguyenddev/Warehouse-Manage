import { del, getPaged, postJson, putJson } from '../client'
import type { PaginationQuery, UnitDto } from '../../types/api'

const v1 = '/api/v1'

export const unitsApi = {
  list: (q?: PaginationQuery) => getPaged<UnitDto>(`${v1}/units`, q),
  create: (body: Record<string, unknown>) => postJson<UnitDto>(`${v1}/units`, body),
  update: (id: number, body: Record<string, unknown>) => putJson<UnitDto>(`${v1}/units/${id}`, body),
  remove: (id: number) => del(`${v1}/units/${id}`),
}
