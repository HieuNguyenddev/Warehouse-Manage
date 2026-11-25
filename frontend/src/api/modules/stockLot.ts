import { getPaged } from '../client'
import type { PaginationQuery, StockLotDto } from '../../types/api'

const v1 = '/api/v1'

export const stockLotsApi = {
  list: (q?: PaginationQuery & { warehouseId?: number; productId?: number }) =>
    getPaged<StockLotDto>(`${v1}/stock-lots`, q),

  nearExpiry: (q?: {
    withinDays?: number
    warehouseId?: number
    pageNumber?: number
    pageSize?: number
  }) => getPaged<StockLotDto>(`${v1}/stock-lots/near-expiry`, q),
}
