import { del, getOne, getPaged, postJson, putJson } from '../client'
import type { ExportReceiptDto, PaginationQuery } from '../../types/api'

const v1 = '/api/v1'

export const exportsApi = {
  list: (q?: PaginationQuery & { warehouseId?: number; customerId?: number; status?: string; source?: string }) =>
    getPaged<ExportReceiptDto>(`${v1}/export-receipts`, q),
  get: (id: number) => getOne<ExportReceiptDto>(`${v1}/export-receipts/${id}`),
  create: (body: Record<string, unknown>) => postJson<ExportReceiptDto>(`${v1}/export-receipts`, body),
  update: (id: number, body: Record<string, unknown>) =>
    putJson<ExportReceiptDto>(`${v1}/export-receipts/${id}`, body),
  confirm: (id: number) => postJson<ExportReceiptDto>(`${v1}/export-receipts/${id}/confirm`),
  cancel: (id: number) => postJson<ExportReceiptDto>(`${v1}/export-receipts/${id}/cancel`),
  remove: (id: number) => del(`${v1}/export-receipts/${id}`),
}
