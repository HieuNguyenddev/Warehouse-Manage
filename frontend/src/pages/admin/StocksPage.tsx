import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  alpha,
  Box,
  Button,
  Checkbox,
  Chip,
  FormControlLabel,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Step,
  StepConnector,
  stepConnectorClasses,
  StepLabel,
  Stepper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material'
import { styled } from '@mui/material/styles'
import LayersIcon from '@mui/icons-material/Layers'
import AddIcon from '@mui/icons-material/Add'
import RefreshIcon from '@mui/icons-material/Refresh'
import TuneIcon from '@mui/icons-material/Tune'
import HistoryIcon from '@mui/icons-material/History'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import FactCheckIcon from '@mui/icons-material/FactCheck'
import CloseIcon from '@mui/icons-material/Close'
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import InputIcon from '@mui/icons-material/Input'
import OutputIcon from '@mui/icons-material/Output'
import WarningAmberRoundedIcon from '@mui/icons-material/WarningAmberRounded'
import EditNoteOutlinedIcon from '@mui/icons-material/EditNoteOutlined'
import FactCheckOutlinedIcon from '@mui/icons-material/FactCheckOutlined'
import ReceiptLongOutlinedIcon from '@mui/icons-material/ReceiptLongOutlined'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import { productsApi, stocksApi, transactionsApi, warehousesApi } from '../../api/endpoints'
import { ApiError } from '../../api/client'
import { useToast } from '../../components/ToastContext'
import { Alert, Badge, Modal, PageHeader, Pager } from '../../components/ui'
import type {
  ProductDto,
  StockTransactionDto,
  WarehouseDto,
  WarehouseStockDto,
  WarehouseStockSummaryDto,
} from '../../types/api'

type FilterMode = 'all' | 'low' | 'out'
type ProcessStepId = 'opening' | 'import' | 'export' | 'monitor' | 'adjust' | 'stocktake' | 'audit'

const PROCESS_STEPS: {
  id: ProcessStepId
  label: string
  short: string
  icon: ReactNode
  color: string
}[] = [
  {
    id: 'opening',
    label: '1. Mở sổ / Đối chiếu',
    short: 'Xem nhập, xuất, tồn kho và chọn bước tiếp theo',
    icon: <Inventory2OutlinedIcon fontSize="small" />,
    color: '#1565c0',
  },
  {
    id: 'import',
    label: '2. Nhập kho',
    short: 'Nhận hàng từ NCC: lưu nháp, xác nhận để cộng tồn',
    icon: <InputIcon fontSize="small" />,
    color: '#0B6E4F',
  },
  {
    id: 'export',
    label: '3. Xuất kho',
    short: 'Cấp phát / bán: lưu nháp, xác nhận để trừ tồn',
    icon: <OutputIcon fontSize="small" />,
    color: '#c62828',
  },
  {
    id: 'monitor',
    label: '4. Theo dõi & cảnh báo',
    short: 'Theo dõi tồn thấp, hết hàng và chỉnh ngưỡng min',
    icon: <WarningAmberRoundedIcon fontSize="small" />,
    color: '#ed6c02',
  },
  {
    id: 'adjust',
    label: '5. Điều chỉnh tồn',
    short: 'Hiệu chỉnh có lý do (hư hỏng, thất thoát…)',
    icon: <EditNoteOutlinedIcon fontSize="small" />,
    color: '#6a1b9a',
  },
  {
    id: 'stocktake',
    label: '6. Kiểm kê',
    short: 'So sổ sách với thực tế, xác nhận lệch',
    icon: <FactCheckOutlinedIcon fontSize="small" />,
    color: '#00897b',
  },
  {
    id: 'audit',
    label: '7. Đối soát lịch sử',
    short: 'Xem lịch sử nhập, xuất, kiểm kê, điều chỉnh',
    icon: <ReceiptLongOutlinedIcon fontSize="small" />,
    color: '#455a64',
  },
]

const ProcessConnector = styled(StepConnector)(({ theme }) => ({
  [`&.${stepConnectorClasses.alternativeLabel}`]: { top: 16 },
  [`& .${stepConnectorClasses.line}`]: {
    height: 2,
    border: 0,
    backgroundColor: alpha(theme.palette.text.primary, 0.12),
    borderRadius: 1,
  },
}))

type ReconRow = {
  key: string
  warehouseId: number
  warehouseCode: string
  warehouseName: string
  productId: number
  productSku: string
  productName: string
  unitSymbol?: string | null
  stockId?: number
  onHand: number
  imported: number
  exported: number
  adjusted: number
  book: number
  diff: number
  /** Khi xuất > nhập: số lượng cần nhập thêm tối thiểu */
  needImportQty: number
  isLow: boolean
  isOut: boolean
  suggest: ProcessStepId
  suggestLabel: string
}

type ReconTotals = {
  imported: number
  exported: number
  onHand: number
  adjusted: number
  diffSkus: number
  lowSkus: number
  freeSkuCount: number
  importShortSkus: number
}

async function fetchAllPages<T>(
  loader: (page: number, pageSize: number) => Promise<{ items: T[]; totalPages: number }>,
  pageSize = 100,
  maxPages = 30,
) {
  const all: T[] = []
  for (let page = 1; page <= maxPages; page++) {
    const res = await loader(page, pageSize)
    all.push(...res.items)
    if (page >= (res.totalPages || 1)) break
  }
  return all
}

function isImportType(type: string) {
  return type === 'Import' || type === 'ImportCancel'
}

function isExportType(type: string) {
  return type === 'Export' || type === 'ExportCancel'
}

function suggestReconAction(row: Omit<ReconRow, 'suggest' | 'suggestLabel' | 'key'>): {
  suggest: ProcessStepId
  suggestLabel: string
} {
  if (!row.stockId) return { suggest: 'opening', suggestLabel: 'Mở sổ' }
  // Ưu tiên: xuất nhiều hơn nhập, bắt buộc nhập thêm
  if (row.needImportQty > 0) return { suggest: 'import', suggestLabel: 'Nhập thêm hàng' }
  if (Math.abs(row.diff) > 0.0001) return { suggest: 'stocktake', suggestLabel: 'Kiểm kê lệch' }
  if (row.isOut) return { suggest: 'import', suggestLabel: 'Nhập bổ sung' }
  if (row.isLow) return { suggest: 'monitor', suggestLabel: 'Theo dõi min' }
  if (row.imported === 0 && row.onHand >= 0) return { suggest: 'import', suggestLabel: 'Nhập kho' }
  if (row.onHand > 0) return { suggest: 'export', suggestLabel: 'Xuất kho' }
  return { suggest: 'audit', suggestLabel: 'Đối soát' }
}

function buildReconRows(
  stocks: WarehouseStockDto[],
  txs: StockTransactionDto[],
  freeSkuCount: number,
): { rows: ReconRow[]; totals: ReconTotals } {
  type Agg = {
    warehouseId: number
    warehouseCode: string
    warehouseName: string
    productId: number
    productSku: string
    productName: string
    unitSymbol?: string | null
    stockId?: number
    onHand: number
    imported: number
    exported: number
    adjusted: number
    isLow: boolean
    isOut: boolean
  }

  const map = new Map<string, Agg>()

  const ensure = (
    warehouseId: number,
    productId: number,
    meta: Partial<Agg>,
  ): Agg => {
    const key = `${warehouseId}:${productId}`
    let row = map.get(key)
    if (!row) {
      row = {
        warehouseId,
        warehouseCode: meta.warehouseCode || '',
        warehouseName: meta.warehouseName || '',
        productId,
        productSku: meta.productSku || `#${productId}`,
        productName: meta.productName || '',
        unitSymbol: meta.unitSymbol,
        stockId: meta.stockId,
        onHand: meta.onHand ?? 0,
        imported: 0,
        exported: 0,
        adjusted: 0,
        isLow: meta.isLow ?? false,
        isOut: meta.isOut ?? false,
      }
      map.set(key, row)
    }
    return row
  }

  for (const s of stocks) {
    const row = ensure(s.warehouseId, s.productId, {
      warehouseCode: s.warehouseCode || '',
      warehouseName: s.warehouseName || '',
      productSku: s.productSku || '',
      productName: s.productName || '',
      unitSymbol: s.unitSymbol,
      stockId: s.id,
      onHand: Number(s.quantity) || 0,
      isLow: s.isLowStock,
      isOut: s.isOutOfStock,
    })
    row.stockId = s.id
    row.onHand = Number(s.quantity) || 0
    row.isLow = s.isLowStock
    row.isOut = s.isOutOfStock
    row.warehouseCode = s.warehouseCode || row.warehouseCode
    row.warehouseName = s.warehouseName || row.warehouseName
    row.productSku = s.productSku || row.productSku
    row.productName = s.productName || row.productName
    row.unitSymbol = s.unitSymbol || row.unitSymbol
  }

  for (const t of txs) {
    const row = ensure(t.warehouseId, t.productId, {
      warehouseCode: t.warehouseCode || '',
      warehouseName: t.warehouseName || '',
      productSku: t.productSku || '',
      productName: t.productName || '',
      unitSymbol: t.unitSymbol,
    })
    const change = Number(t.quantityChange) || 0
    if (isImportType(t.type)) {
      // Import dương, ImportCancel âm: cộng dồn net nhập
      row.imported += change
    } else if (isExportType(t.type)) {
      // Export âm, ExportCancel dương: đổi thành số xuất dương
      row.exported += -change
    } else {
      // Manual / Stocktake / StocktakeCancel
      row.adjusted += change
    }
    if (!row.productSku && t.productSku) row.productSku = t.productSku
    if (!row.productName && t.productName) row.productName = t.productName
    if (!row.unitSymbol && t.unitSymbol) row.unitSymbol = t.unitSymbol
  }

  const rows: ReconRow[] = [...map.values()]
    .map((r) => {
      const book = r.imported - r.exported + r.adjusted
      const needImportQty = Math.max(0, r.exported - r.imported)
      const base = {
        ...r,
        book,
        diff: r.onHand - book,
        needImportQty,
      }
      const { suggest, suggestLabel } = suggestReconAction(base)
      return {
        ...base,
        key: `${r.warehouseId}:${r.productId}`,
        suggest,
        suggestLabel,
      }
    })
    .sort((a, b) => {
      const score = (x: ReconRow) =>
        (x.needImportQty > 0 ? 5000 : 0) +
        (Math.abs(x.diff) > 0.0001 ? 1000 : 0) +
        (x.isOut ? 100 : 0) +
        (x.isLow ? 10 : 0)
      return score(b) - score(a) || a.productSku.localeCompare(b.productSku)
    })

  const totals: ReconTotals = {
    imported: rows.reduce((s, r) => s + r.imported, 0),
    exported: rows.reduce((s, r) => s + r.exported, 0),
    onHand: rows.reduce((s, r) => s + r.onHand, 0),
    adjusted: rows.reduce((s, r) => s + r.adjusted, 0),
    diffSkus: rows.filter((r) => Math.abs(r.diff) > 0.0001).length,
    lowSkus: rows.filter((r) => r.isLow || r.isOut).length,
    freeSkuCount,
    importShortSkus: rows.filter((r) => r.needImportQty > 0).length,
  }

  return { rows, totals }
}

export function StocksPage() {
  const toast = useToast()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const initialWarehouse = Number(searchParams.get('warehouseId') || 0) || 0
  const initialLow = searchParams.get('lowStockOnly') === 'true'

  const [rows, setRows] = useState<WarehouseStockDto[]>([])
  const [summary, setSummary] = useState<WarehouseStockSummaryDto | null>(null)
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([])
  const [products, setProducts] = useState<ProductDto[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [search, setSearch] = useState('')
  const [warehouseId, setWarehouseId] = useState(initialWarehouse)
  const [filterMode, setFilterMode] = useState<FilterMode>(initialLow ? 'low' : 'all')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [activeStep, setActiveStep] = useState<ProcessStepId>('monitor')

  const [detail, setDetail] = useState<WarehouseStockDto | null>(null)
  const [history, setHistory] = useState<StockTransactionDto[]>([])
  const [openingOpen, setOpeningOpen] = useState(false)
  const [reconOpen, setReconOpen] = useState(false)
  const [reconLoading, setReconLoading] = useState(false)
  const [reconRows, setReconRows] = useState<ReconRow[]>([])
  const [reconTotals, setReconTotals] = useState<ReconTotals | null>(null)
  const [adjustOpen, setAdjustOpen] = useState(false)
  const [minOpen, setMinOpen] = useState(false)
  const [occupiedProductIds, setOccupiedProductIds] = useState<Set<number>>(new Set())
  const [openingBusy, setOpeningBusy] = useState(false)

  const [opening, setOpening] = useState({
    warehouseId: 0,
    productId: 0,
    quantity: '' as number | '',
    minQuantity: '' as number | '',
    reason: 'Kiểm đếm tồn đầu kỳ / chuyển số dư',
  })
  const [adjust, setAdjust] = useState({ quantity: 0, reason: '' })
  const [minQty, setMinQty] = useState<number | ''>('')
  const [useProductMin, setUseProductMin] = useState(true)

  const warehouseLabel = useMemo(() => {
    if (!warehouseId) return 'Tất cả kho'
    const w = warehouses.find((x) => x.id === warehouseId)
    return w ? `${w.code} · ${w.name}` : `Kho #${warehouseId}`
  }, [warehouseId, warehouses])

  const openingProducts = useMemo(
    () => products.filter((p) => !occupiedProductIds.has(p.id)),
    [products, occupiedProductIds],
  )

  const openingProduct = useMemo(
    () => products.find((p) => p.id === opening.productId) ?? null,
    [products, opening.productId],
  )

  const importShortRows = useMemo(
    () => reconRows.filter((r) => r.needImportQty > 0),
    [reconRows],
  )

  const stepIndex = PROCESS_STEPS.findIndex((s) => s.id === activeStep)

  async function reload() {
    setLoading(true)
    setError('')
    try {
      const [list, sum] = await Promise.all([
        stocksApi.list({
          pageNumber: page,
          pageSize: 15,
          search: search || undefined,
          warehouseId: warehouseId || undefined,
          lowStockOnly: filterMode === 'low' ? true : undefined,
          outOfStockOnly: filterMode === 'out' ? true : undefined,
        }),
        stocksApi.summary(warehouseId || undefined),
      ])
      setRows(list.items)
      setTotalPages(list.totalPages || 1)
      setSummary(sum)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Không tải được tồn kho')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void (async () => {
      try {
        const [w, p] = await Promise.all([
          warehousesApi.list({ pageNumber: 1, pageSize: 200 }),
          productsApi.list({ pageNumber: 1, pageSize: 200 }),
        ])
        setWarehouses(w.items.filter((x) => x.isActive))
        setProducts(p.items.filter((x) => x.isActive))
      } catch (e) {
        toast.error(e instanceof ApiError ? e.message : 'Không tải được danh mục kho/sản phẩm')
      }
    })()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    void reload()
    const next = new URLSearchParams()
    if (warehouseId) next.set('warehouseId', String(warehouseId))
    if (filterMode === 'low') next.set('lowStockOnly', 'true')
    if (filterMode === 'out') next.set('outOfStockOnly', 'true')
    setSearchParams(next, { replace: true })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, search, warehouseId, filterMode])

  async function openDetail(row: WarehouseStockDto) {
    setDetail(row)
    try {
      const tx = await transactionsApi.list({
        pageNumber: 1,
        pageSize: 8,
        warehouseId: row.warehouseId,
        productId: row.productId,
      })
      setHistory(tx.items)
    } catch {
      setHistory([])
    }
  }

  async function loadReconciliation(whId: number) {
    setReconLoading(true)
    try {
      const [stocks, txs] = await Promise.all([
        fetchAllPages<WarehouseStockDto>((pageNumber, pageSize) =>
          stocksApi.list({ pageNumber, pageSize, warehouseId: whId }),
        ),
        fetchAllPages<StockTransactionDto>((pageNumber, pageSize) =>
          transactionsApi.list({ pageNumber, pageSize, warehouseId: whId }),
        ),
      ])
      const occupied = new Set(stocks.map((s) => s.productId))
      setOccupiedProductIds(occupied)
      const freeSkuCount = products.filter((p) => !occupied.has(p.id)).length
      const { rows: built, totals } = buildReconRows(stocks, txs, freeSkuCount)
      setReconRows(built)
      setReconTotals(totals)
    } catch (e) {
      setReconRows([])
      setReconTotals(null)
      toast.error(e instanceof ApiError ? e.message : 'Không tải được bảng đối chiếu')
    } finally {
      setReconLoading(false)
    }
  }

  async function openReconPanel() {
    const whId = warehouseId || warehouses[0]?.id || 0
    if (!whId) {
      toast.error('Chưa có kho hoạt động — tạo kho trước khi đối chiếu tồn')
      return
    }
    if (!warehouseId) setWarehouseId(whId)
    setActiveStep('opening')
    setReconOpen(true)
    await loadReconciliation(whId)
  }

  async function openOpeningModal(prefillProductId?: number) {
    const whId = warehouseId || warehouses[0]?.id || 0
    setOpeningOpen(true)
    setOpeningBusy(true)
    try {
      let occupied = new Set<number>()
      if (whId) {
        const list = await stocksApi.list({ pageNumber: 1, pageSize: 500, warehouseId: whId })
        occupied = new Set(list.items.map((x) => x.productId))
      }
      setOccupiedProductIds(occupied)
      const firstFree =
        (prefillProductId && !occupied.has(prefillProductId)
          ? products.find((p) => p.id === prefillProductId)
          : undefined) || products.find((p) => !occupied.has(p.id))
      setOpening({
        warehouseId: whId,
        productId: firstFree?.id || 0,
        quantity: '',
        minQuantity: '',
        reason: 'Kiểm đếm tồn đầu kỳ / chuyển số dư',
      })
    } finally {
      setOpeningBusy(false)
    }
  }

  function navigateImportProduct(row: ReconRow) {
    const qty = Math.max(row.needImportQty, 1)
    const q = new URLSearchParams({
      warehouseId: String(row.warehouseId),
      productId: String(row.productId),
      suggestedQty: String(qty),
      openCreate: '1',
    })
    setReconOpen(false)
    setActiveStep('import')
    toast.success(
      row.needImportQty > 0
        ? `${row.productSku}: xuất nhiều hơn nhập ${row.needImportQty.toLocaleString('vi-VN')}. Đang mở phiếu nhập.`
        : `Đang mở phiếu nhập cho ${row.productSku}`,
    )
    navigate(`/admin/imports?${q.toString()}`)
  }

  function goReconAction(row: ReconRow) {
    setActiveStep(row.suggest)
    switch (row.suggest) {
      case 'opening':
        void openOpeningModal(row.productId)
        break
      case 'import':
        navigateImportProduct(row)
        break
      case 'export':
        navigate(`/admin/exports?warehouseId=${row.warehouseId}`)
        break
      case 'monitor':
        setReconOpen(false)
        setWarehouseId(row.warehouseId)
        setPage(1)
        setFilterMode('low')
        toast.success(`Đã lọc tồn thấp tại ${row.warehouseCode} — chỉnh ngưỡng min nếu cần`)
        break
      case 'adjust':
        if (row.stockId) {
          void (async () => {
            try {
              const stock = await stocksApi.get(row.stockId!)
              setDetail(stock)
              setAdjust({ quantity: stock.quantity, reason: '' })
              setAdjustOpen(true)
            } catch (e) {
              toast.error(e instanceof ApiError ? e.message : 'Không mở được điều chỉnh')
            }
          })()
        }
        break
      case 'stocktake':
        navigate(`/admin/stocktakes?warehouseId=${row.warehouseId}`)
        break
      case 'audit':
        navigate(
          `/admin/transactions?warehouseId=${row.warehouseId}&productId=${row.productId}`,
        )
        break
    }
  }

  function runProcessStep(id: ProcessStepId) {
    setActiveStep(id)
    switch (id) {
      case 'opening':
        void openReconPanel()
        break
      case 'import':
        navigate(warehouseId ? `/admin/imports?warehouseId=${warehouseId}` : '/admin/imports')
        break
      case 'export':
        navigate(warehouseId ? `/admin/exports?warehouseId=${warehouseId}` : '/admin/exports')
        break
      case 'monitor':
        setPage(1)
        setFilterMode('low')
        toast.success('Đã lọc tồn thấp — chọn dòng để chỉnh ngưỡng min')
        break
      case 'adjust':
        if (detail) {
          setAdjust({ quantity: detail.quantity, reason: '' })
          setAdjustOpen(true)
        } else if (rows[0]) {
          void openDetail(rows[0]).then(() => {
            setAdjust({ quantity: rows[0].quantity, reason: '' })
            setAdjustOpen(true)
          })
          toast.success('Đã chọn dòng đầu danh sách để điều chỉnh — kiểm tra lý do trước khi lưu')
        } else {
          toast.error('Chưa có dòng tồn — hãy mở sổ tồn đầu kỳ hoặc nhập kho trước')
        }
        break
      case 'stocktake':
        navigate(warehouseId ? `/admin/stocktakes?warehouseId=${warehouseId}` : '/admin/stocktakes')
        break
      case 'audit':
        navigate(warehouseId ? `/admin/transactions?warehouseId=${warehouseId}` : '/admin/transactions')
        break
    }
  }

  async function submitOpening(e: FormEvent) {
    e.preventDefault()
    if (!opening.warehouseId || !opening.productId) {
      toast.error('Chọn kho và thuốc/vật tư chưa có dòng tồn')
      return
    }
    if (opening.quantity === '') {
      toast.error('Nhập số lượng đầu kỳ (có thể = 0 nếu mở sổ trống)')
      return
    }
    const reason = opening.reason.trim()
    if (reason.length < 3) {
      toast.error('Lý do tối thiểu 3 ký tự (bắt buộc ghi sổ)')
      return
    }
    try {
      setOpeningBusy(true)
      await stocksApi.createOpening({
        warehouseId: opening.warehouseId,
        productId: opening.productId,
        quantity: Number(opening.quantity),
        minQuantity: opening.minQuantity === '' ? null : Number(opening.minQuantity),
        reason,
      })
      toast.success(
        Number(opening.quantity) > 0
          ? 'Đã mở sổ và ghi nhận tồn đầu kỳ (Manual)'
          : 'Đã mở sổ với tồn 0 — phát sinh sau dùng Nhập kho',
      )
      setOpeningOpen(false)
      setActiveStep('import')
      setWarehouseId(opening.warehouseId)
      setPage(1)
      void reload()
      if (reconOpen) void loadReconciliation(opening.warehouseId)
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Khởi tạo tồn thất bại')
    } finally {
      setOpeningBusy(false)
    }
  }

  async function submitAdjust(e: FormEvent) {
    e.preventDefault()
    if (!detail) return
    try {
      const updated = await stocksApi.adjust(detail.id, {
        quantity: Number(adjust.quantity),
        reason: adjust.reason.trim(),
      })
      toast.success('Đã điều chỉnh tồn (ghi sổ Manual)')
      setAdjustOpen(false)
      setDetail(updated)
      void reload()
      void openDetail(updated)
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Điều chỉnh thất bại')
    }
  }

  async function submitMin(e: FormEvent) {
    e.preventDefault()
    if (!detail) return
    try {
      const updated = await stocksApi.updateMin(detail.id, {
        minQuantity: useProductMin ? null : Number(minQty || 0),
      })
      toast.success('Đã cập nhật ngưỡng min')
      setMinOpen(false)
      setDetail(updated)
      void reload()
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Cập nhật min thất bại')
    }
  }

  function stockBadge(row: WarehouseStockDto) {
    if (row.isOutOfStock) return <Badge tone="danger">Hết hàng</Badge>
    if (row.isLowStock) return <Badge tone="warn">Tồn thấp</Badge>
    return <Badge tone="ok">Đủ hàng</Badge>
  }

  function infoCell(label: string, value: ReactNode) {
    return (
      <Paper variant="outlined" className="p-2.5" sx={{ borderRadius: 2 }}>
        <Typography variant="caption" color="text.secondary" className="block" sx={{ fontSize: 11 }}>
          {label}
        </Typography>
        <Typography sx={{ fontWeight: 600, mt: 0.35, fontSize: 13.5, wordBreak: 'break-word' }}>{value}</Typography>
      </Paper>
    )
  }

  const activeMeta = PROCESS_STEPS.find((s) => s.id === activeStep)!

  return (
    <Box>
      <PageHeader
        title="Tồn kho"
        subtitle="Theo dõi tồn theo kho: đối chiếu nhập xuất, cảnh báo min, điều chỉnh, kiểm kê và lịch sử"
        icon={<LayersIcon color="primary" fontSize="small" />}
        actions={
          <Stack direction="row" spacing={1}>
            <Button
              size="small"
              startIcon={<RefreshIcon />}
              variant="outlined"
              onClick={() => void reload()}
              disabled={loading}
              sx={{ borderRadius: 2 }}
            >
              Làm mới
            </Button>
            <Button
              size="small"
              startIcon={<AddIcon />}
              variant="contained"
              onClick={() => void openReconPanel()}
              sx={{ borderRadius: 2 }}
            >
              Đối chiếu / Mở sổ
            </Button>
          </Stack>
        }
      />

      <Paper
        elevation={0}
        sx={{
          mb: 2,
          p: 2,
          borderRadius: 3,
          border: '1px solid',
          borderColor: 'divider',
          bgcolor: '#fff',
        }}
      >
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1}
          sx={{ mb: 1.5, justifyContent: 'space-between' }}
        >
          <Box>
            <Typography sx={{ fontWeight: 700, fontSize: 14 }}>Các bước quản lý tồn kho</Typography>
            <Typography variant="caption" color="text.secondary" sx={{ fontSize: 11.5 }}>
              Mở sổ một lần cho mỗi SKU tại kho. Hàng ngày dùng Nhập/Xuất. Cuối kỳ kiểm kê. Điều chỉnh khi có lý do.
            </Typography>
          </Box>
          <Chip
            size="small"
            label={activeMeta.label}
            sx={{
              fontWeight: 700,
              bgcolor: alpha(activeMeta.color, 0.1),
              color: activeMeta.color,
              alignSelf: { sm: 'flex-start' },
            }}
          />
        </Stack>

        <Stepper
          activeStep={stepIndex}
          alternativeLabel
          connector={<ProcessConnector />}
          sx={{ mb: 1.5, display: { xs: 'none', md: 'flex' } }}
        >
          {PROCESS_STEPS.map((s) => (
            <Step key={s.id} completed={false}>
              <StepLabel
                onClick={() => runProcessStep(s.id)}
                sx={{
                  cursor: 'pointer',
                  '& .MuiStepLabel-label': { fontSize: 11, fontWeight: activeStep === s.id ? 700 : 500 },
                }}
                icon={
                  <Box
                    sx={{
                      width: 32,
                      height: 32,
                      borderRadius: '50%',
                      display: 'grid',
                      placeItems: 'center',
                      bgcolor: activeStep === s.id ? s.color : alpha(s.color, 0.12),
                      color: activeStep === s.id ? '#fff' : s.color,
                      boxShadow: activeStep === s.id ? `0 6px 14px ${alpha(s.color, 0.35)}` : 'none',
                    }}
                  >
                    {s.icon}
                  </Box>
                }
              >
                {s.label.replace(/^\d+\.\s*/, '')}
              </StepLabel>
            </Step>
          ))}
        </Stepper>

        <Box
          className="grid gap-1.5"
          sx={{ gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr', lg: 'repeat(4, 1fr)', xl: 'repeat(7, 1fr)' } }}
        >
          {PROCESS_STEPS.map((s) => (
            <Paper
              key={s.id}
              variant="outlined"
              onClick={() => runProcessStep(s.id)}
              sx={{
                p: 1.25,
                borderRadius: 2,
                cursor: 'pointer',
                borderColor: activeStep === s.id ? s.color : 'divider',
                bgcolor: activeStep === s.id ? alpha(s.color, 0.06) : '#fff',
                transition: 'all .15s ease',
                '&:hover': { borderColor: s.color, bgcolor: alpha(s.color, 0.04) },
              }}
            >
              <Stack direction="row" spacing={0.75} sx={{ mb: 0.5, color: s.color, alignItems: 'center' }}>
                {s.icon}
                <Typography sx={{ fontWeight: 700, fontSize: 12 }}>{s.label}</Typography>
              </Stack>
              <Typography variant="caption" color="text.secondary" sx={{ fontSize: 10.5, lineHeight: 1.35, display: 'block' }}>
                {s.short}
              </Typography>
              <Stack direction="row" spacing={0.5} sx={{ mt: 0.75, color: s.color, alignItems: 'center' }}>
                <Typography variant="caption" sx={{ fontWeight: 700, fontSize: 10.5 }}>
                  Thực hiện
                </Typography>
                <ArrowForwardIcon sx={{ fontSize: 12 }} />
              </Stack>
            </Paper>
          ))}
        </Box>
      </Paper>

      {summary ? (
        <div className="mb-2 grid grid-cols-2 gap-2 md:grid-cols-4">
          {[
            { label: 'SKU đang theo dõi', value: summary.totalSkus },
            { label: 'Tồn thấp', value: summary.lowStockCount, warn: true },
            { label: 'Hết hàng', value: summary.outOfStockCount, danger: true },
            { label: 'Tổng số lượng', value: summary.totalQuantity },
          ].map((c) => (
            <Paper key={c.label} className="p-2.5" sx={{ borderRadius: 2 }} variant="outlined">
              <Typography variant="caption" color="text.secondary" sx={{ fontSize: 11 }}>
                {c.label}
              </Typography>
              <Typography
                sx={{
                  fontWeight: 700,
                  fontSize: 18,
                  color: c.danger ? 'error.main' : c.warn ? 'warning.main' : 'text.primary',
                }}
              >
                {Number(c.value).toLocaleString('vi-VN')}
              </Typography>
            </Paper>
          ))}
        </div>
      ) : null}

      <Paper className="p-3" sx={{ borderRadius: 3 }}>
        {error ? <Alert>{error}</Alert> : null}
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.25} className="mb-2">
          <TextField
            size="small"
            placeholder="Tìm SKU / tên SP / kho"
            value={search}
            onChange={(e) => {
              setPage(1)
              setSearch(e.target.value)
            }}
            className="md:max-w-xs"
            fullWidth
          />
          <TextField
            select
            size="small"
            label="Kho"
            value={warehouseId}
            onChange={(e) => {
              setPage(1)
              setWarehouseId(Number(e.target.value))
            }}
            className="md:w-64"
          >
            <MenuItem value={0}>Tất cả kho</MenuItem>
            {warehouses.map((w) => (
              <MenuItem key={w.id} value={w.id}>
                {w.code} · {w.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            size="small"
            label="Trạng thái"
            value={filterMode}
            onChange={(e) => {
              setPage(1)
              setFilterMode(e.target.value as FilterMode)
            }}
            className="md:w-48"
          >
            <MenuItem value="all">Tất cả</MenuItem>
            <MenuItem value="low">Chỉ tồn thấp</MenuItem>
            <MenuItem value="out">Chỉ hết hàng</MenuItem>
          </TextField>
        </Stack>

        <Typography variant="caption" color="text.secondary" className="mb-1.5" sx={{ display: 'block' }}>
          Đang xem: <strong>{warehouseLabel}</strong>. Chọn dòng để xem chi tiết, điều chỉnh hoặc lịch sử.
        </Typography>

        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Kho</TableCell>
                <TableCell>SKU</TableCell>
                <TableCell>Sản phẩm</TableCell>
                <TableCell>ĐVT</TableCell>
                <TableCell align="right">Tồn</TableCell>
                <TableCell align="right">Min</TableCell>
                <TableCell>TT</TableCell>
                <TableCell align="right">Thao tác</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((r) => (
                <TableRow
                  key={r.id}
                  hover
                  selected={detail?.id === r.id}
                  onClick={() => void openDetail(r)}
                  sx={{ cursor: 'pointer' }}
                >
                  <TableCell>
                    <Typography variant="body2" sx={{ fontWeight: 600, fontSize: 13 }}>
                      {r.warehouseCode}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {r.warehouseName}
                    </Typography>
                  </TableCell>
                  <TableCell>{r.productSku}</TableCell>
                  <TableCell>
                    <Typography variant="body2" sx={{ fontSize: 13 }}>
                      {r.productName}
                    </Typography>
                    {r.categoryName ? (
                      <Typography variant="caption" color="text.secondary">
                        {r.categoryName}
                      </Typography>
                    ) : null}
                  </TableCell>
                  <TableCell>{r.unitSymbol || '—'}</TableCell>
                  <TableCell align="right" sx={{ fontWeight: 700 }}>
                    {r.quantity}
                  </TableCell>
                  <TableCell align="right">{r.effectiveMinStock ?? r.minStock}</TableCell>
                  <TableCell>{stockBadge(r)}</TableCell>
                  <TableCell align="right" onClick={(e) => e.stopPropagation()}>
                    <Tooltip title="Chi tiết">
                      <IconButton size="small" onClick={() => void openDetail(r)}>
                        <VisibilityOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Điều chỉnh">
                      <IconButton
                        size="small"
                        onClick={() => {
                          setDetail(r)
                          setAdjust({ quantity: r.quantity, reason: '' })
                          setAdjustOpen(true)
                          setActiveStep('adjust')
                        }}
                      >
                        <EditNoteOutlinedIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                    <Tooltip title="Lịch sử">
                      <IconButton
                        size="small"
                        onClick={() =>
                          navigate(
                            `/admin/transactions?warehouseId=${r.warehouseId}&productId=${r.productId}`,
                          )
                        }
                      >
                        <HistoryIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))}
              {!rows.length && !loading ? (
                <TableRow>
                  <TableCell colSpan={8}>
                    Chưa có dữ liệu tồn kho — bắt đầu từ bước <strong>Mở sổ / Tồn đầu kỳ</strong> hoặc{' '}
                    <strong>Nhập kho</strong>.
                  </TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        </TableContainer>
        <Pager page={page} totalPages={totalPages} onChange={setPage} />
      </Paper>

      {detail ? (
        <Modal
          title={`${detail.productSku} · ${detail.warehouseCode}`}
          subtitle="Chi tiết tồn theo kho"
          icon={<LayersIcon />}
          accent="primary"
          onClose={() => setDetail(null)}
          maxWidth="md"
          actions={
            <>
              <Button size="small" startIcon={<CloseIcon />} variant="text" onClick={() => setDetail(null)} sx={{ mr: 'auto' }}>
                Đóng
              </Button>
              <Button
                size="small"
                startIcon={<FactCheckIcon />}
                variant="outlined"
                onClick={() => navigate(`/admin/stocktakes?warehouseId=${detail.warehouseId}`)}
                sx={{ borderRadius: 2 }}
              >
                Kiểm kho
              </Button>
              <Button
                size="small"
                startIcon={<HistoryIcon />}
                variant="outlined"
                onClick={() =>
                  navigate(
                    `/admin/transactions?warehouseId=${detail.warehouseId}&productId=${detail.productId}`,
                  )
                }
                sx={{ borderRadius: 2 }}
              >
                Lịch sử
              </Button>
              <Button
                size="small"
                startIcon={<TuneIcon />}
                variant="outlined"
                onClick={() => {
                  setUseProductMin(detail.minQuantity == null)
                  setMinQty(detail.minQuantity ?? detail.productMinStock ?? 0)
                  setMinOpen(true)
                  setActiveStep('monitor')
                }}
                sx={{ borderRadius: 2 }}
              >
                Min kho
              </Button>
              <Button
                size="small"
                variant="contained"
                onClick={() => {
                  setAdjust({ quantity: detail.quantity, reason: '' })
                  setAdjustOpen(true)
                  setActiveStep('adjust')
                }}
                sx={{ borderRadius: 2 }}
              >
                Điều chỉnh tồn
              </Button>
            </>
          }
        >
          <Box className="space-y-2.5">
            <Alert severity="info">
              Tồn thay đổi chính từ <strong>phiếu nhập / xuất / kiểm kê</strong>. Điều chỉnh thủ công chỉ khi ngoại lệ
              (hư hỏng, thất thoát, hiệu chỉnh sổ) và bắt buộc ghi rõ lý do.
            </Alert>
            <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
              {infoCell('Kho', `${detail.warehouseCode} · ${detail.warehouseName || ''}`)}
              {infoCell('Sản phẩm', `${detail.productSku} · ${detail.productName || ''}`)}
              {infoCell('Danh mục', detail.categoryName || '—')}
              {infoCell('ĐVT', detail.unitSymbol || '—')}
              {infoCell('Tồn hiện có', detail.quantity)}
              {infoCell(
                'Min hiệu lực',
                `${detail.effectiveMinStock ?? detail.minStock}${
                  detail.minQuantity == null ? ' (theo Product)' : ' (theo kho)'
                }`,
              )}
              {infoCell('Min Product', detail.productMinStock ?? '—')}
              {infoCell('Trạng thái', stockBadge(detail))}
            </div>

            <Typography sx={{ fontWeight: 700, fontSize: 13.5 }}>Biến động gần đây</Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Loại</TableCell>
                    <TableCell>Chứng từ</TableCell>
                    <TableCell align="right">Δ</TableCell>
                    <TableCell align="right">Sau</TableCell>
                    <TableCell>Thời gian</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {history.map((h) => (
                    <TableRow key={h.id}>
                      <TableCell>
                        <Badge>{h.type}</Badge>
                      </TableCell>
                      <TableCell>{h.referenceCode || '—'}</TableCell>
                      <TableCell
                        align="right"
                        sx={{ color: h.quantityChange >= 0 ? 'success.main' : 'error.main', fontWeight: 600 }}
                      >
                        {h.quantityChange >= 0 ? `+${h.quantityChange}` : h.quantityChange}
                      </TableCell>
                      <TableCell align="right">{h.quantityAfter}</TableCell>
                      <TableCell>{new Date(h.createdAt).toLocaleString('vi-VN')}</TableCell>
                    </TableRow>
                  ))}
                  {!history.length ? (
                    <TableRow>
                      <TableCell colSpan={5}>Chưa có giao dịch</TableCell>
                    </TableRow>
                  ) : null}
                </TableBody>
              </Table>
            </TableContainer>
          </Box>
        </Modal>
      ) : null}

      {reconOpen ? (
        <Modal
          title="Đối chiếu tồn kho"
          subtitle="So sánh nhập, xuất, điều chỉnh với tồn trong kho. Bấm thao tác để sang bước tiếp theo."
          icon={<Inventory2OutlinedIcon />}
          accent="primary"
          maxWidth="lg"
          onClose={() => !reconLoading && setReconOpen(false)}
          actions={
            <>
              <Button
                size="small"
                variant="text"
                onClick={() => setReconOpen(false)}
                disabled={reconLoading}
                sx={{ mr: 'auto' }}
              >
                Đóng
              </Button>
              <Button
                size="small"
                variant="outlined"
                startIcon={<RefreshIcon />}
                disabled={reconLoading}
                onClick={() => void loadReconciliation(warehouseId || warehouses[0]?.id || 0)}
                sx={{ borderRadius: 2 }}
              >
                Làm mới
              </Button>
              <Button
                size="small"
                variant="contained"
                startIcon={<AddIcon />}
                disabled={reconLoading}
                onClick={() => void openOpeningModal()}
                sx={{ borderRadius: 2 }}
              >
                Mở sổ SKU mới
              </Button>
            </>
          }
        >
          <Box className="space-y-2.5">
            <Alert severity="info">
              <strong>Công thức:</strong> Sổ tính = Tổng nhập − Tổng xuất + Điều chỉnh/Kiểm kê. Khi{' '}
              <strong>xuất nhiều hơn nhập</strong>, cần nhập thêm đúng sản phẩm. Khi{' '}
              <strong>tồn kho khác sổ tính</strong>, dùng Kiểm kê.
            </Alert>

            {importShortRows.length > 0 ? (
              <Alert severity="warning">
                <Typography sx={{ fontWeight: 700, fontSize: 13, mb: 0.75 }}>
                  Có {importShortRows.length} SKU xuất nhiều hơn nhập — cần nhập thêm hàng
                </Typography>
                <Box className="space-y-1">
                  {importShortRows.slice(0, 8).map((r) => (
                    <Stack
                      key={r.key}
                      direction={{ xs: 'column', sm: 'row' }}
                      spacing={1}
                      sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' } }}
                    >
                      <Typography variant="body2" sx={{ fontSize: 12.5 }}>
                        <strong>{r.productSku}</strong> · {r.productName}: nhập{' '}
                        <strong>{r.imported.toLocaleString('vi-VN')}</strong>, xuất{' '}
                        <strong>{r.exported.toLocaleString('vi-VN')}</strong>, thiếu tối thiểu{' '}
                        <strong style={{ color: '#c62828' }}>
                          {r.needImportQty.toLocaleString('vi-VN')}
                          {r.unitSymbol ? ` ${r.unitSymbol}` : ''}
                        </strong>
                      </Typography>
                      <Button
                        size="small"
                        variant="contained"
                        color="warning"
                        endIcon={<ArrowForwardIcon sx={{ fontSize: 14 }} />}
                        onClick={() => navigateImportProduct(r)}
                        sx={{ borderRadius: 2, textTransform: 'none', flexShrink: 0 }}
                      >
                        Nhập {r.productSku}
                      </Button>
                    </Stack>
                  ))}
                  {importShortRows.length > 8 ? (
                    <Typography variant="caption" color="text.secondary">
                      … và {importShortRows.length - 8} SKU khác trong bảng bên dưới
                    </Typography>
                  ) : null}
                </Box>
              </Alert>
            ) : null}

            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} className="mb-1">
              <TextField
                select
                size="small"
                label="Kho đối chiếu"
                value={warehouseId || warehouses[0]?.id || ''}
                onChange={(e) => {
                  const wh = Number(e.target.value)
                  setWarehouseId(wh)
                  void loadReconciliation(wh)
                }}
                sx={{ minWidth: 260 }}
                disabled={reconLoading}
              >
                {warehouses.map((w) => (
                  <MenuItem key={w.id} value={w.id}>
                    {w.code} · {w.name}
                  </MenuItem>
                ))}
              </TextField>
            </Stack>

            {reconTotals ? (
              <Box
                className="grid gap-1.5"
                sx={{ gridTemplateColumns: { xs: '1fr 1fr', md: 'repeat(7, 1fr)' } }}
              >
                {[
                  { label: 'Tổng nhập', value: reconTotals.imported, color: '#0B6E4F' },
                  { label: 'Tổng xuất', value: reconTotals.exported, color: '#c62828' },
                  { label: 'Tồn trong kho', value: reconTotals.onHand, color: '#1565c0' },
                  { label: 'Đ.chỉnh / KK', value: reconTotals.adjusted, color: '#6a1b9a' },
                  { label: 'SKU xuất > nhập', value: reconTotals.importShortSkus, color: '#e65100' },
                  { label: 'SKU lệch sổ', value: reconTotals.diffSkus, color: '#ed6c02' },
                  { label: 'SKU chưa mở sổ', value: reconTotals.freeSkuCount, color: '#455a64' },
                ].map((c) => (
                  <Paper key={c.label} variant="outlined" sx={{ p: 1.25, borderRadius: 2 }}>
                    <Typography variant="caption" color="text.secondary" sx={{ fontSize: 10.5 }}>
                      {c.label}
                    </Typography>
                    <Typography sx={{ fontWeight: 800, fontSize: 16, color: c.color }}>
                      {Number(c.value).toLocaleString('vi-VN')}
                    </Typography>
                  </Paper>
                ))}
              </Box>
            ) : null}

            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
              {(
                [
                  { id: 'import' as const, label: 'Nhập kho', color: '#0B6E4F' },
                  { id: 'export' as const, label: 'Xuất kho', color: '#c62828' },
                  { id: 'stocktake' as const, label: 'Kiểm kê', color: '#00897b' },
                  { id: 'audit' as const, label: 'Lịch sử', color: '#455a64' },
                ] as const
              ).map((a) => (
                <Button
                  key={a.id}
                  size="small"
                  variant="outlined"
                  onClick={() => {
                    setReconOpen(false)
                    runProcessStep(a.id)
                  }}
                  sx={{ borderRadius: 2, borderColor: a.color, color: a.color }}
                >
                  {a.label}
                </Button>
              ))}
            </Box>

            <TableContainer sx={{ maxHeight: 420 }}>
              <Table size="small" stickyHeader>
                <TableHead>
                  <TableRow>
                    <TableCell>SKU / Thuốc</TableCell>
                    <TableCell align="right">SL nhập</TableCell>
                    <TableCell align="right">SL xuất</TableCell>
                    <TableCell align="right">Đ.chỉnh/KK</TableCell>
                    <TableCell align="right">Sổ tính</TableCell>
                    <TableCell align="right">Tồn kho</TableCell>
                    <TableCell align="right">Chênh lệch</TableCell>
                    <TableCell>Gợi ý</TableCell>
                    <TableCell align="right">Thao tác</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {reconLoading ? (
                    <TableRow>
                      <TableCell colSpan={9}>Đang tổng hợp nhập / xuất / tồn…</TableCell>
                    </TableRow>
                  ) : null}
                  {!reconLoading &&
                    reconRows.map((r) => {
                      const leaked = Math.abs(r.diff) > 0.0001
                      const shortImport = r.needImportQty > 0
                      return (
                        <TableRow
                          key={r.key}
                          hover
                          selected={shortImport || leaked}
                          sx={
                            shortImport
                              ? { bgcolor: alpha('#e65100', 0.06) }
                              : undefined
                          }
                        >
                          <TableCell>
                            <Typography sx={{ fontWeight: 700, fontSize: 12.5 }}>{r.productSku}</Typography>
                            <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                              {r.productName}
                              {r.unitSymbol ? ` · ${r.unitSymbol}` : ''}
                            </Typography>
                            {shortImport ? (
                              <Typography variant="caption" sx={{ color: '#e65100', fontWeight: 700, display: 'block' }}>
                                Thiếu nhập ≥ {r.needImportQty.toLocaleString('vi-VN')}
                                {r.unitSymbol ? ` ${r.unitSymbol}` : ''}
                              </Typography>
                            ) : null}
                          </TableCell>
                          <TableCell align="right" sx={{ color: '#0B6E4F', fontWeight: 600 }}>
                            {r.imported.toLocaleString('vi-VN')}
                          </TableCell>
                          <TableCell align="right" sx={{ color: '#c62828', fontWeight: 600 }}>
                            {r.exported.toLocaleString('vi-VN')}
                          </TableCell>
                          <TableCell align="right">{r.adjusted.toLocaleString('vi-VN')}</TableCell>
                          <TableCell align="right">{r.book.toLocaleString('vi-VN')}</TableCell>
                          <TableCell align="right" sx={{ fontWeight: 700 }}>
                            {r.onHand.toLocaleString('vi-VN')}
                          </TableCell>
                          <TableCell
                            align="right"
                            sx={{
                              fontWeight: 700,
                              color: shortImport || leaked ? 'warning.main' : 'success.main',
                            }}
                          >
                            {r.diff === 0 ? '0' : r.diff > 0 ? `+${r.diff}` : r.diff}
                          </TableCell>
                          <TableCell>
                            <Chip
                              size="small"
                              label={r.suggestLabel}
                              sx={{
                                fontWeight: 700,
                                fontSize: 11,
                                bgcolor: alpha(
                                  PROCESS_STEPS.find((s) => s.id === r.suggest)?.color || '#455a64',
                                  0.12,
                                ),
                                color: PROCESS_STEPS.find((s) => s.id === r.suggest)?.color || '#455a64',
                              }}
                            />
                          </TableCell>
                          <TableCell align="right">
                            <Button
                              size="small"
                              variant="contained"
                              color={shortImport ? 'warning' : 'primary'}
                              endIcon={<ArrowForwardIcon sx={{ fontSize: 14 }} />}
                              onClick={() => goReconAction(r)}
                              sx={{
                                borderRadius: 2,
                                textTransform: 'none',
                                ...(!shortImport
                                  ? {
                                      bgcolor: PROCESS_STEPS.find((s) => s.id === r.suggest)?.color,
                                      '&:hover': {
                                        bgcolor: PROCESS_STEPS.find((s) => s.id === r.suggest)?.color,
                                        filter: 'brightness(0.92)',
                                      },
                                    }
                                  : {}),
                              }}
                            >
                              {shortImport ? `Nhập thêm ${r.productSku}` : r.suggestLabel}
                            </Button>
                          </TableCell>
                        </TableRow>
                      )
                    })}
                  {!reconLoading && !reconRows.length ? (
                    <TableRow>
                      <TableCell colSpan={9}>
                        Chưa có dữ liệu tồn hoặc giao dịch tại kho này. Bấm <strong>Mở sổ SKU mới</strong> hoặc{' '}
                        <strong>Nhập kho</strong>.
                      </TableCell>
                    </TableRow>
                  ) : null}
                </TableBody>
              </Table>
            </TableContainer>
          </Box>
        </Modal>
      ) : null}

      {openingOpen ? (
        <Modal
          title="Mở sổ / Tồn đầu kỳ"
          subtitle="Chỉ dùng một lần cho mỗi cặp kho–SKU: chuyển số dư hoặc kiểm đếm khi triển khai"
          icon={<Inventory2OutlinedIcon />}
          accent="primary"
          onClose={() => !openingBusy && setOpeningOpen(false)}
          actions={
            <>
              <Button
                size="small"
                variant="text"
                onClick={() => setOpeningOpen(false)}
                disabled={openingBusy}
                sx={{ mr: 'auto' }}
              >
                Hủy
              </Button>
              <Button
                size="small"
                type="submit"
                form="opening-form"
                variant="contained"
                disabled={openingBusy || !openingProducts.length}
                sx={{ borderRadius: 2 }}
              >
                {openingBusy ? 'Đang lưu…' : 'Lưu mở sổ tồn'}
              </Button>
            </>
          }
        >
          <Stack component="form" id="opening-form" onSubmit={submitOpening} spacing={2}>
            <Alert severity="info">
              Chỉ mở sổ khi SKU chưa có dòng tồn tại kho. Sau đó dùng <strong>Nhập kho</strong>,{' '}
              <strong>Xuất kho</strong>, <strong>Kiểm kê</strong> hoặc <strong>Điều chỉnh</strong>. Không mở sổ lần 2.
            </Alert>
            <TextField
              select
              fullWidth
              required
              size="small"
              label="Kho"
              value={opening.warehouseId || ''}
              disabled={openingBusy}
              onChange={(e) => {
                const wh = Number(e.target.value)
                void (async () => {
                  setOpeningBusy(true)
                  try {
                    let occupied = new Set<number>()
                    if (wh) {
                      const list = await stocksApi.list({
                        pageNumber: 1,
                        pageSize: 500,
                        warehouseId: wh,
                      })
                      occupied = new Set(list.items.map((x) => x.productId))
                    }
                    setOccupiedProductIds(occupied)
                    const firstFree = products.find((p) => !occupied.has(p.id))
                    setOpening((s) => ({
                      ...s,
                      warehouseId: wh,
                      productId: firstFree?.id || 0,
                    }))
                  } finally {
                    setOpeningBusy(false)
                  }
                })()
              }}
              slotProps={{
                select: {
                  MenuProps: {
                    disablePortal: true,
                    slotProps: { paper: { sx: { maxHeight: 280 } } },
                  },
                },
              }}
            >
              {warehouses.map((w) => (
                <MenuItem key={w.id} value={w.id}>
                  {w.code} · {w.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              select
              fullWidth
              required
              size="small"
              label="Thuốc / vật tư (chưa có tồn tại kho)"
              value={opening.productId || ''}
              disabled={openingBusy || !openingProducts.length}
              onChange={(e) => setOpening((s) => ({ ...s, productId: Number(e.target.value) }))}
              helperText={
                !openingProducts.length
                  ? 'Kho này đã có dòng tồn cho mọi SKU đang active — dùng Nhập kho / Điều chỉnh / Kiểm kê'
                  : `${openingProducts.length} SKU chưa mở sổ tại kho này`
              }
              slotProps={{
                select: {
                  MenuProps: {
                    disablePortal: true,
                    slotProps: { paper: { sx: { maxHeight: 280 } } },
                  },
                },
              }}
            >
              {openingProducts.map((p) => (
                <MenuItem key={p.id} value={p.id}>
                  {p.sku} · {p.name}
                  {p.unitSymbol ? ` (${p.unitSymbol})` : ''}
                </MenuItem>
              ))}
            </TextField>
            {openingProduct ? (
              <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: -1 }}>
                ĐVT: <strong>{openingProduct.unitSymbol || openingProduct.unitName || '—'}</strong>
                {' · '}
                Min sản phẩm: <strong>{openingProduct.minStock}</strong>
              </Typography>
            ) : null}
            <TextField
              fullWidth
              required
              size="small"
              type="number"
              label="Số lượng đầu kỳ (theo kiểm đếm thực tế)"
              value={opening.quantity}
              disabled={openingBusy}
              onChange={(e) =>
                setOpening((s) => ({
                  ...s,
                  quantity: e.target.value === '' ? '' : Number(e.target.value),
                }))
              }
              helperText="Nhập đúng số kiểm đếm. Cho phép 0 nếu chỉ mở sổ trống (chưa có hàng)."
              slotProps={{ htmlInput: { min: 0, step: 'any' } }}
            />
            <TextField
              fullWidth
              size="small"
              type="number"
              label="Min theo kho (tuỳ chọn)"
              value={opening.minQuantity}
              disabled={openingBusy}
              onChange={(e) =>
                setOpening((s) => ({
                  ...s,
                  minQuantity: e.target.value === '' ? '' : Number(e.target.value),
                }))
              }
              helperText={
                openingProduct
                  ? `Để trống = dùng Min sản phẩm (${openingProduct.minStock})`
                  : 'Để trống = dùng Min của sản phẩm'
              }
              slotProps={{ htmlInput: { min: 0 } }}
            />
            <TextField
              fullWidth
              required
              size="small"
              label="Lý do / diễn giải (ghi sổ)"
              value={opening.reason}
              disabled={openingBusy}
              onChange={(e) => setOpening((s) => ({ ...s, reason: e.target.value }))}
              helperText="Bắt buộc ≥ 3 ký tự — phục vụ đối soát (VD: Chuyển số dư kỳ 01/2026)"
              slotProps={{ htmlInput: { minLength: 3, maxLength: 255 } }}
            />
          </Stack>
        </Modal>
      ) : null}

      {adjustOpen && detail ? (
        <Modal
          title="Điều chỉnh tồn"
          subtitle="Hiệu chỉnh sổ sách, bắt buộc ghi lý do"
          icon={<EditNoteOutlinedIcon />}
          accent="warning"
          onClose={() => setAdjustOpen(false)}
          actions={
            <>
              <Button size="small" variant="text" onClick={() => setAdjustOpen(false)} sx={{ mr: 'auto' }}>
                Hủy
              </Button>
              <Button size="small" type="submit" form="adjust-form" variant="contained" color="warning" sx={{ borderRadius: 2 }}>
                Xác nhận điều chỉnh
              </Button>
            </>
          }
        >
          <Box component="form" id="adjust-form" onSubmit={submitAdjust} className="space-y-2.5">
            <Alert severity="warning">
              Đang chỉnh <strong>{detail.productSku}</strong> tại <strong>{detail.warehouseCode}</strong>. Tồn hiện
              tại: <strong>{detail.quantity}</strong>. Không thay cho phiếu nhập/xuất. Hệ thống ghi giao dịch{' '}
              <strong>Manual</strong>.
            </Alert>
            <TextField
              fullWidth
              required
              size="small"
              type="number"
              label="Tồn mới (sau điều chỉnh)"
              value={adjust.quantity}
              onChange={(e) => setAdjust((s) => ({ ...s, quantity: Number(e.target.value) }))}
              slotProps={{ htmlInput: { min: 0, step: 'any' } }}
            />
            <TextField
              fullWidth
              required
              size="small"
              label="Lý do điều chỉnh"
              placeholder="VD: Hư hỏng, thất thoát, hiệu chỉnh sổ sách..."
              value={adjust.reason}
              onChange={(e) => setAdjust((s) => ({ ...s, reason: e.target.value }))}
              multiline
              minRows={2}
            />
          </Box>
        </Modal>
      ) : null}

      {minOpen && detail ? (
        <Modal
          title="Ngưỡng cảnh báo min"
          subtitle="Mức tồn tối thiểu theo kho để cảnh báo sắp hết"
          icon={<WarningAmberRoundedIcon />}
          accent="warning"
          onClose={() => setMinOpen(false)}
          actions={
            <>
              <Button size="small" variant="text" onClick={() => setMinOpen(false)} sx={{ mr: 'auto' }}>
                Hủy
              </Button>
              <Button size="small" type="submit" form="min-form" variant="contained" sx={{ borderRadius: 2 }}>
                Lưu ngưỡng min
              </Button>
            </>
          }
        >
          <Box component="form" id="min-form" onSubmit={submitMin} className="space-y-2.5">
            <Typography variant="body2" color="text.secondary" sx={{ fontSize: 13 }}>
              Product.MinStock mặc định = <strong>{detail.productMinStock}</strong>
            </Typography>
            <FormControlLabel
              control={
                <Checkbox checked={useProductMin} onChange={(e) => setUseProductMin(e.target.checked)} />
              }
              label="Dùng Min của sản phẩm"
            />
            {!useProductMin ? (
              <TextField
                fullWidth
                required
                size="small"
                type="number"
                label="Min theo kho"
                value={minQty}
                onChange={(e) => setMinQty(e.target.value === '' ? '' : Number(e.target.value))}
                slotProps={{ htmlInput: { min: 0 } }}
              />
            ) : null}
          </Box>
        </Modal>
      ) : null}
    </Box>
  )
}
