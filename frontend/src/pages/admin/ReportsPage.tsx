import { useEffect, useMemo, useState } from 'react'
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material'
import AssessmentOutlinedIcon from '@mui/icons-material/AssessmentOutlined'
import PrintIcon from '@mui/icons-material/Print'
import RefreshIcon from '@mui/icons-material/Refresh'
import {
  exportsApi,
  importsApi,
  productsApi,
  stocktakesApi,
  stocksApi,
  transactionsApi,
  warehousesApi,
} from '../../api/endpoints'
import { ApiError } from '../../api/client'
import { PageHeader, money } from '../../components/ui'
import { useToast } from '../../components/ToastContext'
import type {
  ExportReceiptDto,
  ImportReceiptDto,
  ProductDto,
  StocktakeDto,
  StockTransactionDto,
  WarehouseDto,
  WarehouseStockDto,
} from '../../types/api'
import {
  printWarehouseReport,
  type ReportColumn,
} from '../../utils/warehouseReportsPrint'

type ReportId =
  | 'import'
  | 'export'
  | 'nxt'
  | 'stock'
  | 'stockValue'
  | 'minStock'
  | 'stocktake'
  | 'adjust'
  | 'slow'
  | 'history'

type ReportDef = {
  id: ReportId
  stt: number
  name: string
  content: string
  needsPeriod: boolean
}

const REPORTS: ReportDef[] = [
  { id: 'import', stt: 1, name: 'Báo cáo nhập kho', content: 'Số lượng, giá trị nhập theo kỳ', needsPeriod: true },
  { id: 'export', stt: 2, name: 'Báo cáo xuất kho', content: 'Số lượng, giá trị xuất theo kỳ', needsPeriod: true },
  { id: 'nxt', stt: 3, name: 'Báo cáo nhập – xuất – tồn', content: 'Tồn đầu kỳ, nhập, xuất, tồn cuối kỳ', needsPeriod: true },
  { id: 'stock', stt: 4, name: 'Báo cáo tồn kho hiện tại', content: 'Tồn theo sản phẩm, kho, đơn vị', needsPeriod: false },
  { id: 'stockValue', stt: 5, name: 'Báo cáo giá trị tồn kho', content: 'Giá trị hàng tồn theo giá gốc', needsPeriod: false },
  { id: 'minStock', stt: 6, name: 'Báo cáo hàng tồn tối thiểu', content: 'Hàng sắp hết hoặc hết hàng', needsPeriod: false },
  { id: 'stocktake', stt: 7, name: 'Báo cáo kiểm kê kho', content: 'Số liệu thực tế và số liệu sổ sách', needsPeriod: true },
  { id: 'adjust', stt: 8, name: 'Báo cáo điều chỉnh kho', content: 'Chênh lệch tăng, giảm và lý do', needsPeriod: true },
  { id: 'slow', stt: 9, name: 'Báo cáo hàng tồn lâu ngày', content: 'Hàng chậm luân chuyển', needsPeriod: false },
  { id: 'history', stt: 10, name: 'Báo cáo lịch sử giao dịch', content: 'Toàn bộ lịch sử nhập, xuất, điều chỉnh', needsPeriod: true },
]

type PreviewState = {
  columns: ReportColumn[]
  rows: Record<string, string | number>[]
  summaryLines: string[]
  title: string
  subtitle: string
  reportCode: string
}

function toInputDate(d: Date) {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`
}

function startOfDay(isoDate: string) {
  return new Date(`${isoDate}T00:00:00`)
}
function endOfDay(isoDate: string) {
  return new Date(`${isoDate}T23:59:59.999`)
}

function inRange(iso: string, from: Date, to: Date) {
  const t = new Date(iso).getTime()
  return t >= from.getTime() && t <= to.getTime()
}

async function fetchAllPages<T>(
  loader: (page: number, pageSize: number) => Promise<{ items: T[]; totalPages: number }>,
  pageSize = 100,
  maxPages = 20,
) {
  const all: T[] = []
  for (let page = 1; page <= maxPages; page++) {
    const res = await loader(page, pageSize)
    all.push(...res.items)
    if (page >= (res.totalPages || 1)) break
  }
  return all
}

export function ReportsPage() {
  const toast = useToast()
  const now = useMemo(() => new Date(), [])
  const [selected, setSelected] = useState<ReportId>('import')
  const [warehouseId, setWarehouseId] = useState<number | ''>('')
  const [fromDate, setFromDate] = useState(toInputDate(new Date(now.getFullYear(), now.getMonth(), 1)))
  const [toDate, setToDate] = useState(toInputDate(now))
  const [slowDays, setSlowDays] = useState(90)
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [preview, setPreview] = useState<PreviewState | null>(null)

  const current = REPORTS.find((r) => r.id === selected)!

  useEffect(() => {
    void warehousesApi.list({ pageNumber: 1, pageSize: 100 }).then((r) => setWarehouses(r.items)).catch(() => setWarehouses([]))
  }, [])

  const warehouseLabel =
    warehouseId === ''
      ? 'Tất cả kho dược'
      : warehouses.find((w) => w.id === warehouseId)?.name || `Kho #${warehouseId}`

  const periodLabel = current.needsPeriod
    ? `Từ ${fromDate} đến ${toDate}`
    : selected === 'slow'
      ? `Không phát sinh giao dịch trong ${slowDays} ngày gần đây`
      : `Tại thời điểm ${new Date().toLocaleString('vi-VN')}`

  async function generate() {
    setLoading(true)
    setError('')
    setPreview(null)
    try {
      const from = startOfDay(fromDate)
      const to = endOfDay(toDate)
      const wh = warehouseId === '' ? undefined : Number(warehouseId)
      let result: PreviewState

      if (selected === 'import') {
        const list = await fetchAllPages<ImportReceiptDto>((page, pageSize) =>
          importsApi.list({ pageNumber: page, pageSize, status: 'Confirmed', warehouseId: wh }),
        )
        const filtered = list.filter((x) => inRange(x.receiptDate, from, to))
        const details = await Promise.all(filtered.map((x) => importsApi.get(x.id)))
        const rows: PreviewState['rows'] = []
        let qty = 0
        let amount = 0
        details.forEach((r) => {
          r.details.forEach((d) => {
            qty += Number(d.quantity)
            amount += Number(d.lineTotal)
            rows.push({
              code: r.code,
              date: new Date(r.receiptDate).toLocaleDateString('vi-VN'),
              warehouse: r.warehouseName || r.warehouseCode || '',
              supplier: r.supplierName || '',
              sku: d.productSku || '',
              name: d.productName || '',
              unit: d.unitSymbol || '',
              quantity: Number(d.quantity),
              unitPrice: Number(d.unitPrice),
              lineTotal: Number(d.lineTotal),
            })
          })
        })
        result = {
          reportCode: 'BC-NK-01',
          title: 'BÁO CÁO NHẬP KHO',
          subtitle: 'Số lượng và giá trị nhập kho dược theo kỳ',
          columns: [
            { key: 'stt', label: 'STT', align: 'center' },
            { key: 'code', label: 'Số phiếu' },
            { key: 'date', label: 'Ngày' },
            { key: 'warehouse', label: 'Kho' },
            { key: 'supplier', label: 'NCC' },
            { key: 'sku', label: 'SKU' },
            { key: 'name', label: 'Tên thuốc / VT' },
            { key: 'unit', label: 'ĐVT', align: 'center' },
            { key: 'quantity', label: 'SL', align: 'right' },
            { key: 'unitPrice', label: 'Đơn giá', align: 'right' },
            { key: 'lineTotal', label: 'Thành tiền', align: 'right' },
          ],
          rows,
          summaryLines: [`Tổng số dòng: ${rows.length}`, `Tổng SL nhập: ${qty}`, `Tổng giá trị nhập: ${money(amount)}`],
        }
      } else if (selected === 'export') {
        const list = await fetchAllPages<ExportReceiptDto>((page, pageSize) =>
          exportsApi.list({ pageNumber: page, pageSize, status: 'Confirmed', warehouseId: wh }),
        )
        const filtered = list.filter((x) => inRange(x.receiptDate, from, to))
        const details = await Promise.all(filtered.map((x) => exportsApi.get(x.id)))
        const rows: PreviewState['rows'] = []
        let qty = 0
        let amount = 0
        details.forEach((r) => {
          r.details.forEach((d) => {
            qty += Number(d.quantity)
            amount += Number(d.lineTotal)
            rows.push({
              code: r.code,
              date: new Date(r.receiptDate).toLocaleDateString('vi-VN'),
              warehouse: r.warehouseName || r.warehouseCode || '',
              customer: r.customerName || '',
              sku: d.productSku || '',
              name: d.productName || '',
              unit: d.unitSymbol || '',
              quantity: Number(d.quantity),
              unitPrice: Number(d.unitPrice),
              lineTotal: Number(d.lineTotal),
            })
          })
        })
        result = {
          reportCode: 'BC-XK-02',
          title: 'BÁO CÁO XUẤT KHO',
          subtitle: 'Số lượng và giá trị xuất kho dược theo kỳ',
          columns: [
            { key: 'stt', label: 'STT', align: 'center' },
            { key: 'code', label: 'Số phiếu' },
            { key: 'date', label: 'Ngày' },
            { key: 'warehouse', label: 'Kho' },
            { key: 'customer', label: 'Khoa / phòng' },
            { key: 'sku', label: 'SKU' },
            { key: 'name', label: 'Tên thuốc / VT' },
            { key: 'unit', label: 'ĐVT', align: 'center' },
            { key: 'quantity', label: 'SL', align: 'right' },
            { key: 'unitPrice', label: 'Đơn giá', align: 'right' },
            { key: 'lineTotal', label: 'Thành tiền', align: 'right' },
          ],
          rows,
          summaryLines: [`Tổng số dòng: ${rows.length}`, `Tổng SL xuất: ${qty}`, `Tổng giá trị xuất: ${money(amount)}`],
        }
      } else if (selected === 'nxt') {
        const stocks = await fetchAllPages<WarehouseStockDto>((page, pageSize) =>
          stocksApi.list({ pageNumber: page, pageSize, warehouseId: wh }),
        )
        const txs = await fetchAllPages<StockTransactionDto>((page, pageSize) =>
          transactionsApi.list({ pageNumber: page, pageSize, warehouseId: wh }),
        )
        const periodTx = txs.filter((t) => inRange(t.createdAt, from, to))
        const map = new Map<string, { sku: string; name: string; unit: string; warehouse: string; end: number; imp: number; exp: number }>()
        stocks.forEach((s) => {
          const key = `${s.warehouseId}-${s.productId}`
          map.set(key, {
            sku: s.productSku || '',
            name: s.productName || '',
            unit: s.unitSymbol || '',
            warehouse: s.warehouseName || s.warehouseCode || '',
            end: Number(s.quantity),
            imp: 0,
            exp: 0,
          })
        })
        periodTx.forEach((t) => {
          const key = `${t.warehouseId}-${t.productId}`
          const row =
            map.get(key) ||
            ({
              sku: t.productSku || '',
              name: t.productName || '',
              unit: t.unitSymbol || '',
              warehouse: t.warehouseName || t.warehouseCode || '',
              end: Number(t.quantityAfter),
              imp: 0,
              exp: 0,
            } as const)
          const mutable = map.get(key) || { ...row, imp: 0, exp: 0 }
          if (t.quantityChange > 0) mutable.imp += t.quantityChange
          if (t.quantityChange < 0) mutable.exp += Math.abs(t.quantityChange)
          map.set(key, mutable)
        })
        const rows = Array.from(map.values()).map((r) => {
          const opening = r.end - r.imp + r.exp
          return {
            warehouse: r.warehouse,
            sku: r.sku,
            name: r.name,
            unit: r.unit,
            opening,
            importQty: r.imp,
            exportQty: r.exp,
            closing: r.end,
          }
        })
        result = {
          reportCode: 'BC-NXT-03',
          title: 'BÁO CÁO NHẬP – XUẤT – TỒN',
          subtitle: 'Tồn đầu kỳ, phát sinh nhập/xuất và tồn cuối kỳ',
          columns: [
            { key: 'stt', label: 'STT', align: 'center' },
            { key: 'warehouse', label: 'Kho' },
            { key: 'sku', label: 'SKU' },
            { key: 'name', label: 'Tên thuốc / VT' },
            { key: 'unit', label: 'ĐVT', align: 'center' },
            { key: 'opening', label: 'Tồn đầu', align: 'right' },
            { key: 'importQty', label: 'Nhập', align: 'right' },
            { key: 'exportQty', label: 'Xuất', align: 'right' },
            { key: 'closing', label: 'Tồn cuối', align: 'right' },
          ],
          rows,
          summaryLines: [`Số mặt hàng: ${rows.length}`],
        }
      } else if (selected === 'stock' || selected === 'minStock') {
        const stocks = await fetchAllPages<WarehouseStockDto>((page, pageSize) =>
          stocksApi.list({
            pageNumber: page,
            pageSize,
            warehouseId: wh,
            lowStockOnly: selected === 'minStock' ? true : undefined,
          }),
        )
        const rows = stocks.map((s) => ({
          warehouse: s.warehouseName || s.warehouseCode || '',
          sku: s.productSku || '',
          name: s.productName || '',
          unit: s.unitSymbol || '',
          quantity: Number(s.quantity),
          min: Number(s.effectiveMinStock ?? s.minStock),
          status: s.isOutOfStock ? 'Hết hàng' : s.isLowStock ? 'Sắp hết' : 'Đủ',
        }))
        result = {
          reportCode: selected === 'minStock' ? 'BC-TT-06' : 'BC-TK-04',
          title: selected === 'minStock' ? 'BÁO CÁO HÀNG TỒN TỐI THIỂU' : 'BÁO CÁO TỒN KHO HIỆN TẠI',
          subtitle:
            selected === 'minStock'
              ? 'Danh sách thuốc/vật tư sắp hết hoặc hết hàng'
              : 'Tồn theo sản phẩm, kho và đơn vị tính',
          columns: [
            { key: 'stt', label: 'STT', align: 'center' },
            { key: 'warehouse', label: 'Kho' },
            { key: 'sku', label: 'SKU' },
            { key: 'name', label: 'Tên thuốc / VT' },
            { key: 'unit', label: 'ĐVT', align: 'center' },
            { key: 'quantity', label: 'Tồn', align: 'right' },
            { key: 'min', label: 'Min', align: 'right' },
            { key: 'status', label: 'Trạng thái', align: 'center' },
          ],
          rows,
          summaryLines: [`Số dòng: ${rows.length}`, `Tổng SL tồn: ${rows.reduce((a, r) => a + Number(r.quantity), 0)}`],
        }
      } else if (selected === 'stockValue') {
        const [stocks, products] = await Promise.all([
          fetchAllPages<WarehouseStockDto>((page, pageSize) =>
            stocksApi.list({ pageNumber: page, pageSize, warehouseId: wh }),
          ),
          fetchAllPages<ProductDto>((page, pageSize) => productsApi.list({ pageNumber: page, pageSize })),
        ])
        const priceMap = new Map(products.map((p) => [p.id, Number(p.price) || 0]))
        let totalValue = 0
        const rows = stocks.map((s) => {
          const price = priceMap.get(s.productId) || 0
          const value = Number(s.quantity) * price
          totalValue += value
          return {
            warehouse: s.warehouseName || s.warehouseCode || '',
            sku: s.productSku || '',
            name: s.productName || '',
            unit: s.unitSymbol || '',
            quantity: Number(s.quantity),
            unitPrice: price,
            value,
          }
        })
        result = {
          reportCode: 'BC-GT-05',
          title: 'BÁO CÁO GIÁ TRỊ TỒN KHO',
          subtitle: 'Giá trị hàng tồn theo giá gốc (đơn giá danh mục thuốc/VT)',
          columns: [
            { key: 'stt', label: 'STT', align: 'center' },
            { key: 'warehouse', label: 'Kho' },
            { key: 'sku', label: 'SKU' },
            { key: 'name', label: 'Tên thuốc / VT' },
            { key: 'unit', label: 'ĐVT', align: 'center' },
            { key: 'quantity', label: 'Tồn', align: 'right' },
            { key: 'unitPrice', label: 'Giá gốc', align: 'right' },
            { key: 'value', label: 'Giá trị', align: 'right' },
          ],
          rows,
          summaryLines: [`Tổng giá trị tồn kho: ${money(totalValue)}`],
        }
      } else if (selected === 'stocktake') {
        const list = await fetchAllPages<StocktakeDto>((page, pageSize) =>
          stocktakesApi.list({ pageNumber: page, pageSize, status: 'Confirmed', warehouseId: wh }),
        )
        const filtered = list.filter((x) => inRange(x.stocktakeDate, from, to))
        const details = await Promise.all(filtered.map((x) => stocktakesApi.get(x.id)))
        const rows: PreviewState['rows'] = []
        details.forEach((r) => {
          r.details.forEach((d) => {
            rows.push({
              code: r.code,
              date: new Date(r.stocktakeDate).toLocaleDateString('vi-VN'),
              warehouse: r.warehouseName || r.warehouseCode || '',
              sku: d.productSku || '',
              name: d.productName || '',
              unit: d.unitSymbol || '',
              systemQty: Number(d.systemQuantity),
              countedQty: Number(d.countedQuantity),
              diff: Number(d.difference),
              note: d.note || '',
            })
          })
        })
        result = {
          reportCode: 'BC-KK-07',
          title: 'BÁO CÁO KIỂM KÊ KHO',
          subtitle: 'Đối chiếu số liệu sổ sách và số liệu kiểm kê thực tế',
          columns: [
            { key: 'stt', label: 'STT', align: 'center' },
            { key: 'code', label: 'Số KK' },
            { key: 'date', label: 'Ngày' },
            { key: 'warehouse', label: 'Kho' },
            { key: 'sku', label: 'SKU' },
            { key: 'name', label: 'Tên thuốc / VT' },
            { key: 'systemQty', label: 'Sổ sách', align: 'right' },
            { key: 'countedQty', label: 'Thực tế', align: 'right' },
            { key: 'diff', label: 'Chênh lệch', align: 'right' },
            { key: 'note', label: 'Ghi chú' },
          ],
          rows,
          summaryLines: [`Số dòng kiểm kê: ${rows.length}`],
        }
      } else if (selected === 'adjust') {
        const txs = await fetchAllPages<StockTransactionDto>((page, pageSize) =>
          transactionsApi.list({ pageNumber: page, pageSize, warehouseId: wh }),
        )
        const rows = txs
          .filter((t) => inRange(t.createdAt, from, to))
          .filter((t) => t.type === 'Manual' || t.type === 'Stocktake' || t.type === 'StocktakeCancel')
          .map((t) => ({
            date: new Date(t.createdAt).toLocaleString('vi-VN'),
            warehouse: t.warehouseName || t.warehouseCode || '',
            sku: t.productSku || '',
            name: t.productName || '',
            type: t.type,
            change: Number(t.quantityChange),
            before: Number(t.quantityBefore),
            after: Number(t.quantityAfter),
            reason: t.note || t.referenceCode || '',
          }))
        result = {
          reportCode: 'BC-DC-08',
          title: 'BÁO CÁO ĐIỀU CHỈNH KHO',
          subtitle: 'Chênh lệch tăng/giảm tồn và lý do điều chỉnh',
          columns: [
            { key: 'stt', label: 'STT', align: 'center' },
            { key: 'date', label: 'Thời gian' },
            { key: 'warehouse', label: 'Kho' },
            { key: 'sku', label: 'SKU' },
            { key: 'name', label: 'Tên thuốc / VT' },
            { key: 'type', label: 'Loại' },
            { key: 'change', label: 'Biến động', align: 'right' },
            { key: 'before', label: 'Trước', align: 'right' },
            { key: 'after', label: 'Sau', align: 'right' },
            { key: 'reason', label: 'Lý do' },
          ],
          rows,
          summaryLines: [`Số phát sinh điều chỉnh: ${rows.length}`],
        }
      } else if (selected === 'slow') {
        const [stocks, txs] = await Promise.all([
          fetchAllPages<WarehouseStockDto>((page, pageSize) =>
            stocksApi.list({ pageNumber: page, pageSize, warehouseId: wh }),
          ),
          fetchAllPages<StockTransactionDto>((page, pageSize) =>
            transactionsApi.list({ pageNumber: page, pageSize, warehouseId: wh }),
          ),
        ])
        const cutoff = Date.now() - slowDays * 24 * 60 * 60 * 1000
        const lastMap = new Map<string, number>()
        txs.forEach((t) => {
          const key = `${t.warehouseId}-${t.productId}`
          const ts = new Date(t.createdAt).getTime()
          lastMap.set(key, Math.max(lastMap.get(key) || 0, ts))
        })
        const rows = stocks
          .filter((s) => Number(s.quantity) > 0)
          .filter((s) => {
            const last = lastMap.get(`${s.warehouseId}-${s.productId}`) || 0
            return last < cutoff
          })
          .map((s) => {
            const last = lastMap.get(`${s.warehouseId}-${s.productId}`)
            return {
              warehouse: s.warehouseName || s.warehouseCode || '',
              sku: s.productSku || '',
              name: s.productName || '',
              unit: s.unitSymbol || '',
              quantity: Number(s.quantity),
              lastMove: last ? new Date(last).toLocaleDateString('vi-VN') : 'Không có',
              idleDays: last ? Math.floor((Date.now() - last) / (24 * 60 * 60 * 1000)) : '∞',
            }
          })
        result = {
          reportCode: 'BC-CL-09',
          title: 'BÁO CÁO HÀNG TỒN LÂU NGÀY',
          subtitle: `Hàng còn tồn nhưng chậm luân chuyển (≥ ${slowDays} ngày không phát sinh)`,
          columns: [
            { key: 'stt', label: 'STT', align: 'center' },
            { key: 'warehouse', label: 'Kho' },
            { key: 'sku', label: 'SKU' },
            { key: 'name', label: 'Tên thuốc / VT' },
            { key: 'unit', label: 'ĐVT', align: 'center' },
            { key: 'quantity', label: 'Tồn', align: 'right' },
            { key: 'lastMove', label: 'GD gần nhất' },
            { key: 'idleDays', label: 'Số ngày đứng', align: 'right' },
          ],
          rows,
          summaryLines: [`Số mặt hàng chậm luân chuyển: ${rows.length}`],
        }
      } else {
        const txs = await fetchAllPages<StockTransactionDto>((page, pageSize) =>
          transactionsApi.list({ pageNumber: page, pageSize, warehouseId: wh }),
        )
        const rows = txs
          .filter((t) => inRange(t.createdAt, from, to))
          .map((t) => ({
            date: new Date(t.createdAt).toLocaleString('vi-VN'),
            warehouse: t.warehouseName || t.warehouseCode || '',
            sku: t.productSku || '',
            name: t.productName || '',
            type: t.type,
            change: Number(t.quantityChange),
            before: Number(t.quantityBefore),
            after: Number(t.quantityAfter),
            ref: t.referenceCode || '',
            note: t.note || '',
          }))
        result = {
          reportCode: 'BC-LSSGĐ-10',
          title: 'BÁO CÁO LỊCH SỬ GIAO DỊCH KHO',
          subtitle: 'Toàn bộ phát sinh nhập, xuất, kiểm kê và điều chỉnh trong kỳ',
          columns: [
            { key: 'stt', label: 'STT', align: 'center' },
            { key: 'date', label: 'Thời gian' },
            { key: 'warehouse', label: 'Kho' },
            { key: 'sku', label: 'SKU' },
            { key: 'name', label: 'Tên thuốc / VT' },
            { key: 'type', label: 'Loại GD' },
            { key: 'change', label: 'Biến động', align: 'right' },
            { key: 'before', label: 'Trước', align: 'right' },
            { key: 'after', label: 'Sau', align: 'right' },
            { key: 'ref', label: 'Chứng từ' },
            { key: 'note', label: 'Ghi chú' },
          ],
          rows,
          summaryLines: [`Tổng giao dịch: ${rows.length}`],
        }
      }

      setPreview(result)
      toast.success(`Đã lập ${result.title.toLowerCase()} (${result.rows.length} dòng)`)
    } catch (e) {
      const msg = e instanceof ApiError ? e.message : 'Không lập được báo cáo'
      setError(msg)
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }

  function handlePrint() {
    if (!preview) {
      toast.error('Hãy lập báo cáo trước khi in')
      return
    }
    printWarehouseReport({
      reportCode: preview.reportCode,
      title: preview.title,
      subtitle: preview.subtitle,
      periodLabel,
      warehouseLabel,
      columns: preview.columns,
      rows: preview.rows,
      summaryLines: preview.summaryLines,
      footerNote: 'Báo cáo được lập từ hệ thống quản lý kho nhà thuốc bệnh viện theo số liệu sổ sách.',
    })
  }

  return (
    <Box>
      <PageHeader
        icon={<AssessmentOutlinedIcon color="primary" />}
        title="Báo cáo kho dược"
        subtitle="10 mẫu báo cáo nhập – xuất – tồn theo quy định, xem trước và in phiếu"
        actions={
          <Stack direction="row" spacing={1}>
            <Button variant="outlined" startIcon={<RefreshIcon />} disabled={loading} onClick={() => void generate()}>
              Lập báo cáo
            </Button>
            <Button variant="contained" startIcon={<PrintIcon />} disabled={!preview || loading} onClick={handlePrint}>
              In báo cáo
            </Button>
          </Stack>
        }
      />

      <Box className="grid grid-cols-1 gap-3 lg:grid-cols-12">
        <Paper className="lg:col-span-4 p-3" sx={{ borderRadius: 3 }}>
          <Typography sx={{ fontWeight: 800, mb: 1.5 }}>Danh mục báo cáo</Typography>
          <Stack spacing={1}>
            {REPORTS.map((r) => (
              <Paper
                key={r.id}
                variant="outlined"
                onClick={() => {
                  setSelected(r.id)
                  setPreview(null)
                }}
                sx={{
                  p: 1.25,
                  borderRadius: 2,
                  cursor: 'pointer',
                  borderColor: selected === r.id ? 'primary.main' : 'divider',
                  bgcolor: selected === r.id ? 'rgba(21,101,192,0.06)' : '#fff',
                  transition: 'all .15s ease',
                  '&:hover': { borderColor: 'primary.light' },
                }}
              >
                <Stack direction="row" spacing={1} alignItems="flex-start">
                  <Chip size="small" label={r.stt} color={selected === r.id ? 'primary' : 'default'} sx={{ fontWeight: 800 }} />
                  <Box>
                    <Typography sx={{ fontWeight: 700, fontSize: 14 }}>{r.name}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {r.content}
                    </Typography>
                  </Box>
                </Stack>
              </Paper>
            ))}
          </Stack>
        </Paper>

        <Stack spacing={2} className="lg:col-span-8">
          <Paper className="p-3" sx={{ borderRadius: 3 }}>
            <Typography sx={{ fontWeight: 800, mb: 1.5 }}>
              {current.stt}. {current.name}
            </Typography>
            <Typography variant="body2" color="text.secondary" className="mb-2">
              {current.content}
            </Typography>
            <Box className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-4">
              <TextField
                select
                size="small"
                label="Kho"
                value={warehouseId}
                onChange={(e) => setWarehouseId(e.target.value === '' ? '' : Number(e.target.value))}
              >
                <MenuItem value="">Tất cả kho</MenuItem>
                {warehouses.map((w) => (
                  <MenuItem key={w.id} value={w.id}>
                    {w.code} — {w.name}
                  </MenuItem>
                ))}
              </TextField>
              {current.needsPeriod ? (
                <>
                  <TextField
                    size="small"
                    type="date"
                    label="Từ ngày"
                    value={fromDate}
                    onChange={(e) => setFromDate(e.target.value)}
                    InputLabelProps={{ shrink: true }}
                  />
                  <TextField
                    size="small"
                    type="date"
                    label="Đến ngày"
                    value={toDate}
                    onChange={(e) => setToDate(e.target.value)}
                    InputLabelProps={{ shrink: true }}
                  />
                </>
              ) : null}
              {selected === 'slow' ? (
                <TextField
                  size="small"
                  type="number"
                  label="Số ngày không GD"
                  value={slowDays}
                  onChange={(e) => setSlowDays(Math.max(1, Number(e.target.value) || 90))}
                  slotProps={{ htmlInput: { min: 1 } }}
                />
              ) : null}
            </Box>
            <Stack direction="row" spacing={1} className="mt-2">
              <Button variant="contained" disabled={loading} onClick={() => void generate()}>
                {loading ? 'Đang lập...' : 'Lập báo cáo'}
              </Button>
              <Button variant="outlined" startIcon={<PrintIcon />} disabled={!preview || loading} onClick={handlePrint}>
                In theo mẫu
              </Button>
            </Stack>
          </Paper>

          {error ? <Alert severity="error">{error}</Alert> : null}

          <Paper className="p-3" sx={{ borderRadius: 3, minHeight: 320 }}>
            <Stack direction="row" className="mb-2 items-center justify-between">
              <Typography sx={{ fontWeight: 800 }}>Xem trước</Typography>
              {preview ? (
                <Chip size="small" color="success" label={`${preview.rows.length} dòng`} sx={{ fontWeight: 700 }} />
              ) : null}
            </Stack>
            {loading ? (
              <Box className="flex min-h-[220px] flex-col items-center justify-center gap-2">
                <CircularProgress size={28} />
                <Typography color="text.secondary">Đang tổng hợp số liệu...</Typography>
              </Box>
            ) : preview ? (
              <>
                <Typography variant="body2" className="mb-1" sx={{ fontWeight: 700 }}>
                  {preview.title}
                </Typography>
                <Typography variant="caption" color="text.secondary" display="block" className="mb-2">
                  {periodLabel} · {warehouseLabel}
                </Typography>
                <TableContainer sx={{ maxHeight: 420 }}>
                  <Table size="small" stickyHeader>
                    <TableHead>
                      <TableRow>
                        {preview.columns.map((c) => (
                          <TableCell key={c.key} align={c.align || 'left'} sx={{ fontWeight: 700, whiteSpace: 'nowrap' }}>
                            {c.label}
                          </TableCell>
                        ))}
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {preview.rows.slice(0, 200).map((r, idx) => (
                        <TableRow key={idx} hover>
                          {preview.columns.map((c) => {
                            const raw = c.key === 'stt' ? idx + 1 : r[c.key]
                            const moneyKeys = new Set(['unitPrice', 'lineTotal', 'value'])
                            const display =
                              typeof raw === 'number'
                                ? moneyKeys.has(c.key)
                                  ? money(raw)
                                  : Number(raw).toLocaleString('vi-VN')
                                : String(raw ?? '')
                            return (
                              <TableCell key={c.key} align={c.align || 'left'}>
                                {display}
                              </TableCell>
                            )
                          })}
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
                {preview.rows.length > 200 ? (
                  <Typography variant="caption" color="text.secondary" className="mt-1" display="block">
                    Đang xem 200/{preview.rows.length} dòng — bản in đầy đủ toàn bộ dữ liệu.
                  </Typography>
                ) : null}
                {preview.summaryLines.map((l) => (
                  <Typography key={l} variant="body2" sx={{ fontWeight: 700 }} className="mt-1">
                    {l}
                  </Typography>
                ))}
              </>
            ) : (
              <Box className="flex min-h-[220px] items-center justify-center">
                <Typography color="text.secondary">Chọn báo cáo và bấm Lập báo cáo để xem dữ liệu</Typography>
              </Box>
            )}
          </Paper>
        </Stack>
      </Box>
    </Box>
  )
}
