import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import {
  Box,
  Button,
  Checkbox,
  FormControlLabel,
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
import LayersIcon from '@mui/icons-material/Layers'
import AddIcon from '@mui/icons-material/Add'
import RefreshIcon from '@mui/icons-material/Refresh'
import TuneIcon from '@mui/icons-material/Tune'
import HistoryIcon from '@mui/icons-material/History'
import VisibilityOutlinedIcon from '@mui/icons-material/VisibilityOutlined'
import FactCheckIcon from '@mui/icons-material/FactCheck'
import CloseIcon from '@mui/icons-material/Close'
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

  const [detail, setDetail] = useState<WarehouseStockDto | null>(null)
  const [history, setHistory] = useState<StockTransactionDto[]>([])
  const [openingOpen, setOpeningOpen] = useState(false)
  const [adjustOpen, setAdjustOpen] = useState(false)
  const [minOpen, setMinOpen] = useState(false)

  const [opening, setOpening] = useState({
    warehouseId: 0,
    productId: 0,
    quantity: 0,
    minQuantity: '' as number | '',
    reason: 'Tồn đầu kỳ',
  })
  const [adjust, setAdjust] = useState({ quantity: 0, reason: '' })
  const [minQty, setMinQty] = useState<number | ''>('')
  const [useProductMin, setUseProductMin] = useState(true)

  const warehouseLabel = useMemo(() => {
    if (!warehouseId) return 'Tất cả kho'
    const w = warehouses.find((x) => x.id === warehouseId)
    return w ? `${w.code} · ${w.name}` : `Kho #${warehouseId}`
  }, [warehouseId, warehouses])

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
    // sync URL
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

  async function submitOpening(e: FormEvent) {
    e.preventDefault()
    try {
      await stocksApi.createOpening({
        warehouseId: opening.warehouseId,
        productId: opening.productId,
        quantity: Number(opening.quantity),
        minQuantity: opening.minQuantity === '' ? null : Number(opening.minQuantity),
        reason: opening.reason.trim(),
      })
      toast.success('Đã khởi tạo tồn đầu kỳ')
      setOpeningOpen(false)
      void reload()
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Khởi tạo tồn thất bại')
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
      toast.success('Đã điều chỉnh tồn')
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
      <Paper variant="outlined" className="p-3" sx={{ borderRadius: 2 }}>
        <Typography variant="caption" color="text.secondary" className="block">
          {label}
        </Typography>
        <Typography sx={{ fontWeight: 600, mt: 0.5, wordBreak: 'break-word' }}>{value}</Typography>
      </Paper>
    )
  }

  return (
    <Box>
      <PageHeader
        title="Tồn kho"
        subtitle="Theo dõi tồn theo kho · cảnh báo thấp · điều chỉnh có lý do (phiếu nhập/xuất/kiểm kê mới là nguồn chính)"
        icon={<LayersIcon color="primary" />}
        actions={
          <Stack direction="row" spacing={1}>
            <Button startIcon={<RefreshIcon />} variant="outlined" onClick={() => void reload()} disabled={loading}>
              Làm mới
            </Button>
            <Button
              startIcon={<AddIcon />}
              variant="contained"
              onClick={() => {
                setOpening({
                  warehouseId: warehouseId || warehouses[0]?.id || 0,
                  productId: products[0]?.id || 0,
                  quantity: 0,
                  minQuantity: '',
                  reason: 'Tồn đầu kỳ',
                })
                setOpeningOpen(true)
              }}
            >
              Tồn đầu kỳ
            </Button>
          </Stack>
        }
      />

      {summary ? (
        <div className="mb-3 grid grid-cols-2 gap-2 md:grid-cols-4">
          {[
            { label: 'SKU đang theo dõi', value: summary.totalSkus },
            { label: 'Tồn thấp', value: summary.lowStockCount, warn: true },
            { label: 'Hết hàng', value: summary.outOfStockCount, danger: true },
            { label: 'Tổng số lượng', value: summary.totalQuantity },
          ].map((c) => (
            <Paper key={c.label} className="p-3" sx={{ borderRadius: 2 }} variant="outlined">
              <Typography variant="caption" color="text.secondary">
                {c.label}
              </Typography>
              <Typography
                variant="h6"
                sx={{
                  fontWeight: 700,
                  color: c.danger ? 'error.main' : c.warn ? 'warning.main' : 'text.primary',
                }}
              >
                {c.value}
              </Typography>
            </Paper>
          ))}
        </div>
      ) : null}

      <Paper className="p-4" sx={{ borderRadius: 3 }}>
        {error ? <Alert>{error}</Alert> : null}
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} className="mb-3">
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

        <Typography variant="body2" color="text.secondary" className="mb-2">
          Đang xem: <strong>{warehouseLabel}</strong> · bấm dòng để xem chi tiết / điều chỉnh / lịch sử
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
                    <Typography variant="body2" sx={{ fontWeight: 600 }}>
                      {r.warehouseCode}
                    </Typography>
                    <Typography variant="caption" color="text.secondary">
                      {r.warehouseName}
                    </Typography>
                  </TableCell>
                  <TableCell>{r.productSku}</TableCell>
                  <TableCell>
                    <Typography variant="body2">{r.productName}</Typography>
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
                  <TableCell colSpan={8}>Chưa có dữ liệu tồn kho</TableCell>
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
          icon={<LayersIcon color="primary" />}
          onClose={() => setDetail(null)}
          maxWidth="md"
          actions={
            <>
              <Button startIcon={<CloseIcon />} variant="outlined" onClick={() => setDetail(null)}>
                Đóng
              </Button>
              <Button
                startIcon={<FactCheckIcon />}
                variant="outlined"
                onClick={() => navigate(`/admin/stocktakes?warehouseId=${detail.warehouseId}`)}
              >
                Kiểm kho
              </Button>
              <Button
                startIcon={<HistoryIcon />}
                variant="outlined"
                onClick={() =>
                  navigate(
                    `/admin/transactions?warehouseId=${detail.warehouseId}&productId=${detail.productId}`,
                  )
                }
              >
                Lịch sử đầy đủ
              </Button>
              <Button
                startIcon={<TuneIcon />}
                variant="outlined"
                onClick={() => {
                  setUseProductMin(detail.minQuantity == null)
                  setMinQty(detail.minQuantity ?? detail.productMinStock ?? 0)
                  setMinOpen(true)
                }}
              >
                Min kho
              </Button>
              <Button
                variant="contained"
                onClick={() => {
                  setAdjust({ quantity: detail.quantity, reason: '' })
                  setAdjustOpen(true)
                }}
              >
                Điều chỉnh tồn
              </Button>
            </>
          }
        >
          <Box className="space-y-3">
            <Typography variant="body2" color="text.secondary">
              Tồn thay đổi chính từ phiếu nhập / xuất / kiểm kê. Điều chỉnh thủ công chỉ dùng khi cần
              hiệu chỉnh có lý do (ghi sổ Manual).
            </Typography>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
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

            <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
              Biến động gần đây
            </Typography>
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

      {openingOpen ? (
        <Modal
          title="Khởi tạo tồn đầu kỳ"
          icon={<AddIcon color="primary" />}
          onClose={() => setOpeningOpen(false)}
          actions={
            <>
              <Button variant="outlined" onClick={() => setOpeningOpen(false)}>
                Hủy
              </Button>
              <Button type="submit" form="opening-form" variant="contained">
                Lưu
              </Button>
            </>
          }
        >
          <Stack component="form" id="opening-form" onSubmit={submitOpening} spacing={2.5}>
            <TextField
              select
              fullWidth
              required
              label="Kho"
              value={opening.warehouseId || ''}
              onChange={(e) => setOpening((s) => ({ ...s, warehouseId: Number(e.target.value) }))}
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
              label="Sản phẩm"
              value={opening.productId || ''}
              onChange={(e) => setOpening((s) => ({ ...s, productId: Number(e.target.value) }))}
              slotProps={{
                select: {
                  MenuProps: {
                    disablePortal: true,
                    slotProps: { paper: { sx: { maxHeight: 280 } } },
                  },
                },
              }}
            >
              {products.map((p) => (
                <MenuItem key={p.id} value={p.id}>
                  {p.sku} · {p.name}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              fullWidth
              required
              type="number"
              label="Số lượng đầu kỳ"
              value={opening.quantity}
              onChange={(e) => setOpening((s) => ({ ...s, quantity: Number(e.target.value) }))}
              slotProps={{ htmlInput: { min: 0, step: 'any' } }}
            />
            <TextField
              fullWidth
              type="number"
              label="Min theo kho (tuỳ chọn)"
              value={opening.minQuantity}
              onChange={(e) =>
                setOpening((s) => ({
                  ...s,
                  minQuantity: e.target.value === '' ? '' : Number(e.target.value),
                }))
              }
              helperText="Để trống = dùng Min của sản phẩm"
              slotProps={{ htmlInput: { min: 0 } }}
            />
            <TextField
              fullWidth
              required
              label="Lý do"
              value={opening.reason}
              onChange={(e) => setOpening((s) => ({ ...s, reason: e.target.value }))}
            />
          </Stack>
        </Modal>
      ) : null}

      {adjustOpen && detail ? (
        <Modal
          title="Điều chỉnh tồn"
          icon={<TuneIcon color="primary" />}
          onClose={() => setAdjustOpen(false)}
          actions={
            <>
              <Button variant="outlined" onClick={() => setAdjustOpen(false)}>
                Hủy
              </Button>
              <Button type="submit" form="adjust-form" variant="contained">
                Xác nhận
              </Button>
            </>
          }
        >
          <Box component="form" id="adjust-form" onSubmit={submitAdjust} className="space-y-3">
            <Alert>
              Đang chỉnh <strong>{detail.productSku}</strong> tại <strong>{detail.warehouseCode}</strong>. Tồn hiện
              tại: <strong>{detail.quantity}</strong>
            </Alert>
            <TextField
              fullWidth
              required
              type="number"
              label="Tồn mới"
              value={adjust.quantity}
              onChange={(e) => setAdjust((s) => ({ ...s, quantity: Number(e.target.value) }))}
              slotProps={{ htmlInput: { min: 0, step: 'any' } }}
            />
            <TextField
              fullWidth
              required
              label="Lý do điều chỉnh"
              placeholder="VD: Hư hỏng, thất thoát, hiệu chỉnh sổ sách..."
              value={adjust.reason}
              onChange={(e) => setAdjust((s) => ({ ...s, reason: e.target.value }))}
            />
          </Box>
        </Modal>
      ) : null}

      {minOpen && detail ? (
        <Modal
          title="Ngưỡng cảnh báo theo kho"
          icon={<TuneIcon color="primary" />}
          onClose={() => setMinOpen(false)}
          actions={
            <>
              <Button variant="outlined" onClick={() => setMinOpen(false)}>
                Hủy
              </Button>
              <Button type="submit" form="min-form" variant="contained">
                Lưu
              </Button>
            </>
          }
        >
          <Box component="form" id="min-form" onSubmit={submitMin} className="space-y-3">
            <Typography variant="body2" color="text.secondary">
              Product.MinStock = <strong>{detail.productMinStock}</strong>
            </Typography>
            <FormControlLabel
              control={
                <Checkbox
                  checked={useProductMin}
                  onChange={(e) => setUseProductMin(e.target.checked)}
                />
              }
              label="Dùng Min của sản phẩm"
            />
            {!useProductMin ? (
              <TextField
                fullWidth
                required
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
