import { del, getOne, getPaged, postJson, putJson } from '../client'
import type { PaginationQuery, StocktakeDto } from '../../types/api'

const v1 = '/api/v1'

export const stocktakesApi = {
  list: (q?: PaginationQuery & { warehouseId?: number; status?: string }) =>
    getPaged<StocktakeDto>(`${v1}/stocktakes`, q),
  get: (id: number) => getOne<StocktakeDto>(`${v1}/stocktakes/${id}`),
  create: (body: Record<string, unknown>) => postJson<StocktakeDto>(`${v1}/stocktakes`, body),
  createAuto: (body: {
    warehouseId: number
    note?: string | null
    matchSystemQuantity?: boolean
    lowStockOnly?: boolean
  }) => postJson<StocktakeDto>(`${v1}/stocktakes/auto`, body),
  update: (id: number, body: Record<string, unknown>) => putJson<StocktakeDto>(`${v1}/stocktakes/${id}`, body),
  confirm: (id: number) => postJson<StocktakeDto>(`${v1}/stocktakes/${id}/confirm`),
  cancel: (id: number) => postJson<StocktakeDto>(`${v1}/stocktakes/${id}/cancel`),
  remove: (id: number) => del(`${v1}/stocktakes/${id}`),
}
