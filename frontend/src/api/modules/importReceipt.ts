import { del, getOne, getPaged, postJson, putJson } from '../client'
import type { ImportReceiptDto, PaginationQuery } from '../../types/api'

const v1 = '/api/v1'

export const importsApi = {
  list: (q?: PaginationQuery & { warehouseId?: number; supplierId?: number; status?: string }) =>
    getPaged<ImportReceiptDto>(`${v1}/import-receipts`, q),
  get: (id: number) => getOne<ImportReceiptDto>(`${v1}/import-receipts/${id}`),
  create: (body: Record<string, unknown>) => postJson<ImportReceiptDto>(`${v1}/import-receipts`, body),
  update: (id: number, body: Record<string, unknown>) =>
    putJson<ImportReceiptDto>(`${v1}/import-receipts/${id}`, body),
  confirm: (id: number) => postJson<ImportReceiptDto>(`${v1}/import-receipts/${id}/confirm`),
  cancel: (id: number) => postJson<ImportReceiptDto>(`${v1}/import-receipts/${id}/cancel`),
  remove: (id: number) => del(`${v1}/import-receipts/${id}`),
}
