export interface ApiResponse<T> {
  success: boolean
  message: string
  data: T
}

export interface PagedResult<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalRecords: number
  totalPages: number
}

export interface PaginationQuery {
  search?: string
  pageNumber?: number
  pageSize?: number
  [key: string]: unknown
}

export interface UserDto {
  id: number
  username: string
  fullName: string
  email: string
  phone?: string | null
  isActive: boolean
  roleId: number
  roleName?: string | null
}

export interface LoginResponse {
  token: string
  expiresAt: string
  portal: 'admin' | 'customer' | string
  user: UserDto
  customerId?: number | null
}

export interface NamedEntity {
  id: number
  name: string
  description?: string | null
  isActive: boolean
}

export interface CategoryDto extends NamedEntity {}

export interface UnitDto {
  id: number
  name: string
  symbol: string
  description?: string | null
  isActive: boolean
}

export interface RoleDto {
  id: number
  name: string
  description?: string | null
}

export interface SupplierDto {
  id: number
  name: string
  contactName?: string | null
  phone?: string | null
  email?: string | null
  address?: string | null
  taxCode?: string | null
  note?: string | null
  isActive: boolean
}

export interface CustomerDto extends SupplierDto {
  avatarUrl?: string | null
  userId?: number | null
  username?: string | null
  roleName?: string | null
}

export interface WarehouseDto {
  id: number
  code: string
  name: string
  address?: string | null
  phone?: string | null
  managerName?: string | null
  note?: string | null
  isActive: boolean
  /** General | ColdChain | Controlled | Emergency | MedicalSupply */
  warehouseType?: string | null
}

export interface ProductImageDto {
  id: number
  imageUrl: string
  sortOrder: number
  isPrimary: boolean
}

export interface ProductDto {
  id: number
  sku: string
  name: string
  description?: string | null
  /** Giá bán (đồng bộ từ phiếu nhập khi Confirm) */
  price: number
  /** Alias giá bán — cùng giá trị với price */
  salePrice?: number
  minStock: number
  isActive: boolean
  categoryId: number
  categoryName?: string | null
  unitId: number
  unitName?: string | null
  unitSymbol?: string | null
  images: ProductImageDto[]
  registrationNumber?: string | null
  activeIngredient?: string | null
  strength?: string | null
  dosageForm?: string | null
  manufacturer?: string | null
  isControlledSubstance?: boolean
  requiresColdStorage?: boolean
  expiryAlertDays?: number
}

export interface StockLotDto {
  id: number
  warehouseId: number
  warehouseCode?: string | null
  warehouseName?: string | null
  productId: number
  productSku?: string | null
  productName?: string | null
  unitSymbol?: string | null
  lotNumber: string
  expiryDate?: string | null
  manufactureDate?: string | null
  quantity: number
  daysToExpiry?: number | null
  isExpired: boolean
  isNearExpiry: boolean
  expiryAlertDays: number
  isControlledSubstance: boolean
  requiresColdStorage: boolean
}

export interface WarehouseStockDto {
  id: number
  warehouseId: number
  warehouseCode?: string | null
  warehouseName?: string | null
  productId: number
  productSku?: string | null
  productName?: string | null
  categoryName?: string | null
  unitSymbol?: string | null
  /** Min theo Product */
  productMinStock: number
  /** Min riêng theo kho (null = dùng productMinStock) */
  minQuantity?: number | null
  /** Ngưỡng hiệu lực */
  effectiveMinStock: number
  /** Alias tương thích cũ */
  minStock: number
  quantity: number
  isLowStock: boolean
  isOutOfStock: boolean
  createdAt?: string
  updatedAt?: string | null
}

export interface WarehouseStockSummaryDto {
  warehouseId?: number | null
  totalSkus: number
  lowStockCount: number
  outOfStockCount: number
  totalQuantity: number
}

export interface ReceiptDetailLine {
  id?: number
  productId: number
  productSku?: string | null
  productName?: string | null
  unitSymbol?: string | null
  quantity: number
  /** Giá nhập (phiếu nhập) hoặc đơn giá xuất */
  unitPrice: number
  /** Giá bán — khi Confirm nhập sẽ cập nhật Product.price (trang người dùng) */
  salePrice?: number | null
  lineTotal: number
  lotNumber?: string | null
  expiryDate?: string | null
  manufactureDate?: string | null
  lotAllocationsJson?: string | null
  note?: string | null
}

export interface ImportReceiptDto {
  id: number
  code: string
  receiptDate: string
  warehouseId: number
  warehouseCode?: string | null
  warehouseName?: string | null
  supplierId: number
  supplierName?: string | null
  supplierTaxCode?: string | null
  supplierAddress?: string | null
  supplierPhone?: string | null
  supplierEmail?: string | null
  status: string
  note?: string | null
  /** Tổng giá mua hàng ( SL × đơn giá) */
  goodsAmount: number
  /** Chi phí vận chuyển */
  shippingFee: number
  /** Chiết khấu thương mại */
  tradeDiscount: number
  /** Thuế không được hoàn lại */
  nonRefundableTax: number
  /** Tổng thanh toán = giá mua − CK + VC + thuế KĐHT */
  totalAmount: number
  totalQuantity: number
  /** Mã hóa đơn mua hàng (sau xác nhận) */
  purchaseInvoiceCode?: string | null
  confirmedAt?: string | null
  cancelledAt?: string | null
  details: ReceiptDetailLine[]
}

export interface ExportReceiptDto {
  id: number
  code: string
  receiptDate: string
  warehouseId: number
  warehouseCode?: string | null
  warehouseName?: string | null
  customerId: number
  customerName?: string | null
  status: string
  source?: string | null
  /** DepartmentIssue | PatientDispense | InternalTransfer | Destruction | ReturnToSupplier */
  issuePurpose?: string | null
  invoiceCode?: string | null
  note?: string | null
  totalAmount: number
  totalQuantity: number
  confirmedAt?: string | null
  cancelledAt?: string | null
  createdAt?: string
  details: ReceiptDetailLine[]
}

export interface InvoiceDetailDto {
  id: number
  productId: number
  productSku: string
  productName: string
  unitSymbol?: string | null
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface InvoiceDto {
  id: number
  code: string
  invoiceDate: string
  customerId: number
  customerName: string
  customerPhone?: string | null
  customerEmail?: string | null
  customerAddress?: string | null
  customerTaxCode?: string | null
  exportReceiptId: number
  exportReceiptCode?: string | null
  warehouseId: number
  warehouseCode?: string | null
  warehouseName?: string | null
  subTotal: number
  shippingFee: number
  totalAmount: number
  totalQuantity: number
  status: string
  paymentMethod: string
  note?: string | null
  details: InvoiceDetailDto[]
  createdAt: string
}

export interface StoreCheckoutResult {
  invoice: InvoiceDto
  exportReceipt: ExportReceiptDto
}

export interface StocktakeDetailDto {
  id: number
  productId: number
  productSku?: string | null
  productName?: string | null
  unitSymbol?: string | null
  systemQuantity: number
  countedQuantity: number
  difference: number
  note?: string | null
}

export interface StocktakeDto {
  id: number
  code: string
  stocktakeDate: string
  warehouseId: number
  warehouseCode?: string | null
  warehouseName?: string | null
  status: string
  note?: string | null
  totalLines: number
  diffLines: number
  confirmedAt?: string | null
  cancelledAt?: string | null
  details: StocktakeDetailDto[]
}

export interface StockTransactionDto {
  id: number
  warehouseId: number
  warehouseCode?: string | null
  warehouseName?: string | null
  productId: number
  productSku?: string | null
  productName?: string | null
  unitSymbol?: string | null
  type: string
  quantityChange: number
  quantityBefore: number
  quantityAfter: number
  referenceType?: string | null
  referenceId?: number | null
  referenceCode?: string | null
  note?: string | null
  createdAt: string
}
