import { del, getOne, getPaged, postForm, postJson, putJson } from '../client'
import type { CustomerDto, PaginationQuery } from '../../types/api'

const v1 = '/api/v1'

export const customersApi = {
  list: (q?: PaginationQuery) => getPaged<CustomerDto>(`${v1}/customers`, q),
  get: (id: number) => getOne<CustomerDto>(`${v1}/customers/${id}`),
  create: (body: Record<string, unknown>) => postJson<CustomerDto>(`${v1}/customers`, body),
  update: (id: number, body: Record<string, unknown>) => putJson<CustomerDto>(`${v1}/customers/${id}`, body),
  remove: (id: number) => del(`${v1}/customers/${id}`),
  uploadAvatar: (id: number, file: File) => {
    const form = new FormData()
    form.append('file', file)
    return postForm<CustomerDto>(`${v1}/customers/${id}/avatar`, form)
  },
}
