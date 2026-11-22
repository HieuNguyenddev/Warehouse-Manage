import { useEffect, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  Box,
  Button,
  Checkbox,
  FormControlLabel,
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
import FactCheckIcon from '@mui/icons-material/FactCheck'
import AddIcon from '@mui/icons-material/Add'
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh'
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlineOutlined'
import CancelOutlinedIcon from '@mui/icons-material/CancelOutlined'
import DeleteIcon from '@mui/icons-material/Delete'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import { productsApi, stocktakesApi, warehousesApi } from '../../api/endpoints'
import { ApiError } from '../../api/client'
import { useToast } from '../../components/ToastContext'
import { Alert, Badge, ConfirmDialog, Modal, PageHeader, Pager, statusTone } from '../../components/ui'
import type { ProductDto, StocktakeDto, WarehouseDto } from '../../types/api'

export function StocktakesPage() {
  const toast = useToast()
  const [searchParams] = useSearchParams()
  const [rows, setRows] = useState<StocktakeDto[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [error, setError] = useState('')
  const [open, setOpen] = useState(false)
  const [autoOpen, setAutoOpen] = useState(false)
  const [detail, setDetail] = useState<StocktakeDto | null>(null)
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([])
  const [products, setProducts] = useState<ProductDto[]>([])
  const [filterWarehouseId, setFilterWarehouseId] = useState(
    Number(searchParams.get('warehouseId') || 0) || 0,
  )
  const [form, setForm] = useState({ warehouseId: 0, productId: 0, countedQuantity: 0, note: '' })
  const [autoForm, setAutoForm] = useState({
    warehouseId: 0,
    note: '',
    matchSystemQuantity: true,
    lowStockOnly: false,
  })
  const [busy, setBusy] = useState(false)
  const [confirmAct, setConfirmAct] = useState<{ id: number; action: 'confirm' | 'cancel' | 'remove' } | null>(null)

  async function reload() {
    const data = await stocktakesApi.list({
      pageNumber: page,
      pageSize: 10,
      warehouseId: filterWarehouseId || undefined,
    })
    setRows(data.items)
    setTotalPages(data.totalPages || 1)
  }

  useEffect(() => {
    void (async () => {
      const [w, p] = await Promise.all([
        warehousesApi.list({ pageNumber: 1, pageSize: 100 }),
        productsApi.list({ pageNumber: 1, pageSize: 100 }),
      ])
      setWarehouses(w.items)
      setProducts(p.items)
      const fromUrl = Number(searchParams.get('warehouseId') || 0) || 0
      const firstWh = fromUrl || w.items[0]?.id || 0
      setForm((f) => ({ ...f, warehouseId: firstWh, productId: p.items[0]?.id || 0 }))
      setAutoForm((f) => ({ ...f, warehouseId: firstWh }))
    })()
  }, [searchParams])

  useEffect(() => {
    void reload().catch((e) => setError(e instanceof ApiError ? e.message : 'Lỗi'))
  }, [page, filterWarehouseId])

  async function create(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    try {
      await stocktakesApi.create({
        warehouseId: form.warehouseId,
        note: form.note || null,
        details: [{ productId: form.productId, countedQuantity: form.countedQuantity }],
      })
      toast.success('Đã tạo phiếu kiểm kho')
      setOpen(false)
      await reload()
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Tạo thất bại'
      setError(msg)
      toast.error(msg)
    } finally {
      setBusy(false)
    }
  }

  async function createAuto(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    try {
      const created = await stocktakesApi.createAuto({
        warehouseId: autoForm.warehouseId,
        note: autoForm.note || null,
        matchSystemQuantity: autoForm.matchSystemQuantity,
        lowStockOnly: autoForm.lowStockOnly,
      })
      toast.success(`Kiểm kho tự động: ${created.code} · ${created.totalLines} dòng`)
      setAutoOpen(false)
      setDetail(created)
      await reload()
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Kiểm kho tự động thất bại'
      setError(msg)
      toast.error(msg)
    } finally {
      setBusy(false)
    }
  }

  async function runAct() {
    if (!confirmAct) return
    const { id, action } = confirmAct
    setBusy(true)
    try {
      if (action === 'confirm') await stocktakesApi.confirm(id)
      if (action === 'cancel') await stocktakesApi.cancel(id)
      if (action === 'remove') await stocktakesApi.remove(id)
      toast.success(
        action === 'confirm' ? 'Đã xác nhận kiểm kho' : action === 'cancel' ? 'Đã hủy phiếu' : 'Đã xóa phiếu',
      )
      setConfirmAct(null)
      setDetail(null)
      await reload()
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Thao tác thất bại'
      setError(msg)
      toast.error(msg)
    } finally {
      setBusy(false)
    }
  }

  return (
    <Box>
      <PageHeader
        title="Kiểm kho"
        subtitle="Lập phiếu thủ công hoặc kiểm kho tự động từ tồn hệ thống · Nháp → Xác nhận"
        icon={<FactCheckIcon color="primary" />}
        actions={
          <Stack direction="row" spacing={1}>
            <Button
              startIcon={<AutoFixHighIcon />}
              variant="outlined"
              color="secondary"
              onClick={() => setAutoOpen(true)}
            >
              Kiểm kho tự động
            </Button>
            <Button startIcon={<AddIcon />} onClick={() => setOpen(true)}>
              Tạo thủ công
            </Button>
          </Stack>
        }
      />
      <Paper className="p-4" sx={{ borderRadius: 3 }}>
        {error ? <Alert>{error}</Alert> : null}
        <Stack direction="row" spacing={1.5} className="mb-3">
          <TextField
            select
            size="small"
            label="Lọc kho"
            value={filterWarehouseId}
            onChange={(e) => {
              setPage(1)
              setFilterWarehouseId(Number(e.target.value))
            }}
            className="w-64"
          >
            <MenuItem value={0}>Tất cả kho</MenuItem>
            {warehouses.map((w) => (
              <MenuItem key={w.id} value={w.id}>
                {w.code} · {w.name}
              </MenuItem>
            ))}
          </TextField>
        </Stack>
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Mã</TableCell>
                <TableCell>Kho</TableCell>
                <TableCell>Trạng thái</TableCell>
                <TableCell>Ghi chú</TableCell>
                <TableCell align="right">Dòng</TableCell>
                <TableCell align="right">Lệch</TableCell>
                <TableCell align="right">Thao tác</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((r) => (
                <TableRow
                  key={r.id}
                  hover
                  selected={detail?.id === r.id}
                  sx={{ cursor: 'pointer' }}
                  onClick={async () => setDetail(await stocktakesApi.get(r.id))}
                >
                  <TableCell>{r.code}</TableCell>
                  <TableCell>{r.warehouseCode}</TableCell>
                  <TableCell>
                    <Badge tone={statusTone(r.status)}>{r.status}</Badge>
                  </TableCell>
                  <TableCell>
                    <Typography variant="caption" color="text.secondary">
                      {r.note || '—'}
                    </Typography>
                  </TableCell>
                  <TableCell align="right">{r.totalLines}</TableCell>
                  <TableCell align="right">{r.diffLines}</TableCell>
                  <TableCell align="right" onClick={(e) => e.stopPropagation()}>
                    <Stack direction="row" spacing={0.5} className="flex-wrap justify-end">
                      <Button
                        size="small"
                        variant="text"
                        startIcon={<VisibilityOutlinedIcon />}
                        onClick={async () => setDetail(await stocktakesApi.get(r.id))}
                      >
                        Chi tiết
                      </Button>
                      {r.status === 'Draft' ? (
                        <>
                          <Button
                            size="small"
                            startIcon={<CheckCircleOutlineIcon />}
                            onClick={() => setConfirmAct({ id: r.id, action: 'confirm' })}
                          >
                            Xác nhận
                          </Button>
                          <Button
                            size="small"
                            variant="outlined"
                            startIcon={<CancelOutlinedIcon />}
                            onClick={() => setConfirmAct({ id: r.id, action: 'cancel' })}
                          >
                            Hủy
                          </Button>
                          <Button
                            size="small"
                            color="error"
                            startIcon={<DeleteIcon />}
                            onClick={() => setConfirmAct({ id: r.id, action: 'remove' })}
                          >
                            Xóa
                          </Button>
                        </>
                      ) : r.status === 'Confirmed' ? (
                        <Button
                          size="small"
                          variant="outlined"
                          startIcon={<CancelOutlinedIcon />}
                          onClick={() => setConfirmAct({ id: r.id, action: 'cancel' })}
                        >
                          Hủy
                        </Button>
                      ) : null}
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
        <Pager page={page} totalPages={totalPages} onChange={setPage} />
      </Paper>

      {open ? (
        <Modal title="Tạo kiểm kho thủ công" icon={<AddIcon color="primary" />} onClose={() => setOpen(false)}>
          <Box component="form" onSubmit={create} className="grid grid-cols-1 gap-3 pt-1 sm:grid-cols-2">
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
              label="Sản phẩm"
              size="small"
              value={form.productId}
              onChange={(e) => setForm({ ...form, productId: Number(e.target.value) })}
            >
              {products.map((p) => (
                <MenuItem key={p.id} value={p.id}>
                  {p.sku} - {p.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label="Số đếm thực tế"
              type="number"
              size="small"
              value={form.countedQuantity}
              onChange={(e) => setForm({ ...form, countedQuantity: Number(e.target.value) })}
            />
            <TextField
              label="Ghi chú"
              size="small"
              value={form.note}
              onChange={(e) => setForm({ ...form, note: e.target.value })}
            />
            <Stack direction="row" spacing={1.5} className="justify-end sm:col-span-2">
              <Button type="button" variant="outlined" onClick={() => setOpen(false)}>
                Đóng
              </Button>
              <Button type="submit" disabled={busy}>
                {busy ? 'Đang tạo...' : 'Tạo'}
              </Button>
            </Stack>
          </Box>
        </Modal>
      ) : null}

      {autoOpen ? (
        <Modal
          title="Kiểm kho tự động"
          icon={<AutoFixHighIcon color="secondary" />}
          onClose={() => setAutoOpen(false)}
        >
          <Typography variant="body2" color="text.secondary" className="mb-3">
            Hệ thống tự lấy toàn bộ (hoặc tồn thấp) sản phẩm đang có tồn trong kho để tạo phiếu nháp.
          </Typography>
          <Box component="form" onSubmit={createAuto} className="grid grid-cols-1 gap-3">
            <TextField
              select
              label="Kho cần kiểm"
              size="small"
              value={autoForm.warehouseId}
              onChange={(e) => setAutoForm({ ...autoForm, warehouseId: Number(e.target.value) })}
            >
              {warehouses.map((w) => (
                <MenuItem key={w.id} value={w.id}>
                  {w.code} - {w.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label="Ghi chú"
              size="small"
              value={autoForm.note}
              onChange={(e) => setAutoForm({ ...autoForm, note: e.target.value })}
              placeholder="Để trống sẽ dùng ghi chú mặc định"
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={autoForm.matchSystemQuantity}
                  onChange={(e) => setAutoForm({ ...autoForm, matchSystemQuantity: e.target.checked })}
                />
              }
              label="Số đếm = tồn hệ thống (chênh lệch 0)"
            />
            <FormControlLabel
              control={
                <Checkbox
                  checked={autoForm.lowStockOnly}
                  onChange={(e) => setAutoForm({ ...autoForm, lowStockOnly: e.target.checked })}
                />
              }
              label="Chỉ lấy mặt hàng tồn thấp"
            />
            <Stack direction="row" spacing={1.5} className="justify-end">
              <Button type="button" variant="outlined" onClick={() => setAutoOpen(false)}>
                Đóng
              </Button>
              <Button type="submit" color="secondary" startIcon={<AutoFixHighIcon />} disabled={busy}>
                {busy ? 'Đang tạo...' : 'Chạy kiểm tự động'}
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
          maxWidth="md"
          actions={
            <>
              <Button variant="outlined" onClick={() => setDetail(null)}>
                Đóng
              </Button>
              {detail.status === 'Draft' ? (
                <>
                  <Button
                    startIcon={<CheckCircleOutlineIcon />}
                    onClick={() => setConfirmAct({ id: detail.id, action: 'confirm' })}
                  >
                    Xác nhận
                  </Button>
                  <Button
                    variant="outlined"
                    startIcon={<CancelOutlinedIcon />}
                    onClick={() => setConfirmAct({ id: detail.id, action: 'cancel' })}
                  >
                    Hủy phiếu
                  </Button>
                  <Button
                    color="error"
                    startIcon={<DeleteIcon />}
                    onClick={() => setConfirmAct({ id: detail.id, action: 'remove' })}
                  >
                    Xóa
                  </Button>
                </>
              ) : detail.status === 'Confirmed' ? (
                <Button
                  variant="outlined"
                  startIcon={<CancelOutlinedIcon />}
                  onClick={() => setConfirmAct({ id: detail.id, action: 'cancel' })}
                >
                  Hủy phiếu
                </Button>
              ) : null}
            </>
          }
        >
          <div className="mb-3 grid grid-cols-1 gap-2 sm:grid-cols-3">
            <Paper variant="outlined" className="p-3" sx={{ borderRadius: 2 }}>
              <Typography variant="caption" color="text.secondary">
                Trạng thái
              </Typography>
              <Box className="mt-1">
                <Badge tone={statusTone(detail.status)}>{detail.status}</Badge>
              </Box>
            </Paper>
            <Paper variant="outlined" className="p-3" sx={{ borderRadius: 2 }}>
              <Typography variant="caption" color="text.secondary">
                Kho
              </Typography>
              <Typography sx={{ fontWeight: 600 }}>{detail.warehouseName}</Typography>
            </Paper>
            <Paper variant="outlined" className="p-3" sx={{ borderRadius: 2 }}>
              <Typography variant="caption" color="text.secondary">
                Dòng / lệch
              </Typography>
              <Typography sx={{ fontWeight: 600 }}>
                {detail.totalLines} / {detail.diffLines}
              </Typography>
            </Paper>
            {detail.note ? (
              <Paper variant="outlined" className="p-3 sm:col-span-3" sx={{ borderRadius: 2 }}>
                <Typography variant="caption" color="text.secondary">
                  Ghi chú
                </Typography>
                <Typography>{detail.note}</Typography>
              </Paper>
            ) : null}
          </div>
          <TableContainer>
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell>SKU</TableCell>
                  <TableCell align="right">Hệ thống</TableCell>
                  <TableCell align="right">Đếm</TableCell>
                  <TableCell align="right">Lệch</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {detail.details.map((d) => (
                  <TableRow key={d.id}>
                    <TableCell>{d.productSku}</TableCell>
                    <TableCell align="right">{d.systemQuantity}</TableCell>
                    <TableCell align="right">{d.countedQuantity}</TableCell>
                    <TableCell align="right">
                      <Badge tone={d.difference === 0 ? 'ok' : 'warn'}>{d.difference}</Badge>
                    </TableCell>
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
            ? 'Xác nhận kiểm kho'
            : confirmAct?.action === 'cancel'
              ? 'Hủy phiếu kiểm kho'
              : 'Xóa phiếu kiểm kho'
        }
        message={
          confirmAct?.action === 'confirm'
            ? 'Xác nhận sẽ gán tồn kho theo số đếm. Tiếp tục?'
            : confirmAct?.action === 'cancel'
              ? 'Hủy phiếu (nếu đã xác nhận sẽ hoàn tồn về số hệ thống lúc lập phiếu).'
              : 'Xóa phiếu nháp? Thao tác không hoàn tác.'
        }
        confirmLabel={confirmAct?.action === 'remove' ? 'Xóa' : 'Đồng ý'}
        danger={confirmAct?.action === 'remove'}
        loading={busy}
        onClose={() => setConfirmAct(null)}
        onConfirm={() => void runAct()}
      />
    </Box>
  )
}
