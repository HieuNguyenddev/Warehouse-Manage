import { getOne, getPaged, postJson, putJson } from '../client'
import type {
  PaginationQuery,
  WarehouseStockDto,
  WarehouseStockSummaryDto,
} from '../../types/api'

const v1 = '/api/v1'

export const stocksApi = {
  list: (q?: PaginationQuery & {
    warehouseId?: number
    productId?: number
    lowStockOnly?: boolean
    outOfStockOnly?: boolean
  }) => getPaged<WarehouseStockDto>(`${v1}/warehouse-stocks`, q),
  summary: (warehouseId?: number) =>
    getOne<WarehouseStockSummaryDto>(
      `${v1}/warehouse-stocks/summary`,
      warehouseId ? { warehouseId } : undefined,
    ),
  get: (id: number) => getOne<WarehouseStockDto>(`${v1}/warehouse-stocks/${id}`),
  createOpening: (body: {
    warehouseId: number
    productId: number
    quantity: number
    minQuantity?: number | null
    reason: string
  }) => postJson<WarehouseStockDto>(`${v1}/warehouse-stocks`, body),
  adjust: (id: number, body: { quantity: number; reason: string }) =>
    postJson<WarehouseStockDto>(`${v1}/warehouse-stocks/${id}/adjust`, body),
  updateMin: (id: number, body: { minQuantity?: number | null }) =>
    putJson<WarehouseStockDto>(`${v1}/warehouse-stocks/${id}/min`, body),
}
