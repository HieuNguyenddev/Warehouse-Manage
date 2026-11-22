import { apiRequest, del, getOne, getPaged, postForm, postJson, putJson } from '../client'
import type { PaginationQuery, ProductDto } from '../../types/api'

const v1 = '/api/v1'

export const productsApi = {
  list: (q?: PaginationQuery & { categoryId?: number }) => getPaged<ProductDto>(`${v1}/products`, q),
  get: (id: number) => getOne<ProductDto>(`${v1}/products/${id}`),
  create: (body: Record<string, unknown>) => postJson<ProductDto>(`${v1}/products`, body),
  update: (id: number, body: Record<string, unknown>) => putJson<ProductDto>(`${v1}/products/${id}`, body),
  remove: (id: number) => del(`${v1}/products/${id}`),
  uploadImages: (id: number, files: File[]) => {
    const form = new FormData()
    files.forEach((f) => form.append('files', f))
    return postForm<ProductDto>(`${v1}/products/${id}/images`, form)
  },
  replaceImage: (id: number, imageId: number, file: File) => {
    const form = new FormData()
    form.append('file', file)
    return apiRequest<ProductDto>(`${v1}/products/${id}/images/${imageId}`, {
      method: 'PUT',
      body: form,
    })
  },
  deleteImage: (id: number, imageId: number) =>
    apiRequest<ProductDto>(`${v1}/products/${id}/images/${imageId}`, { method: 'DELETE' }),
  setPrimary: (id: number, imageId: number) =>
    putJson<ProductDto>(`${v1}/products/${id}/images/${imageId}/primary`),
}
