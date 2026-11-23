import { useEffect, useState, type FormEvent } from 'react'
import {
  Box,
  Button,
  Chip,
  IconButton,
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
  Tooltip,
  Typography,
} from '@mui/material'
import InputIcon from '@mui/icons-material/Input'
import OutputIcon from '@mui/icons-material/Output'
import AddIcon from '@mui/icons-material/Add'
import RefreshIcon from '@mui/icons-material/Refresh'
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlineOutlined'
import CancelOutlinedIcon from '@mui/icons-material/CancelOutlined'
import DeleteIcon from '@mui/icons-material/Delete'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import { customersApi, exportsApi, importsApi, productsApi, suppliersApi, warehousesApi } from '../../api/endpoints'
import { ApiError } from '../../api/client'
import { useToast } from '../../components/ToastContext'
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

export function ImportsPage() {
  return <ReceiptsPage kind="import" />
}

export function ExportsPage() {
  return <ReceiptsPage kind="export" />
}

function isStoreExport(r: ImportReceiptDto | ExportReceiptDto) {
  if (!('source' in r) && !('note' in r)) return false
  const ex = r as ExportReceiptDto
  return ex.source === 'Store' || !!ex.note?.includes('FreshMart')
}

function ReceiptsPage({ kind }: { kind: Kind }) {
  const toast = useToast()
  const api = kind === 'import' ? importsApi : exportsApi
  const KindIcon = kind === 'import' ? InputIcon : OutputIcon
  const [rows, setRows] = useState<(ImportReceiptDto | ExportReceiptDto)[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [search, setSearch] = useState('')
  const [sourceFilter, setSourceFilter] = useState<'all' | 'store' | 'manual'>('all')
  const [error, setError] = useState('')
  const [open, setOpen] = useState(false)
  const [detail, setDetail] = useState<ImportReceiptDto | ExportReceiptDto | null>(null)
  const [confirmAct, setConfirmAct] = useState<{ id: number; action: 'confirm' | 'cancel' | 'remove' } | null>(null)
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([])
  const [partners, setPartners] = useState<{ id: number; name: string }[]>([])
  const [products, setProducts] = useState<ProductDto[]>([])
  const [form, setForm] = useState({
    warehouseId: 0,
    partnerId: 0,
    receiptDate: toDateTimeLocalValue(),
    note: '',
    productId: 0,
    quantity: 1,
    unitPrice: 0,
  })

  async function reload() {
    try {
      const q: Record<string, unknown> = { search, pageNumber: page, pageSize: 12 }
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
    void (async () => {
      try {
        const [w, p, pr] = await Promise.all([
          warehousesApi.list({ pageNumber: 1, pageSize: 100 }),
          kind === 'import'
            ? suppliersApi.list({ pageNumber: 1, pageSize: 100 })
            : customersApi.list({ pageNumber: 1, pageSize: 100 }),
          productsApi.list({ pageNumber: 1, pageSize: 100 }),
        ])
        setWarehouses(w.items)
        setPartners(p.items.map((x) => ({ id: x.id, name: x.name })))
        setProducts(pr.items)
        setForm((f) => ({
          ...f,
          warehouseId: w.items[0]?.id || 0,
          partnerId: p.items[0]?.id || 0,
          productId: pr.items[0]?.id || 0,
          unitPrice: pr.items[0]?.price || 0,
          receiptDate: f.receiptDate || toDateTimeLocalValue(),
        }))
        setError('')
      } catch (e) {
        setError(e instanceof ApiError ? e.message : 'Không tải được dữ liệu phụ trợ')
      }
    })()
  }, [kind])

  useEffect(() => {
    void reload()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, search, kind, sourceFilter])

  async function create(e: FormEvent) {
    e.preventDefault()
    setError('')
    try {
      const receiptDate = fromDateTimeLocal(form.receiptDate)
      const body =
        kind === 'import'
          ? {
              warehouseId: form.warehouseId,
              supplierId: form.partnerId,
              receiptDate,
              note: form.note || null,
              details: [{ productId: form.productId, quantity: form.quantity, unitPrice: form.unitPrice }],
            }
          : {
              warehouseId: form.warehouseId,
              customerId: form.partnerId,
              receiptDate,
              note: form.note || null,
              source: 'Manual',
              details: [{ productId: form.productId, quantity: form.quantity, unitPrice: form.unitPrice }],
            }
      await api.create(body)
      toast.success('Đã tạo phiếu Draft')
      setOpen(false)
      setForm((f) => ({ ...f, receiptDate: toDateTimeLocalValue(), note: '' }))
      await reload()
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Tạo phiếu thất bại'
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
        action === 'confirm' ? 'Đã xác nhận phiếu' : action === 'cancel' ? 'Đã hủy phiếu' : 'Đã xóa phiếu',
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
        title={kind === 'import' ? 'Nhập kho' : 'Xuất kho'}
        subtitle={
          kind === 'import'
            ? 'Phiếu nhập · mới nhất lên đầu'
            : 'Phiếu xuất (tay + đơn FreshMart) · mới nhất lên đầu'
        }
        icon={<KindIcon color="primary" fontSize="small" />}
        actions={
          <Button
            size="small"
            startIcon={<AddIcon />}
            onClick={() => {
              setForm((f) => ({ ...f, receiptDate: toDateTimeLocalValue() }))
              setOpen(true)
            }}
          >
            Tạo Draft
          </Button>
        }
      />
      <Paper className="p-3" sx={{ borderRadius: 2 }}>
        {error ? <Alert>{error}</Alert> : null}
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} className="mb-2 items-center">
          <TextField
            size="small"
            placeholder={kind === 'export' ? 'Tìm mã PX / HD / KH...' : 'Tìm mã phiếu...'}
            value={search}
            onChange={(e) => {
              setPage(1)
              setSearch(e.target.value)
            }}
            className="sm:max-w-xs"
            fullWidth
          />
          {kind === 'export' ? (
            <Stack direction="row" spacing={0.5}>
              {(
                [
                  ['all', 'Tất cả'],
                  ['store', 'KH mua'],
                  ['manual', 'Tạo tay'],
                ] as const
              ).map(([key, label]) => (
                <Chip
                  key={key}
                  size="small"
                  label={label}
                  color={sourceFilter === key ? 'primary' : 'default'}
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
                <TableCell>{kind === 'import' ? 'NCC' : 'KH'}</TableCell>
                <TableCell>TT</TableCell>
                <TableCell align="right">SL</TableCell>
                <TableCell align="right">Tiền</TableCell>
                <TableCell align="right" width={120}>
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
                    sx={{
                      cursor: 'pointer',
                      bgcolor: store ? 'rgba(0,137,123,0.04)' : undefined,
                    }}
                    onClick={async () => setDetail(await api.get(r.id))}
                  >
                    <TableCell>
                      <Stack direction="row" spacing={0.5} className="items-center flex-wrap">
                        <Typography sx={{ fontWeight: 700, fontSize: 13 }}>{r.code}</Typography>
                        {store ? <Chip size="small" color="success" label="Online" sx={{ height: 20, fontSize: 10 }} /> : null}
                      </Stack>
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
                      {r.confirmedAt ? (
                        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', fontSize: 10 }}>
                          XN {formatDateTime(r.confirmedAt)}
                        </Typography>
                      ) : null}
                    </TableCell>
                    <TableCell>{r.warehouseCode}</TableCell>
                    <TableCell>
                      <Typography noWrap sx={{ maxWidth: 140, fontSize: 13 }}>
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
                        <Tooltip title="Chi tiết">
                          <IconButton size="small" onClick={async () => setDetail(await api.get(r.id))}>
                            <VisibilityOutlinedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        {r.status === 'Draft' ? (
                          <>
                            <Tooltip title="Xác nhận">
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
                            <Tooltip title="Xóa">
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
                          <Tooltip title="Hủy (hoàn tồn)">
                            <IconButton size="small" onClick={() => setConfirmAct({ id: r.id, action: 'cancel' })}>
                              <CancelOutlinedIcon fontSize="small" />
                            </IconButton>
                          </Tooltip>
                        ) : null}
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
        <Modal title="Tạo phiếu Draft" icon={<KindIcon color="primary" />} onClose={() => setOpen(false)} maxWidth="sm">
          <Box component="form" onSubmit={create} className="grid grid-cols-1 gap-2 pt-1 sm:grid-cols-2">
            <TextField
              select
              label="Kho"
              size="small"
              value={form.warehouseId}
              onChange={(e) => setForm({ ...form, warehouseId: Number(e.target.value) })}
            >
              {warehouses.map((w) => (
                <MenuItem key={w.id} value={w.id}>
                  {w.code} - {w.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              select
              label={kind === 'import' ? 'Nhà cung cấp' : 'Khách hàng'}
              size="small"
              value={form.partnerId}
              onChange={(e) => setForm({ ...form, partnerId: Number(e.target.value) })}
            >
              {partners.map((p) => (
                <MenuItem key={p.id} value={p.id}>
                  {p.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label="Thời gian"
              type="datetime-local"
              size="small"
              value={form.receiptDate}
              onChange={(e) => setForm({ ...form, receiptDate: e.target.value })}
              slotProps={{ htmlInput: { step: 1 } }}
              className="sm:col-span-2"
            />
            <TextField
              select
              label="Sản phẩm"
              size="small"
              value={form.productId}
              onChange={(e) => {
                const id = Number(e.target.value)
                const p = products.find((x) => x.id === id)
                setForm({ ...form, productId: id, unitPrice: p?.price || 0 })
              }}
              className="sm:col-span-2"
            >
              {products.map((p) => (
                <MenuItem key={p.id} value={p.id}>
                  {p.sku} - {p.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label="Số lượng"
              type="number"
              size="small"
              slotProps={{ htmlInput: { min: 0.01, step: 0.01 } }}
              value={form.quantity}
              onChange={(e) => setForm({ ...form, quantity: Number(e.target.value) })}
            />
            <TextField
              label="Đơn giá"
              type="number"
              size="small"
              value={form.unitPrice}
              onChange={(e) => setForm({ ...form, unitPrice: Number(e.target.value) })}
            />
            <TextField
              label="Ghi chú"
              size="small"
              value={form.note}
              onChange={(e) => setForm({ ...form, note: e.target.value })}
              className="sm:col-span-2"
            />
            <Stack direction="row" spacing={1} className="justify-end sm:col-span-2">
              <Button type="button" size="small" variant="outlined" onClick={() => setOpen(false)}>
                Hủy
              </Button>
              <Button type="submit" size="small">
                Tạo
              </Button>
            </Stack>
          </Box>
        </Modal>
      ) : null}

      {detail ? (
        <Modal
          title={detail.code}
          icon={<VisibilityOutlinedIcon color="primary" />}
          onClose={() => setDetail(null)}
          maxWidth="sm"
          actions={
            <>
              <Button size="small" variant="outlined" onClick={() => setDetail(null)}>
                Đóng
              </Button>
              {detail.status === 'Draft' ? (
                <>
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
                  Hủy phiếu
                </Button>
              ) : null}
            </>
          }
        >
          <Stack spacing={1} className="mb-2">
            <Stack direction="row" spacing={1} className="flex-wrap items-center">
              <Badge tone={statusTone(detail.status)}>{detail.status}</Badge>
              {kind === 'export' && isStoreExport(detail) ? (
                <Chip size="small" color="success" label="Đơn KH mua (FreshMart)" />
              ) : null}
              {(detail as ExportReceiptDto).invoiceCode ? (
                <Chip size="small" variant="outlined" label={(detail as ExportReceiptDto).invoiceCode!} />
              ) : null}
            </Stack>
            <Typography variant="body2">
              <strong>Thời gian:</strong> {formatDateTime(detail.receiptDate)}
            </Typography>
            <Typography variant="body2">
              <strong>Kho:</strong> {detail.warehouseCode} — {detail.warehouseName}
            </Typography>
            <Typography variant="body2">
              <strong>{kind === 'import' ? 'NCC' : 'KH'}:</strong>{' '}
              {kind === 'import'
                ? (detail as ImportReceiptDto).supplierName
                : (detail as ExportReceiptDto).customerName}
            </Typography>
            <Typography variant="body2">
              <strong>Tổng:</strong> {money(detail.totalAmount)} · {detail.totalQuantity} sp
            </Typography>
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
                  <TableCell align="right">Đơn giá</TableCell>
                  <TableCell align="right">TT</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {detail.details.map((d) => (
                  <TableRow key={d.id}>
                    <TableCell>{d.productSku}</TableCell>
                    <TableCell>{d.productName}</TableCell>
                    <TableCell align="right">{d.quantity}</TableCell>
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
            ? 'Xác nhận sẽ cập nhật tồn kho theo chi tiết phiếu. Tiếp tục?'
            : confirmAct?.action === 'cancel'
              ? 'Hủy sẽ hoàn tác tồn (nếu đã xác nhận). Tiếp tục?'
              : 'Xóa phiếu Draft? Không hoàn tác.'
        }
        confirmLabel={confirmAct?.action === 'remove' ? 'Xóa' : 'Đồng ý'}
        danger={confirmAct?.action === 'remove'}
        onClose={() => setConfirmAct(null)}
        onConfirm={() => void runAct()}
      />
    </Box>
  )
}
