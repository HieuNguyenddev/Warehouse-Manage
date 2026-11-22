import { getOne, getPaged, postForm, postJson, putJson } from '../client'
import type {
  CategoryDto,
  CustomerDto,
  InvoiceDto,
  PaginationQuery,
  ProductDto,
  StoreCheckoutResult,
} from '../../types/api'

const v1 = '/api/v1'

export const storeApi = {
  products: (q?: PaginationQuery & { categoryId?: number }) =>
    getPaged<ProductDto>(`${v1}/store/products`, q),
  product: (id: number) => getOne<ProductDto>(`${v1}/store/products/${id}`),
  categories: (q?: PaginationQuery) => getPaged<CategoryDto>(`${v1}/store/categories`, q),
  me: () => getOne<CustomerDto>(`${v1}/store/me`),
  updateMe: (body: {
    name: string
    contactName?: string | null
    phone?: string | null
    email?: string | null
    address?: string | null
    note?: string | null
    newPassword?: string | null
  }) => putJson<CustomerDto>(`${v1}/store/me`, body),
  uploadAvatar: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return postForm<CustomerDto>(`${v1}/store/me/avatar`, form)
  },
  checkout: (body: {
    items: { productId: number; quantity: number }[]
    note?: string | null
    shippingAddress?: string | null
  }) => postJson<StoreCheckoutResult>(`${v1}/store/checkout`, body),
  invoices: (q?: PaginationQuery) => getPaged<InvoiceDto>(`${v1}/store/invoices`, q),
  invoice: (id: number) => getOne<InvoiceDto>(`${v1}/store/invoices/${id}`),
}
