import { getOne, getPaged } from '../client'
import type { PaginationQuery, StockTransactionDto } from '../../types/api'

const v1 = '/api/v1'

export const transactionsApi = {
  list: (q?: PaginationQuery & Record<string, unknown>) =>
    getPaged<StockTransactionDto>(`${v1}/stock-transactions`, q),
  get: (id: number) => getOne<StockTransactionDto>(`${v1}/stock-transactions/${id}`),
}
