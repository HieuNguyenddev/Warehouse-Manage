import { useEffect, useState, type ReactNode } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  Box,
  Button,
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
import HistoryIcon from '@mui/icons-material/History'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import { transactionsApi, warehousesApi } from '../../api/endpoints'
import { ApiError } from '../../api/client'
import { Alert, Badge, Modal, PageHeader, Pager } from '../../components/ui'
import type { StockTransactionDto, WarehouseDto } from '../../types/api'

const TYPES = ['Import', 'ImportCancel', 'Export', 'ExportCancel', 'Stocktake', 'StocktakeCancel', 'Manual']

export function TransactionsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [rows, setRows] = useState<StockTransactionDto[]>([])
  const [warehouses, setWarehouses] = useState<WarehouseDto[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [type, setType] = useState('')
  const [search, setSearch] = useState('')
  const [warehouseId, setWarehouseId] = useState(Number(searchParams.get('warehouseId') || 0) || 0)
  const [productId, setProductId] = useState(Number(searchParams.get('productId') || 0) || 0)
  const [error, setError] = useState('')
  const [detail, setDetail] = useState<StockTransactionDto | null>(null)

  useEffect(() => {
    void warehousesApi.list({ pageNumber: 1, pageSize: 100 }).then((r) => setWarehouses(r.items))
  }, [])

  useEffect(() => {
    void (async () => {
      try {
        const data = await transactionsApi.list({
          pageNumber: page,
          pageSize: 15,
          search: search || undefined,
          type: type || undefined,
          warehouseId: warehouseId || undefined,
          productId: productId || undefined,
        })
        setRows(data.items)
        setTotalPages(data.totalPages || 1)
      } catch (e) {
        setError(e instanceof ApiError ? e.message : 'Lỗi tải lịch sử')
      }
    })()

    const next = new URLSearchParams()
    if (warehouseId) next.set('warehouseId', String(warehouseId))
    if (productId) next.set('productId', String(productId))
    if (type) next.set('type', type)
    setSearchParams(next, { replace: true })
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, type, search, warehouseId, productId])

  return (
    <Box>
      <PageHeader
        title="Lịch sử tồn"
        subtitle="Sổ cái biến động tồn · lọc theo kho / sản phẩm / loại"
        icon={<HistoryIcon color="primary" />}
      />
      <Paper className="p-4" sx={{ borderRadius: 3 }}>
        {error ? <Alert>{error}</Alert> : null}
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} className="mb-3 flex-wrap">
          <TextField
            size="small"
            placeholder="Tìm mã chứng từ / SP"
            value={search}
            onChange={(e) => {
              setPage(1)
              setSearch(e.target.value)
            }}
            className="sm:max-w-xs"
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
            className="sm:w-52"
          >
            <MenuItem value={0}>Tất cả kho</MenuItem>
            {warehouses.map((w) => (
              <MenuItem key={w.id} value={w.id}>
                {w.code}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            size="small"
            type="number"
            label="ProductId"
            value={productId || ''}
            onChange={(e) => {
              setPage(1)
              setProductId(Number(e.target.value) || 0)
            }}
            className="sm:w-36"
            helperText={productId ? 'Lọc theo SP' : ' '}
          />
          <TextField
            select
            size="small"
            label="Loại"
            value={type}
            onChange={(e) => {
              setPage(1)
              setType(e.target.value)
            }}
            className="sm:w-52"
          >
            <MenuItem value="">Tất cả loại</MenuItem>
            {TYPES.map((t) => (
              <MenuItem key={t} value={t}>
                {t}
              </MenuItem>
            ))}
          </TextField>
          {productId || warehouseId ? (
            <Button
              variant="outlined"
              onClick={() => {
                setWarehouseId(0)
                setProductId(0)
                setPage(1)
              }}
            >
              Xóa lọc
            </Button>
          ) : null}
        </Stack>
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>ID</TableCell>
                <TableCell>Loại</TableCell>
                <TableCell>Kho</TableCell>
                <TableCell>SKU</TableCell>
                <TableCell align="right">Trước</TableCell>
                <TableCell align="right">Δ</TableCell>
                <TableCell align="right">Sau</TableCell>
                <TableCell>Chứng từ</TableCell>
                <TableCell>Thời gian</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((r) => (
                <TableRow
                  key={r.id}
                  hover
                  selected={detail?.id === r.id}
                  onClick={() => setDetail(r)}
                  sx={{ cursor: 'pointer' }}
                >
                  <TableCell>{r.id}</TableCell>
                  <TableCell>
                    <Badge>{r.type}</Badge>
                  </TableCell>
                  <TableCell>{r.warehouseCode}</TableCell>
                  <TableCell>{r.productSku}</TableCell>
                  <TableCell align="right">{r.quantityBefore}</TableCell>
                  <TableCell
                    align="right"
                    sx={{ fontWeight: 700, color: r.quantityChange >= 0 ? 'success.main' : 'error.main' }}
                  >
                    {r.quantityChange >= 0 ? `+${r.quantityChange}` : r.quantityChange}
                  </TableCell>
                  <TableCell align="right">{r.quantityAfter}</TableCell>
                  <TableCell>{r.referenceCode || '—'}</TableCell>
                  <TableCell>{new Date(r.createdAt).toLocaleString('vi-VN')}</TableCell>
                </TableRow>
              ))}
              {!rows.length ? (
                <TableRow>
                  <TableCell colSpan={9}>Chưa có giao dịch</TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        </TableContainer>
        <Pager page={page} totalPages={totalPages} onChange={setPage} />
      </Paper>

      {detail ? (
        <Modal
          title={`Giao dịch #${detail.id}`}
          icon={<InfoOutlinedIcon color="primary" />}
          onClose={() => setDetail(null)}
          maxWidth="md"
          actions={
            <Button variant="outlined" onClick={() => setDetail(null)}>
              Đóng
            </Button>
          }
        >
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
            {(
              [
                ['Loại', <Badge key="t">{detail.type}</Badge>],
                ['Kho', `${detail.warehouseCode} · ${detail.warehouseName || ''}`],
                ['SKU', detail.productSku],
                ['Sản phẩm', detail.productName],
                ['ĐVT', detail.unitSymbol || '—'],
                ['Trước', detail.quantityBefore],
                [
                  'Thay đổi',
                  <Typography
                    key="d"
                    sx={{ fontWeight: 700, color: detail.quantityChange >= 0 ? 'success.main' : 'error.main' }}
                  >
                    {detail.quantityChange >= 0 ? `+${detail.quantityChange}` : detail.quantityChange}
                  </Typography>,
                ],
                ['Sau', detail.quantityAfter],
                ['Chứng từ', detail.referenceCode || '—'],
                ['Loại CT', detail.referenceType || '—'],
                ['Ref ID', detail.referenceId ?? '—'],
                ['Thời gian', new Date(detail.createdAt).toLocaleString('vi-VN')],
                ['Ghi chú', detail.note || '—'],
              ] as [string, ReactNode][]
            ).map(([label, value]) => (
              <Paper key={label} variant="outlined" className="p-3" sx={{ borderRadius: 2 }}>
                <Typography variant="caption" color="text.secondary" className="block">
                  {label}
                </Typography>
                <Typography sx={{ fontWeight: 600, mt: 0.5, wordBreak: 'break-word' }}>{value}</Typography>
              </Paper>
            ))}
          </div>
        </Modal>
      ) : null}
    </Box>
  )
}
