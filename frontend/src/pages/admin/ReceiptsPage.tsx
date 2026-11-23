import { useEffect, useMemo, useRef, useState, type FormEvent, type ReactNode } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  alpha,
  Box,
  Button,
  Chip,
  Divider,
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
import InputIcon from '@mui/icons-material/Input'
import OutputIcon from '@mui/icons-material/Output'
import AddIcon from '@mui/icons-material/Add'
import RefreshIcon from '@mui/icons-material/Refresh'
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlineOutlined'
import CancelOutlinedIcon from '@mui/icons-material/CancelOutlined'
import DeleteIcon from '@mui/icons-material/Delete'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import PrintIcon from '@mui/icons-material/Print'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import DeleteOutlinedIcon from '@mui/icons-material/DeleteOutlined'
import FileDownloadOutlinedIcon from '@mui/icons-material/FileDownloadOutlined'
import ReceiptLongIcon from '@mui/icons-material/ReceiptLong'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import ArrowForwardIcon from '@mui/icons-material/ArrowForward'
import SaveOutlinedIcon from '@mui/icons-material/SaveOutlined'
import Inventory2OutlinedIcon from '@mui/icons-material/Inventory2Outlined'
import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined'
import FactCheckOutlinedIcon from '@mui/icons-material/FactCheckOutlined'
import WarehouseOutlinedIcon from '@mui/icons-material/WarehouseOutlined'
import { customersApi, exportsApi, importsApi, productsApi, suppliersApi, warehousesApi } from '../../api/endpoints'
import { ApiError } from '../../api/client'
import { useToast } from '../../components/ToastContext'
import { exportPurchaseInvoiceCsv, printPurchaseInvoiceVn } from '../../utils/purchaseInvoiceVn'
import { openPrintHtml, openPrintShell, writePrintHtml } from '../../utils/printWindow'
import {
  Alert,
  Badge,
  ConfirmDialog,
  Modal,
  PageHeader,
  Pager,
  formatDateTime,
  fromDateTimeLocal,
  money,
  statusTone,
  toDateTimeLocalValue,
} from '../../components/ui'
import type { ExportReceiptDto, ImportReceiptDto, ProductDto, WarehouseDto } from '../../types/api'

type Kind = 'import' | 'export'
type LineForm = { key: string; productId: number; quantity: number; unitPrice: number; note: string }

const STEPS_IMPORT = ['Thông tin & chi phí', 'Danh sách thuốc nhập', 'Xem lại & lưu nháp']
const STEPS_EXPORT = ['Thông tin phiếu xuất', 'Danh sách thuốc xuất', 'Xem lại & lưu nháp']

const WizardConnector = styled(StepConnector)(({ theme }) => ({
  [`&.${stepConnectorClasses.alternativeLabel}`]: { top: 18 },
  [`&.${stepConnectorClasses.active}`]: {
    [`& .${stepConnectorClasses.line}`]: {
      background: `linear-gradient(90deg, ${theme.palette.primary.main}, ${theme.palette.primary.light})`,
    },
  },
  [`&.${stepConnectorClasses.completed}`]: {
    [`& .${stepConnectorClasses.line}`]: { backgroundColor: theme.palette.primary.main },
  },
  [`& .${stepConnectorClasses.line}`]: {
    height: 3,
    border: 0,
    borderRadius: 2,
    backgroundColor: alpha(theme.palette.text.primary, 0.12),
  },
}))

function WizardStepIcon({
  active,
  completed,
  icon,
  accent,
}: {
  active?: boolean
  completed?: boolean
  icon: ReactNode
  accent: string
}) {
  return (
    <Box
      sx={{
        width: 38,
        height: 38,
        borderRadius: '50%',
        display: 'grid',
        placeItems: 'center',
        bgcolor: completed || active ? accent : alpha('#0F172A', 0.08),
        color: completed || active ? '#fff' : '#64748B',
        boxShadow: active ? `0 8px 18px ${alpha(accent, 0.35)}` : 'none',
        transition: 'all .2s ease',
        '& .MuiSvgIcon-root': { fontSize: 18 },
      }}
    >
      {completed ? <CheckCircleOutlineIcon /> : icon}
    </Box>
  )
}

type ImportCosts = {
  shippingFee: number
  tradeDiscount: number
  nonRefundableTax: number
}

export function ImportsPage() {
  return <ReceiptsPage kind="import" />
}

export function ExportsPage() {
  return <ReceiptsPage kind="export" />
}

function isStoreExport(r: ImportReceiptDto | ExportReceiptDto) {
  if (!('source' in r) && !('note' in r)) return false
  const ex = r as ExportReceiptDto
  return (
    ex.source === 'Store' ||
    !!ex.note?.includes('FreshMart') ||
    !!ex.note?.includes('Nhà thuốc BV')
  )
}

function newLine(product?: ProductDto): LineForm {
  return {
    key: `${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
    productId: product?.id || 0,
    quantity: 1,
    unitPrice: product?.price || 0,
    note: '',
  }
}

function buildReceiptPrintHtml(
  kind: Kind,
  detail: ImportReceiptDto | ExportReceiptDto,
) {
  const partnerLabel = kind === 'import' ? 'Nhà cung cấp' : 'Khoa / phòng nhận'
  const partnerName =
    kind === 'import'
      ? (detail as ImportReceiptDto).supplierName
      : (detail as ExportReceiptDto).customerName
  const title = kind === 'import' ? 'PHIẾU NHẬP KHO DƯỢC' : 'PHIẾU XUẤT KHO DƯỢC'
  const rows = detail.details
    .map(
      (d, i) =>
        `<tr>
          <td style="text-align:center">${i + 1}</td>
          <td>${d.productSku || ''}</td>
          <td>${d.productName || ''}</td>
          <td style="text-align:right">${d.quantity}</td>
          <td>${d.unitSymbol || ''}</td>
          <td style="text-align:right">${Number(d.unitPrice).toLocaleString('vi-VN')}</td>
          <td style="text-align:right">${Number(d.lineTotal).toLocaleString('vi-VN')}</td>
        </tr>`,
    )
    .join('')

  return `<!doctype html>
<html lang="vi"><head><meta charset="utf-8"/>
<title>${detail.code}</title>
<style>
  body{font-family:Arial,sans-serif;padding:24px;color:#111}
  h1{font-size:18px;margin:0 0 4px;text-align:center}
  .sub{text-align:center;color:#555;margin-bottom:16px;font-size:12px}
  .meta{font-size:13px;margin-bottom:12px}
  .meta div{margin:3px 0}
  table{width:100%;border-collapse:collapse;font-size:12px}
  th,td{border:1px solid #333;padding:6px}
  th{background:#f2f2f2}
  .foot{margin-top:18px;display:flex;justify-content:space-between;font-size:12px}
  .sign{width:30%;text-align:center}
  @media print{button{display:none}}
</style></head><body>
  <h1>${title}</h1>
  <div class="sub">Kho nhà thuốc bệnh viện · Mã ${detail.code} · Trạng thái ${detail.status}</div>
  <div class="meta">
    <div><b>Thời gian:</b> ${formatDateTime(detail.receiptDate)}</div>
    <div><b>Kho:</b> ${detail.warehouseCode || ''} — ${detail.warehouseName || ''}</div>
    <div><b>${partnerLabel}:</b> ${partnerName || ''}</div>
    ${detail.note ? `<div><b>Ghi chú:</b> ${detail.note}</div>` : ''}
    ${detail.confirmedAt ? `<div><b>Xác nhận:</b> ${formatDateTime(detail.confirmedAt)}</div>` : ''}
  </div>
  <table>
    <thead>
      <tr>
        <th>STT</th><th>SKU</th><th>Tên thuốc / VT</th><th>SL</th><th>ĐVT</th><th>Đơn giá</th><th>Thành tiền</th>
      </tr>
    </thead>
    <tbody>${rows}</tbody>
  </table>
  <div class="meta" style="margin-top:10px">
    <b>Tổng SL:</b> ${detail.totalQuantity}
    &nbsp;&nbsp;|&nbsp;&nbsp;
    <b>Tổng tiền:</b> ${Number(detail.totalAmount).toLocaleString('vi-VN')} đ
  </div>
  <div class="foot">
    <div class="sign"><div>Người lập</div><br/><br/>(Ký, họ tên)</div>
    <div class="sign"><div>Thủ kho</div><br/><br/>(Ký, họ tên)</div>
    <div class="sign"><div>${kind === 'import' ? 'Kế toán' : 'Khoa nhận'}</div><br/><br/>(Ký, họ tên)</div>
  </div>
  <script>window.onload=()=>window.print()</script>
</body></html>`
}

function printReceiptReport(
  kind: Kind,
  detail: ImportReceiptDto | ExportReceiptDto,
) {
  openPrintHtml(buildReceiptPrintHtml(kind, detail), { width: 900, height: 700 })
}

async function printReceiptAfterFetch(
  kind: Kind,
  id: number,
  api: { get: (id: number) => Promise<ImportReceiptDto | ExportReceiptDto> },
  onError: (msg: string) => void,
) {
  // Mở cửa sổ ngay trong click (tránh popup blocker sau await)
  const shell = openPrintShell({ width: 900, height: 700 })
  try {
    const full = await api.get(id)
    const html = buildReceiptPrintHtml(kind, full)
    if (shell) writePrintHtml(shell, html)
    else openPrintHtml(html, { width: 900, height: 700 })
  } catch (err) {
    try {
      shell?.close()
    } catch {
      /* ignore */
    }
    onError(err instanceof ApiError ? err.message : 'Không tải được phiếu để in')
  }
}

function ReceiptsPage({ kind }: { kind: Kind }) {
  const toast = useToast()
  const [searchParams, setSearchParams] = useSearchParams()
  const prefApplied = useRef(false)
  const api = kind === 'import' ? importsApi : exportsApi
  const KindIcon = kind === 'import' ? InputIcon : OutputIcon
  const steps = kind === 'import' ? STEPS_IMPORT : STEPS_EXPORT

  const [rows, setRows] = useState<(ImportReceiptDto | ExportReceiptDto)[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<'all' | 'Draft' | 'Confirmed' | 'Cancelled'>('all')
  const [sourceFilter, setSourceFilter] = useState<'all' | 'store' | 'manual'>('all')
  const [error, setError] = useState('')
  const [open, setOpen] = useState(false)
  const [wizardStep, setWizardStep] = useState(0)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [detail, setDetail] = useState<ImportReceiptDto | ExportReceiptDto | null>(null)
  const [confirmAct, setConfirmAct] = useState<{ id: number; action: 'confirm' | 'cancel' | 'remove' } | null>(null)
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([])
  const [partners, setPartners] = useState<{ id: number; name: string }[]>([])
  const [products, setProducts] = useState<ProductDto[]>([])
  const [header, setHeader] = useState({
    warehouseId: 0,
    partnerId: 0,
    receiptDate: toDateTimeLocalValue(),
    note: '',
  })
  const [costs, setCosts] = useState<ImportCosts>({
    shippingFee: 0,
    tradeDiscount: 0,
    nonRefundableTax: 0,
  })
  const [lines, setLines] = useState<LineForm[]>([newLine()])
  const [prefHint, setPrefHint] = useState('')

  const lineTotal = useMemo(
    () => lines.reduce((s, l) => s + (Number(l.quantity) || 0) * (Number(l.unitPrice) || 0), 0),
    [lines],
  )
  const qtyTotal = useMemo(() => lines.reduce((s, l) => s + (Number(l.quantity) || 0), 0), [lines])
  const payableTotal = useMemo(() => {
    if (kind !== 'import') return lineTotal
    return (
      lineTotal -
      (Number(costs.tradeDiscount) || 0) +
      (Number(costs.shippingFee) || 0) +
      (Number(costs.nonRefundableTax) || 0)
    )
  }, [kind, lineTotal, costs])

  async function reload() {
    try {
      const q: Record<string, unknown> = { search, pageNumber: page, pageSize: 12 }
      if (statusFilter !== 'all') q.status = statusFilter
      if (kind === 'export' && sourceFilter === 'store') q.source = 'Store'
      if (kind === 'export' && sourceFilter === 'manual') q.source = 'Manual'
      const data = await api.list(q)
      setRows(data.items)
      setTotalPages(data.totalPages || 1)
      setError('')
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Lỗi tải phiếu')
    }
  }

  useEffect(() => {
    prefApplied.current = false
    void (async () => {
      try {
        const [w, p, pr] = await Promise.all([
          warehousesApi.list({ pageNumber: 1, pageSize: 200 }),
          kind === 'import'
            ? suppliersApi.list({ pageNumber: 1, pageSize: 200 })
            : customersApi.list({ pageNumber: 1, pageSize: 200 }),
          productsApi.list({ pageNumber: 1, pageSize: 500 }),
        ])
        setWarehouses(w.items)
        setPartners(p.items.map((x) => ({ id: x.id, name: x.name })))
        setProducts(pr.items.filter((x) => x.isActive))
        const qWh = Number(searchParams.get('warehouseId') || 0) || 0
        setHeader((h) => ({
          ...h,
          warehouseId: qWh || h.warehouseId || w.items[0]?.id || 0,
          partnerId: h.partnerId || p.items[0]?.id || 0,
        }))
        setLines((prev) => {
          if (prev.length === 1 && !prev[0].productId && pr.items[0]) return [newLine(pr.items[0])]
          return prev
        })
        setError('')
      } catch (e) {
        setError(e instanceof ApiError ? e.message : 'Không tải được dữ liệu phụ trợ')
      }
    })()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [kind])

  useEffect(() => {
    void reload()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, search, kind, sourceFilter, statusFilter])

  // Từ trang Tồn kho đối chiếu: mở phiếu nhập đúng sản phẩm khi xuất > nhập
  useEffect(() => {
    if (kind !== 'import' || prefApplied.current) return
    if (!products.length || !warehouses.length) return
    const openCreateFlag = searchParams.get('openCreate') === '1'
    const productId = Number(searchParams.get('productId') || 0) || 0
    const warehouseId = Number(searchParams.get('warehouseId') || 0) || 0
    const suggestedQty = Number(searchParams.get('suggestedQty') || 0) || 0
    if (!openCreateFlag || !productId) return

    const product = products.find((p) => p.id === productId)
    if (!product) {
      toast.error('Không tìm thấy sản phẩm cần nhập thêm')
      prefApplied.current = true
      return
    }

    prefApplied.current = true
    const qty = suggestedQty > 0 ? suggestedQty : 1
    setEditingId(null)
    setWizardStep(1)
    setHeader((h) => ({
      ...h,
      warehouseId: warehouseId || h.warehouseId || warehouses[0]?.id || 0,
      receiptDate: toDateTimeLocalValue(),
      note: `Nhập bổ sung theo đối chiếu tồn (xuất vượt nhập) — ${product.sku}`,
    }))
    setCosts({ shippingFee: 0, tradeDiscount: 0, nonRefundableTax: 0 })
    setLines([
      {
        ...newLine(product),
        quantity: qty,
        note: `Thiếu tối thiểu ${qty}${product.unitSymbol ? ` ${product.unitSymbol}` : ''} so với tổng xuất`,
      },
    ])
    setPrefHint(
      `Đã chọn sẵn ${product.sku} · ${product.name}. Số lượng gợi ý ${qty.toLocaleString('vi-VN')}${
        product.unitSymbol ? ` ${product.unitSymbol}` : ''
      } vì xuất nhiều hơn nhập. Kiểm tra NCC, lưu nháp rồi xác nhận.`,
    )
    setOpen(true)
    toast.success(`Đã mở phiếu nhập cho ${product.sku}`)

    const next = new URLSearchParams(searchParams)
    next.delete('openCreate')
    next.delete('productId')
    next.delete('suggestedQty')
    setSearchParams(next, { replace: true })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [kind, products, warehouses, searchParams])

  function openCreate() {
    setEditingId(null)
    setWizardStep(0)
    setPrefHint('')
    setHeader((h) => ({
      ...h,
      receiptDate: toDateTimeLocalValue(),
      note: '',
    }))
    setCosts({ shippingFee: 0, tradeDiscount: 0, nonRefundableTax: 0 })
    setLines([newLine(products[0])])
    setOpen(true)
  }

  async function openEdit(id: number) {
    try {
      const r = await api.get(id)
      if (r.status !== 'Draft') {
        toast.error('Chỉ sửa được phiếu ở trạng thái Nháp (Draft)')
        return
      }
      setEditingId(r.id)
      setWizardStep(0)
      setHeader({
        warehouseId: r.warehouseId,
        partnerId: kind === 'import' ? (r as ImportReceiptDto).supplierId : (r as ExportReceiptDto).customerId,
        receiptDate: toDateTimeLocalValue(new Date(r.receiptDate)),
        note: r.note || '',
      })
      if (kind === 'import') {
        const imp = r as ImportReceiptDto
        setCosts({
          shippingFee: Number(imp.shippingFee) || 0,
          tradeDiscount: Number(imp.tradeDiscount) || 0,
          nonRefundableTax: Number(imp.nonRefundableTax) || 0,
        })
      }
      setLines(
        r.details.map((d) => ({
          key: `e-${d.id}`,
          productId: d.productId,
          quantity: d.quantity,
          unitPrice: d.unitPrice,
          note: d.note || '',
        })),
      )
      setOpen(true)
    } catch (e) {
      toast.error(e instanceof ApiError ? e.message : 'Không mở được phiếu')
    }
  }

  function validateHeader() {
    if (!header.warehouseId) return 'Chọn kho'
    if (!header.partnerId) return kind === 'import' ? 'Chọn nhà cung cấp' : 'Chọn khoa/phòng nhận'
    if (!header.receiptDate) return 'Chọn thời gian phiếu'
    if (kind === 'import') {
      if (costs.shippingFee < 0 || costs.tradeDiscount < 0 || costs.nonRefundableTax < 0)
        return 'Chi phí / chiết khấu / thuế không được âm'
    }
    return ''
  }

  function validateLines() {
    if (!lines.length) return 'Cần ít nhất 1 dòng thuốc'
    if (lines.some((l) => !l.productId || l.quantity <= 0)) return 'Mỗi dòng cần thuốc và số lượng > 0'
    const ids = lines.map((l) => l.productId)
    if (new Set(ids).size !== ids.length) return 'Không được trùng thuốc trên cùng phiếu — gộp số lượng lại'
    if (kind === 'import' && costs.tradeDiscount > lineTotal)
      return 'Chiết khấu thương mại không được lớn hơn tổng giá mua hàng'
    return ''
  }

  async function saveReceipt(e?: FormEvent) {
    e?.preventDefault()
    const hErr = validateHeader()
    const lErr = validateLines()
    if (hErr || lErr) {
      const msg = hErr || lErr
      setError(msg)
      toast.error(msg)
      return
    }
    setError('')
    try {
      const receiptDate = fromDateTimeLocal(header.receiptDate)
      const details = lines.map((l) => ({
        productId: l.productId,
        quantity: Number(l.quantity),
        unitPrice: Number(l.unitPrice),
        note: l.note || null,
      }))
      const body =
        kind === 'import'
          ? {
              warehouseId: header.warehouseId,
              supplierId: header.partnerId,
              receiptDate,
              note: header.note || null,
              shippingFee: Number(costs.shippingFee) || 0,
              tradeDiscount: Number(costs.tradeDiscount) || 0,
              nonRefundableTax: Number(costs.nonRefundableTax) || 0,
              details,
            }
          : {
              warehouseId: header.warehouseId,
              customerId: header.partnerId,
              receiptDate,
              note: header.note || null,
              // 0 = Manual (số) — tương thích API; backend cũng ép Manual cho admin
              source: 0,
              details,
            }

      if (editingId) {
        await api.update(editingId, body)
        toast.success('Đã cập nhật phiếu nháp')
      } else {
        await api.create(body)
        toast.success('Đã tạo phiếu nháp (Draft) — bước tiếp: Xác nhận để cập nhật tồn')
      }
      setOpen(false)
      setEditingId(null)
      await reload()
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Lưu phiếu thất bại'
      setError(msg)
      toast.error(msg)
    }
  }

  async function runAct() {
    if (!confirmAct) return
    const { id, action } = confirmAct
    try {
      if (action === 'confirm') await api.confirm(id)
      if (action === 'cancel') await api.cancel(id)
      if (action === 'remove') await api.remove(id)
      toast.success(
        action === 'confirm'
          ? kind === 'import'
            ? 'Đã xác nhận nhập — tồn đã cộng, hóa đơn mua hàng (HDN) đã phát hành'
            : 'Đã xác nhận xuất — tồn kho đã trừ'
          : action === 'cancel'
            ? 'Đã hủy phiếu (hoàn tồn nếu đã xác nhận)'
            : 'Đã xóa phiếu nháp',
      )
      setConfirmAct(null)
      setDetail(null)
      await reload()
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Thao tác thất bại'
      setError(msg)
      toast.error(msg)
    }
  }

  return (
    <Box>
      <PageHeader
        title={kind === 'import' ? 'Nhập kho dược' : 'Xuất kho dược'}
        subtitle={
          kind === 'import'
            ? 'Quy trình: Tạo nháp (nhiều thuốc) → Kiểm tra → Xác nhận cộng tồn → In phiếu'
            : 'Quy trình: Tạo nháp (nhiều thuốc) → Kiểm tra tồn → Xác nhận trừ tồn → In phiếu'
        }
        icon={<KindIcon color={kind === 'import' ? 'success' : 'primary'} fontSize="small" />}
        actions={
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={openCreate}
            sx={{
              fontWeight: 700,
              borderRadius: 2,
              px: 2,
              bgcolor: kind === 'import' ? '#0B6E4F' : undefined,
              '&:hover': { bgcolor: kind === 'import' ? '#095c42' : undefined },
            }}
          >
            Tạo phiếu mới
          </Button>
        }
      />

      <Paper
        className="mb-3 p-3"
        sx={{
          borderRadius: 3,
          border: '1px solid',
          borderColor: kind === 'import' ? alpha('#0B6E4F', 0.18) : alpha('#1565C0', 0.18),
          background:
            kind === 'import'
              ? `linear-gradient(135deg, ${alpha('#0B6E4F', 0.06)}, ${alpha('#D4E157', 0.12)})`
              : `linear-gradient(135deg, ${alpha('#1565C0', 0.06)}, ${alpha('#42A5F5', 0.1)})`,
        }}
      >
        <Stack direction="row" spacing={1.5} alignItems="flex-start">
          {kind === 'import' ? (
            <LocalShippingOutlinedIcon sx={{ color: '#0B6E4F', mt: 0.2 }} />
          ) : (
            <Inventory2OutlinedIcon sx={{ color: '#1565C0', mt: 0.2 }} />
          )}
          <Typography variant="body2" color="text.secondary" sx={{ lineHeight: 1.55 }}>
            {kind === 'import' ? (
              <>
                <strong>Nhập kho:</strong> chọn NCC + kho → nhập <em>giá mua</em>, chi phí vận chuyển, chiết khấu thương mại,
                thuế không hoàn lại → lưu <em>Draft</em> → <em>Xác nhận</em> (cộng tồn + phát hành hóa đơn HDN).
              </>
            ) : (
              <>
                <strong>Xuất kho:</strong> chọn khoa/phòng + kho xuất → thêm nhiều dòng thuốc → lưu{' '}
                <em>Draft</em> → <em>Xác nhận</em> để trừ tồn (đủ hàng). In báo cáo sau khi tạo/xác nhận.
              </>
            )}
          </Typography>
        </Stack>
      </Paper>

      <Paper className="p-3" sx={{ borderRadius: 2 }}>
        {error && !open ? <Alert>{error}</Alert> : null}
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} className="mb-2 items-center flex-wrap">
          <TextField
            size="small"
            placeholder={kind === 'export' ? 'Tìm mã PX / HD / khoa...' : 'Tìm mã PN / NCC...'}
            value={search}
            onChange={(e) => {
              setPage(1)
              setSearch(e.target.value)
            }}
            className="sm:max-w-xs"
            fullWidth
          />
          <Stack direction="row" spacing={0.5} className="flex-wrap">
            {(['all', 'Draft', 'Confirmed', 'Cancelled'] as const).map((key) => (
              <Chip
                key={key}
                size="small"
                label={key === 'all' ? 'Tất cả TT' : key === 'Draft' ? 'Nháp' : key === 'Confirmed' ? 'Đã XN' : 'Đã hủy'}
                color={statusFilter === key ? 'primary' : 'default'}
                variant={statusFilter === key ? 'filled' : 'outlined'}
                onClick={() => {
                  setPage(1)
                  setStatusFilter(key)
                }}
              />
            ))}
          </Stack>
          {kind === 'export' ? (
            <Stack direction="row" spacing={0.5}>
              {(
                [
                  ['all', 'Tất cả nguồn'],
                  ['store', 'Nhà thuốc BV'],
                  ['manual', 'Xuất tay'],
                ] as const
              ).map(([key, label]) => (
                <Chip
                  key={key}
                  size="small"
                  label={label}
                  color={sourceFilter === key ? 'secondary' : 'default'}
                  variant={sourceFilter === key ? 'filled' : 'outlined'}
                  onClick={() => {
                    setPage(1)
                    setSourceFilter(key)
                  }}
                />
              ))}
            </Stack>
          ) : null}
          <IconButton size="small" onClick={() => void reload()} aria-label="Làm mới">
            <RefreshIcon fontSize="small" />
          </IconButton>
        </Stack>

        <TableContainer>
          <Table size="small" sx={{ '& td, & th': { py: 0.75, px: 1, fontSize: 13 } }}>
            <TableHead>
              <TableRow>
                <TableCell>Mã</TableCell>
                <TableCell>Thời gian</TableCell>
                <TableCell>Kho</TableCell>
                <TableCell>{kind === 'import' ? 'NCC' : 'Khoa/phòng'}</TableCell>
                <TableCell>TT</TableCell>
                <TableCell align="right">SL</TableCell>
                <TableCell align="right">Tiền</TableCell>
                <TableCell align="right" width={160}>
                  Thao tác
                </TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((r) => {
                const store = kind === 'export' && isStoreExport(r)
                const ex = r as ExportReceiptDto
                return (
                  <TableRow
                    key={r.id}
                    hover
                    selected={detail?.id === r.id}
                    sx={{ cursor: 'pointer', bgcolor: store ? 'rgba(0,137,123,0.04)' : undefined }}
                    onClick={async () => setDetail(await api.get(r.id))}
                  >
                    <TableCell>
                      <Stack direction="row" spacing={0.5} className="items-center flex-wrap">
                        <Typography sx={{ fontWeight: 700, fontSize: 13 }}>{r.code}</Typography>
                        {store ? (
                          <Chip size="small" color="success" label="Online" sx={{ height: 20, fontSize: 10 }} />
                        ) : null}
                      </Stack>
                      {kind === 'import' && (r as ImportReceiptDto).purchaseInvoiceCode ? (
                        <Typography variant="caption" color="primary" sx={{ display: 'block', fontWeight: 600 }}>
                          {(r as ImportReceiptDto).purchaseInvoiceCode}
                        </Typography>
                      ) : null}
                      {ex.invoiceCode ? (
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block' }}>
                          {ex.invoiceCode}
                        </Typography>
                      ) : null}
                    </TableCell>
                    <TableCell sx={{ whiteSpace: 'nowrap' }}>
                      <Typography variant="body2" sx={{ fontWeight: 600, fontSize: 12 }}>
                        {formatDateTime(r.receiptDate)}
                      </Typography>
                    </TableCell>
                    <TableCell>{r.warehouseCode}</TableCell>
                    <TableCell>
                      <Typography noWrap sx={{ maxWidth: 160, fontSize: 13 }}>
                        {kind === 'import'
                          ? (r as ImportReceiptDto).supplierName
                          : (r as ExportReceiptDto).customerName}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Badge tone={statusTone(r.status)}>{r.status}</Badge>
                    </TableCell>
                    <TableCell align="right">{r.totalQuantity}</TableCell>
                    <TableCell align="right" sx={{ fontWeight: 600, whiteSpace: 'nowrap' }}>
                      {money(r.totalAmount)}
                    </TableCell>
                    <TableCell align="right" onClick={(e) => e.stopPropagation()}>
                      <Stack direction="row" spacing={0} className="justify-end">
                        <Tooltip title="Chi tiết / In">
                          <IconButton size="small" onClick={async () => setDetail(await api.get(r.id))}>
                            <VisibilityOutlinedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        {r.status === 'Draft' ? (
                          <>
                            <Tooltip title="Sửa nháp">
                              <IconButton size="small" onClick={() => void openEdit(r.id)}>
                                <EditOutlinedIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Xác nhận (cập nhật tồn)">
                              <IconButton
                                size="small"
                                color="success"
                                onClick={() => setConfirmAct({ id: r.id, action: 'confirm' })}
                              >
                                <CheckCircleOutlineIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Hủy">
                              <IconButton size="small" onClick={() => setConfirmAct({ id: r.id, action: 'cancel' })}>
                                <CancelOutlinedIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Xóa nháp">
                              <IconButton
                                size="small"
                                color="error"
                                onClick={() => setConfirmAct({ id: r.id, action: 'remove' })}
                              >
                                <DeleteIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          </>
                        ) : r.status === 'Confirmed' ? (
                          <>
                            {kind === 'import' ? (
                              <Tooltip title="In hóa đơn VN">
                                <IconButton
                                  size="small"
                                  color="primary"
                                  onClick={async () => {
                                    const shell = openPrintShell({ width: 960, height: 720 })
                                    try {
                                      const full = (await api.get(r.id)) as ImportReceiptDto
                                      printPurchaseInvoiceVn(full, shell)
                                    } catch (err) {
                                      try {
                                        shell?.close()
                                      } catch {
                                        /* ignore */
                                      }
                                      const msg =
                                        err instanceof ApiError ? err.message : 'Không tải được hóa đơn để in'
                                      setError(msg)
                                      toast.error(msg)
                                    }
                                  }}
                                >
                                  <ReceiptLongIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            ) : null}
                            <Tooltip title="In phiếu kho">
                              <IconButton
                                size="small"
                                onClick={() =>
                                  void printReceiptAfterFetch(kind, r.id, api, (msg) => {
                                    setError(msg)
                                    toast.error(msg)
                                  })
                                }
                              >
                                <PrintIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                            <Tooltip title="Hủy (hoàn tồn)">
                              <IconButton size="small" onClick={() => setConfirmAct({ id: r.id, action: 'cancel' })}>
                                <CancelOutlinedIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          </>
                        ) : (
                          <Tooltip title="In phiếu">
                            <IconButton
                              size="small"
                              onClick={() =>
                                void printReceiptAfterFetch(kind, r.id, api, (msg) => {
                                  setError(msg)
                                  toast.error(msg)
                                })
                              }
                            >
                              <PrintIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        )}
                      </Stack>
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </TableContainer>
        {!rows.length && !error ? (
          <Typography className="py-8 text-center" color="text.secondary" variant="body2">
            Không có phiếu
          </Typography>
        ) : null}
        <Pager page={page} totalPages={totalPages} onChange={setPage} />
      </Paper>

      {open ? (
        <Modal
          title={
            editingId
              ? `Sửa phiếu nháp #${editingId}`
              : kind === 'import'
                ? 'Tạo phiếu nhập kho'
                : 'Tạo phiếu xuất kho'
          }
          subtitle={
            kind === 'import'
              ? 'Nhập thuốc / vật tư từ NCC · lưu nháp trước khi xác nhận cộng tồn'
              : 'Xuất thuốc / vật tư cho khoa · lưu nháp trước khi xác nhận trừ tồn'
          }
          icon={<KindIcon />}
          accent={kind === 'import' ? 'success' : 'primary'}
          onClose={() => {
            setOpen(false)
            setEditingId(null)
            setPrefHint('')
          }}
          maxWidth="lg"
          actions={
            <>
              <Button
                startIcon={<ArrowBackIcon />}
                variant="outlined"
                disabled={wizardStep === 0}
                onClick={() => setWizardStep((s) => Math.max(0, s - 1))}
                sx={{ borderRadius: 2, mr: 'auto' }}
              >
                Quay lại
              </Button>
              <Button
                variant="text"
                onClick={() => {
                  setOpen(false)
                  setEditingId(null)
                  setPrefHint('')
                }}
                sx={{ borderRadius: 2 }}
              >
                Đóng
              </Button>
              {wizardStep < 2 ? (
                <Button
                  variant="contained"
                  endIcon={<ArrowForwardIcon />}
                  onClick={() => {
                    if (wizardStep === 0) {
                      const msg = validateHeader()
                      if (msg) {
                        setError(msg)
                        toast.error(msg)
                        return
                      }
                    }
                    if (wizardStep === 1) {
                      const msg = validateLines()
                      if (msg) {
                        setError(msg)
                        toast.error(msg)
                        return
                      }
                    }
                    setError('')
                    setWizardStep((s) => s + 1)
                  }}
                  sx={{
                    borderRadius: 2,
                    fontWeight: 700,
                    px: 2.5,
                    bgcolor: kind === 'import' ? '#0B6E4F' : undefined,
                    '&:hover': { bgcolor: kind === 'import' ? '#095c42' : undefined },
                  }}
                >
                  Tiếp tục
                </Button>
              ) : (
                <Button
                  variant="contained"
                  startIcon={<SaveOutlinedIcon />}
                  onClick={() => void saveReceipt()}
                  sx={{
                    borderRadius: 2,
                    fontWeight: 700,
                    px: 2.5,
                    bgcolor: kind === 'import' ? '#0B6E4F' : undefined,
                    '&:hover': { bgcolor: kind === 'import' ? '#095c42' : undefined },
                  }}
                >
                  {editingId ? 'Cập nhật nháp' : 'Lưu phiếu nháp'}
                </Button>
              )}
            </>
          }
        >
          {(() => {
            const accent = kind === 'import' ? '#0B6E4F' : '#1565C0'
            const stepIcons = [
              <WarehouseOutlinedIcon key="s0" />,
              <Inventory2OutlinedIcon key="s1" />,
              <FactCheckOutlinedIcon key="s2" />,
            ]
            return (
              <>
                <Paper
                  elevation={0}
                  sx={{
                    mb: 2.5,
                    p: 2,
                    borderRadius: 3,
                    bgcolor: '#fff',
                    border: '1px solid',
                    borderColor: 'divider',
                  }}
                >
                  <Stepper activeStep={wizardStep} alternativeLabel connector={<WizardConnector />}>
                    {steps.map((label, idx) => (
                      <Step key={label} completed={wizardStep > idx}>
                        <StepLabel
                          StepIconComponent={() => (
                            <WizardStepIcon
                              active={wizardStep === idx}
                              completed={wizardStep > idx}
                              icon={stepIcons[idx]}
                              accent={accent}
                            />
                          )}
                        >
                          <Typography
                            variant="caption"
                            sx={{
                              fontWeight: wizardStep === idx ? 800 : 600,
                              color: wizardStep === idx ? accent : 'text.secondary',
                            }}
                          >
                            {label}
                          </Typography>
                        </StepLabel>
                      </Step>
                    ))}
                  </Stepper>
                </Paper>

                {error ? <Alert>{error}</Alert> : null}
                {prefHint ? <Alert severity="warning">{prefHint}</Alert> : null}

                {wizardStep === 0 ? (
                  <Stack spacing={2}>
                    <Paper
                      elevation={0}
                      sx={{ p: 2.25, borderRadius: 3, border: '1px solid', borderColor: 'divider', bgcolor: '#fff' }}
                    >
                      <Stack direction="row" spacing={1} alignItems="center" className="mb-2">
                        <WarehouseOutlinedIcon sx={{ color: accent, fontSize: 20 }} />
                        <Typography sx={{ fontWeight: 800 }}>Thông tin chung</Typography>
                      </Stack>
                      <Box className="grid grid-cols-1 gap-2.5 sm:grid-cols-2">
                        <TextField
                          select
                          label="Kho"
                          size="small"
                          value={header.warehouseId}
                          onChange={(e) => setHeader({ ...header, warehouseId: Number(e.target.value) })}
                        >
                          {warehouses.map((w) => (
                            <MenuItem key={w.id} value={w.id}>
                              {w.code} - {w.name}
                            </MenuItem>
                          ))}
                        </TextField>
                        <TextField
                          select
                          label={kind === 'import' ? 'Nhà cung cấp dược' : 'Khoa / phòng nhận'}
                          size="small"
                          value={header.partnerId}
                          onChange={(e) => setHeader({ ...header, partnerId: Number(e.target.value) })}
                        >
                          {partners.map((p) => (
                            <MenuItem key={p.id} value={p.id}>
                              {p.name}
                            </MenuItem>
                          ))}
                        </TextField>
                        <TextField
                          label="Thời gian phiếu"
                          type="datetime-local"
                          size="small"
                          value={header.receiptDate}
                          onChange={(e) => setHeader({ ...header, receiptDate: e.target.value })}
                          slotProps={{ htmlInput: { step: 1 } }}
                          className="sm:col-span-2"
                        />
                        <TextField
                          label="Ghi chú"
                          size="small"
                          value={header.note}
                          onChange={(e) => setHeader({ ...header, note: e.target.value })}
                          className="sm:col-span-2"
                          multiline
                          minRows={2}
                          placeholder={
                            kind === 'import' ? 'Số hóa đơn NCC, số lô...' : 'Phiếu lĩnh khoa, lý do xuất...'
                          }
                        />
                      </Box>
                    </Paper>

                    {kind === 'import' ? (
                      <Paper
                        elevation={0}
                        sx={{
                          p: 2.25,
                          borderRadius: 3,
                          border: '1px solid',
                          borderColor: alpha(accent, 0.2),
                          bgcolor: alpha(accent, 0.04),
                        }}
                      >
                        <Stack direction="row" spacing={1} alignItems="center" className="mb-2">
                          <LocalShippingOutlinedIcon sx={{ color: accent, fontSize: 20 }} />
                          <Typography sx={{ fontWeight: 800 }}>Chi phí mua hàng</Typography>
                        </Stack>
                        <Box className="grid grid-cols-1 gap-2.5 sm:grid-cols-3">
                          <TextField
                            label="Chi phí vận chuyển"
                            type="number"
                            size="small"
                            value={costs.shippingFee}
                            onChange={(e) => setCosts({ ...costs, shippingFee: Number(e.target.value) })}
                            slotProps={{ htmlInput: { min: 0, step: 1000 } }}
                            helperText="Cộng vào tổng thanh toán"
                          />
                          <TextField
                            label="Chiết khấu thương mại"
                            type="number"
                            size="small"
                            value={costs.tradeDiscount}
                            onChange={(e) => setCosts({ ...costs, tradeDiscount: Number(e.target.value) })}
                            slotProps={{ htmlInput: { min: 0, step: 1000 } }}
                            helperText="Trừ khỏi giá mua hàng"
                          />
                          <TextField
                            label="Thuế không được hoàn lại"
                            type="number"
                            size="small"
                            value={costs.nonRefundableTax}
                            onChange={(e) => setCosts({ ...costs, nonRefundableTax: Number(e.target.value) })}
                            slotProps={{ htmlInput: { min: 0, step: 1000 } }}
                            helperText="Cộng vào tổng thanh toán"
                          />
                        </Box>
                      </Paper>
                    ) : null}
                  </Stack>
                ) : null}

                {wizardStep === 1 ? (
                  <Box>
                    <Stack
                      direction={{ xs: 'column', sm: 'row' }}
                      spacing={1}
                      className="mb-2 items-start sm:items-center justify-between"
                    >
                      <Box>
                        <Typography sx={{ fontWeight: 800 }}>Chi tiết thuốc / vật tư</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {lines.length} dòng · chọn đúng SKU trước khi tiếp tục
                        </Typography>
                      </Box>
                      <Button
                        variant="contained"
                        size="small"
                        startIcon={<AddIcon />}
                        onClick={() => setLines((prev) => [...prev, newLine(products[0])])}
                        sx={{
                          borderRadius: 2,
                          fontWeight: 700,
                          bgcolor: accent,
                          '&:hover': { bgcolor: kind === 'import' ? '#095c42' : '#0d47a1' },
                        }}
                      >
                        Thêm dòng
                      </Button>
                    </Stack>

                    <Stack spacing={1.25} sx={{ maxHeight: 380, overflowY: 'auto', pr: 0.5 }}>
                      {lines.map((line, idx) => {
                        const lineAmt = (Number(line.quantity) || 0) * (Number(line.unitPrice) || 0)
                        return (
                          <Paper
                            key={line.key}
                            elevation={0}
                            sx={{
                              p: 1.75,
                              borderRadius: 2.5,
                              border: '1px solid',
                              borderColor: 'divider',
                              bgcolor: '#fff',
                              transition: 'box-shadow .15s ease, border-color .15s ease',
                              '&:hover': {
                                borderColor: alpha(accent, 0.35),
                                boxShadow: `0 8px 22px ${alpha(accent, 0.08)}`,
                              },
                            }}
                          >
                            <Stack
                              direction="row"
                              className="mb-1.5 items-center justify-between"
                            >
                              <Chip
                                size="small"
                                label={`Dòng ${idx + 1}`}
                                sx={{
                                  fontWeight: 700,
                                  bgcolor: alpha(accent, 0.1),
                                  color: accent,
                                }}
                              />
                              <Tooltip title={lines.length <= 1 ? 'Cần ít nhất 1 dòng' : 'Xóa dòng'}>
                                <span>
                                  <IconButton
                                    size="small"
                                    color="error"
                                    disabled={lines.length <= 1}
                                    onClick={() => setLines((prev) => prev.filter((_, i) => i !== idx))}
                                  >
                                    <DeleteOutlinedIcon fontSize="small" />
                                  </IconButton>
                                </span>
                              </Tooltip>
                            </Stack>
                            <Box
                              className="grid gap-2"
                              sx={{
                                gridTemplateColumns: {
                                  xs: '1fr',
                                  md: 'minmax(0,1.6fr) 110px 140px 120px',
                                },
                                alignItems: 'center',
                              }}
                            >
                              <TextField
                                select
                                label="Thuốc / vật tư"
                                size="small"
                                fullWidth
                                value={line.productId}
                                onChange={(e) => {
                                  const id = Number(e.target.value)
                                  const p = products.find((x) => x.id === id)
                                  setLines((prev) =>
                                    prev.map((l, i) =>
                                      i === idx
                                        ? { ...l, productId: id, unitPrice: p?.price || l.unitPrice }
                                        : l,
                                    ),
                                  )
                                }}
                              >
                                {products.map((p) => (
                                  <MenuItem key={p.id} value={p.id}>
                                    {p.sku} · {p.name}
                                  </MenuItem>
                                ))}
                              </TextField>
                              <TextField
                                label="SL"
                                type="number"
                                size="small"
                                fullWidth
                                slotProps={{ htmlInput: { min: 0.01, step: 0.01 } }}
                                value={line.quantity}
                                onChange={(e) =>
                                  setLines((prev) =>
                                    prev.map((l, i) =>
                                      i === idx ? { ...l, quantity: Number(e.target.value) } : l,
                                    ),
                                  )
                                }
                              />
                              <TextField
                                label={kind === 'import' ? 'Giá mua' : 'Đơn giá'}
                                type="number"
                                size="small"
                                fullWidth
                                value={line.unitPrice}
                                onChange={(e) =>
                                  setLines((prev) =>
                                    prev.map((l, i) =>
                                      i === idx ? { ...l, unitPrice: Number(e.target.value) } : l,
                                    ),
                                  )
                                }
                              />
                              <Box
                                sx={{
                                  px: 1.25,
                                  py: 1,
                                  borderRadius: 2,
                                  bgcolor: alpha(accent, 0.08),
                                  textAlign: { xs: 'left', md: 'right' },
                                }}
                              >
                                <Typography variant="caption" color="text.secondary" display="block">
                                  Thành tiền
                                </Typography>
                                <Typography sx={{ fontWeight: 800, color: accent, whiteSpace: 'nowrap' }}>
                                  {money(lineAmt)}
                                </Typography>
                              </Box>
                            </Box>
                          </Paper>
                        )
                      })}
                    </Stack>

                    <Paper
                      elevation={0}
                      sx={{
                        mt: 2,
                        p: 1.75,
                        borderRadius: 2.5,
                        background: `linear-gradient(135deg, ${alpha(accent, 0.12)}, ${alpha(accent, 0.04)})`,
                        border: '1px solid',
                        borderColor: alpha(accent, 0.2),
                      }}
                    >
                      <Stack
                        direction={{ xs: 'column', sm: 'row' }}
                        spacing={1.5}
                        justifyContent="space-between"
                        alignItems={{ sm: 'center' }}
                      >
                        <Typography variant="body2" sx={{ fontWeight: 700 }}>
                          Tổng SL: <Box component="span" sx={{ color: accent }}>{qtyTotal}</Box>
                          {' · '}
                          {kind === 'import' ? 'Giá mua' : 'Giá trị'}:{' '}
                          <Box component="span" sx={{ color: accent }}>{money(lineTotal)}</Box>
                        </Typography>
                        {kind === 'import' ? (
                          <Chip
                            label={`Thanh toán ${money(payableTotal)}`}
                            sx={{ fontWeight: 800, bgcolor: accent, color: '#fff' }}
                          />
                        ) : null}
                      </Stack>
                    </Paper>
                  </Box>
                ) : null}

                {wizardStep === 2 ? (
                  <Stack spacing={2}>
                    <Paper
                      elevation={0}
                      sx={{ p: 2.25, borderRadius: 3, border: '1px solid', borderColor: 'divider', bgcolor: '#fff' }}
                    >
                      <Stack direction="row" spacing={1} alignItems="center" className="mb-1.5">
                        <FactCheckOutlinedIcon sx={{ color: accent, fontSize: 20 }} />
                        <Typography sx={{ fontWeight: 800 }}>Kiểm tra trước khi lưu nháp</Typography>
                      </Stack>
                      <Box className="grid grid-cols-1 gap-2 sm:grid-cols-2">
                        {[
                          {
                            label: 'Kho',
                            value:
                              warehouses.find((w) => w.id === header.warehouseId)?.name ||
                              String(header.warehouseId),
                          },
                          {
                            label: kind === 'import' ? 'NCC' : 'Khoa/phòng',
                            value:
                              partners.find((p) => p.id === header.partnerId)?.name ||
                              String(header.partnerId),
                          },
                          { label: 'Thời gian', value: header.receiptDate },
                          ...(header.note ? [{ label: 'Ghi chú', value: header.note }] : []),
                        ].map((item) => (
                          <Box
                            key={item.label}
                            sx={{
                              p: 1.25,
                              borderRadius: 2,
                              bgcolor: alpha('#0F172A', 0.03),
                              border: '1px solid',
                              borderColor: 'divider',
                            }}
                          >
                            <Typography variant="caption" color="text.secondary">
                              {item.label}
                            </Typography>
                            <Typography variant="body2" sx={{ fontWeight: 700 }}>
                              {item.value}
                            </Typography>
                          </Box>
                        ))}
                      </Box>

                      {kind === 'import' ? (
                        <Box
                          className="mt-2 grid grid-cols-2 gap-2 sm:grid-cols-4"
                          sx={{
                            p: 1.5,
                            borderRadius: 2,
                            bgcolor: alpha(accent, 0.05),
                            border: '1px dashed',
                            borderColor: alpha(accent, 0.3),
                          }}
                        >
                          <Box>
                            <Typography variant="caption" color="text.secondary">
                              Giá mua
                            </Typography>
                            <Typography sx={{ fontWeight: 700 }}>{money(lineTotal)}</Typography>
                          </Box>
                          <Box>
                            <Typography variant="caption" color="text.secondary">
                              Chiết khấu
                            </Typography>
                            <Typography sx={{ fontWeight: 700 }}>− {money(costs.tradeDiscount)}</Typography>
                          </Box>
                          <Box>
                            <Typography variant="caption" color="text.secondary">
                              Vận chuyển + thuế
                            </Typography>
                            <Typography sx={{ fontWeight: 700 }}>
                              {money(costs.shippingFee + costs.nonRefundableTax)}
                            </Typography>
                          </Box>
                          <Box>
                            <Typography variant="caption" color="text.secondary">
                              Thanh toán
                            </Typography>
                            <Typography sx={{ fontWeight: 800, color: accent }}>
                              {money(payableTotal)}
                            </Typography>
                          </Box>
                        </Box>
                      ) : null}
                    </Paper>

                    <Paper
                      elevation={0}
                      sx={{ borderRadius: 3, border: '1px solid', borderColor: 'divider', overflow: 'hidden' }}
                    >
                      <Box
                        sx={{
                          px: 2,
                          py: 1.25,
                          bgcolor: alpha(accent, 0.06),
                          borderBottom: '1px solid',
                          borderColor: 'divider',
                        }}
                      >
                        <Typography sx={{ fontWeight: 800, fontSize: 14 }}>
                          Danh sách dòng ({lines.length})
                        </Typography>
                      </Box>
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>SKU</TableCell>
                              <TableCell>Tên</TableCell>
                              <TableCell align="right">SL</TableCell>
                              <TableCell align="right">Đơn giá</TableCell>
                              <TableCell align="right">TT</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {lines.map((l) => {
                              const p = products.find((x) => x.id === l.productId)
                              return (
                                <TableRow key={l.key} hover>
                                  <TableCell>
                                    <Chip size="small" label={p?.sku || '—'} variant="outlined" />
                                  </TableCell>
                                  <TableCell sx={{ fontWeight: 600 }}>{p?.name}</TableCell>
                                  <TableCell align="right">{l.quantity}</TableCell>
                                  <TableCell align="right">{money(l.unitPrice)}</TableCell>
                                  <TableCell align="right" sx={{ fontWeight: 700, color: accent }}>
                                    {money((Number(l.quantity) || 0) * (Number(l.unitPrice) || 0))}
                                  </TableCell>
                                </TableRow>
                              )
                            })}
                          </TableBody>
                        </Table>
                      </TableContainer>
                      <Divider />
                      <Box sx={{ px: 2, py: 1.5, bgcolor: '#fff' }}>
                        <Typography variant="body2" sx={{ fontWeight: 800 }}>
                          Tổng {lines.length} dòng · SL {qtyTotal} ·{' '}
                          {kind === 'import' ? `Thanh toán ${money(payableTotal)}` : money(lineTotal)}
                        </Typography>
                        <Typography variant="caption" color="text.secondary" display="block" className="mt-0.5">
                          Sau khi lưu nháp: bấm <strong>Xác nhận</strong> trên danh sách để{' '}
                          {kind === 'import'
                            ? 'cộng tồn kho và phát hành hóa đơn mua hàng (HDN)'
                            : 'trừ tồn kho'}
                          .
                        </Typography>
                      </Box>
                    </Paper>
                  </Stack>
                ) : null}
              </>
            )
          })()}
        </Modal>
      ) : null}

      {detail ? (
        <Modal
          title={detail.code}
          icon={<VisibilityOutlinedIcon color="primary" />}
          onClose={() => setDetail(null)}
          maxWidth="md"
          actions={
            <>
              {kind === 'import' ? (
                <>
                  <Button
                    size="small"
                    variant="outlined"
                    startIcon={<ReceiptLongIcon />}
                    onClick={() => printPurchaseInvoiceVn(detail as ImportReceiptDto)}
                  >
                    In hóa đơn VN
                  </Button>
                  <Button
                    size="small"
                    variant="outlined"
                    startIcon={<FileDownloadOutlinedIcon />}
                    onClick={() => exportPurchaseInvoiceCsv(detail as ImportReceiptDto)}
                  >
                    Export CSV
                  </Button>
                </>
              ) : null}
              <Button
                size="small"
                variant="outlined"
                startIcon={<PrintIcon />}
                onClick={() => printReceiptReport(kind, detail)}
              >
                In phiếu kho
              </Button>
              <Button size="small" variant="outlined" onClick={() => setDetail(null)}>
                Đóng
              </Button>
              {detail.status === 'Draft' ? (
                <>
                  <Button size="small" startIcon={<EditOutlinedIcon />} onClick={() => void openEdit(detail.id)}>
                    Sửa
                  </Button>
                  <Button
                    size="small"
                    startIcon={<CheckCircleOutlineIcon />}
                    onClick={() => setConfirmAct({ id: detail.id, action: 'confirm' })}
                  >
                    Xác nhận
                  </Button>
                  <Button
                    size="small"
                    variant="outlined"
                    onClick={() => setConfirmAct({ id: detail.id, action: 'cancel' })}
                  >
                    Hủy
                  </Button>
                  <Button
                    size="small"
                    color="error"
                    onClick={() => setConfirmAct({ id: detail.id, action: 'remove' })}
                  >
                    Xóa
                  </Button>
                </>
              ) : detail.status === 'Confirmed' ? (
                <Button
                  size="small"
                  variant="outlined"
                  onClick={() => setConfirmAct({ id: detail.id, action: 'cancel' })}
                >
                  Hủy phiếu (hoàn tồn)
                </Button>
              ) : null}
            </>
          }
        >
          <Stack spacing={1} className="mb-2">
            <Stack direction="row" spacing={1} className="flex-wrap items-center">
              <Badge tone={statusTone(detail.status)}>{detail.status}</Badge>
              {kind === 'export' && isStoreExport(detail) ? (
                <Chip size="small" color="success" label="Đơn nhà thuốc BV" />
              ) : null}
            </Stack>
            <Typography variant="body2">
              <strong>Thời gian:</strong> {formatDateTime(detail.receiptDate)}
            </Typography>
            <Typography variant="body2">
              <strong>Kho:</strong> {detail.warehouseCode} — {detail.warehouseName}
            </Typography>
            <Typography variant="body2">
              <strong>{kind === 'import' ? 'NCC' : 'Khoa/phòng'}:</strong>{' '}
              {kind === 'import'
                ? (detail as ImportReceiptDto).supplierName
                : (detail as ExportReceiptDto).customerName}
            </Typography>
            {kind === 'import' && (detail as ImportReceiptDto).purchaseInvoiceCode ? (
              <Typography variant="body2" color="primary" sx={{ fontWeight: 700 }}>
                Hóa đơn: {(detail as ImportReceiptDto).purchaseInvoiceCode}
              </Typography>
            ) : null}
            <Typography variant="body2">
              <strong>Tổng thanh toán:</strong> {money(detail.totalAmount)} · {detail.totalQuantity} ·{' '}
              {detail.details.length} dòng
            </Typography>
            {kind === 'import' ? (
              <Box className="rounded border border-slate-200 bg-slate-50 p-2 text-sm">
                <div>Giá mua hàng: {money((detail as ImportReceiptDto).goodsAmount ?? 0)}</div>
                <div>Chiết khấu thương mại: − {money((detail as ImportReceiptDto).tradeDiscount ?? 0)}</div>
                <div>Chi phí vận chuyển: {money((detail as ImportReceiptDto).shippingFee ?? 0)}</div>
                <div>Thuế không được hoàn lại: {money((detail as ImportReceiptDto).nonRefundableTax ?? 0)}</div>
              </Box>
            ) : null}
            {detail.confirmedAt ? (
              <Typography variant="body2" color="text.secondary">
                Xác nhận: {formatDateTime(detail.confirmedAt)}
              </Typography>
            ) : null}
            {detail.note ? (
              <Typography variant="body2" color="text.secondary">
                {detail.note}
              </Typography>
            ) : null}
          </Stack>
          <TableContainer>
            <Table size="small" sx={{ '& td, & th': { py: 0.5, px: 1, fontSize: 12 } }}>
              <TableHead>
                <TableRow>
                  <TableCell>SKU</TableCell>
                  <TableCell>Tên</TableCell>
                  <TableCell align="right">SL</TableCell>
                  <TableCell>ĐVT</TableCell>
                  <TableCell align="right">{kind === 'import' ? 'Giá mua' : 'Đơn giá'}</TableCell>
                  <TableCell align="right">TT</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {detail.details.map((d) => (
                  <TableRow key={d.id}>
                    <TableCell>{d.productSku}</TableCell>
                    <TableCell>{d.productName}</TableCell>
                    <TableCell align="right">{d.quantity}</TableCell>
                    <TableCell>{d.unitSymbol}</TableCell>
                    <TableCell align="right">{money(d.unitPrice)}</TableCell>
                    <TableCell align="right">{money(d.lineTotal)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </Modal>
      ) : null}

      <ConfirmDialog
        open={!!confirmAct}
        title={
          confirmAct?.action === 'confirm'
            ? 'Xác nhận phiếu'
            : confirmAct?.action === 'cancel'
              ? 'Hủy phiếu'
              : 'Xóa phiếu'
        }
        message={
          confirmAct?.action === 'confirm'
            ? kind === 'import'
              ? 'Xác nhận sẽ CỘNG tồn kho và phát hành hóa đơn mua hàng (HDN) theo mẫu VN. Tiếp tục?'
              : 'Xác nhận sẽ TRỪ tồn kho theo từng dòng thuốc (cần đủ tồn). Tiếp tục?'
            : confirmAct?.action === 'cancel'
              ? 'Hủy sẽ hoàn tác tồn nếu phiếu đã xác nhận. Tiếp tục?'
              : 'Xóa phiếu nháp? Không hoàn tác.'
        }
        confirmLabel={confirmAct?.action === 'remove' ? 'Xóa' : 'Đồng ý'}
        danger={confirmAct?.action === 'remove'}
        onClose={() => setConfirmAct(null)}
        onConfirm={() => void runAct()}
      />
    </Box>
  )
}
