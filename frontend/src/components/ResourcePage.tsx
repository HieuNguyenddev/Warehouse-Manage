import { useCallback, useEffect, useState, type FormEvent, type MouseEvent, type ReactNode } from 'react'
import {
  Box,
  Button,
  Checkbox,
  Divider,
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
import AddIcon from '@mui/icons-material/Add'
import RefreshIcon from '@mui/icons-material/Refresh'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import DeleteIcon from '@mui/icons-material/Delete'
import SearchIcon from '@mui/icons-material/Search'
import SaveIcon from '@mui/icons-material/Save'
import CloseIcon from '@mui/icons-material/Close'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import type { PagedResult } from '../types/api'
import { ApiError } from '../api/client'
import { useToast } from './ToastContext'
import { Alert, ConfirmDialog, Modal, PageHeader, Pager } from './ui'

export interface FieldDef {
  name: string
  label: string
  type?: 'text' | 'number' | 'checkbox' | 'textarea' | 'select'
  required?: boolean
  options?: { value: string | number; label: string }[]
  placeholder?: string
}

interface Props<T extends { id: number }> {
  title: string
  subtitle?: string
  icon?: ReactNode
  columns: { key: string; header: string; render?: (row: T) => ReactNode }[]
  fields: FieldDef[]
  list: (q: { search?: string; pageNumber: number; pageSize: number }) => Promise<PagedResult<T>>
  create?: (body: Record<string, unknown>) => Promise<T>
  update?: (id: number, body: Record<string, unknown>) => Promise<T>
  remove?: (id: number) => Promise<void>
  toForm?: (row: T) => Record<string, unknown>
  /** Nội dung thêm trong popup chi tiết */
  renderDetail?: (row: T) => ReactNode
  extraActions?: (row: T, reload: () => void, closeDetail?: () => void) => ReactNode
}

function formatValue(value: unknown) {
  if (value === null || value === undefined || value === '') return '—'
  if (typeof value === 'boolean') return value ? 'Có' : 'Không'
  if (typeof value === 'object') return JSON.stringify(value)
  return String(value)
}

export function ResourcePage<T extends { id: number }>({
  title,
  subtitle,
  icon,
  columns,
  fields,
  list,
  create,
  update,
  remove,
  toForm,
  renderDetail,
  extraActions,
}: Props<T>) {
  const toast = useToast()
  const [rows, setRows] = useState<T[]>([])
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [editing, setEditing] = useState<T | null>(null)
  const [formOpen, setFormOpen] = useState(false)
  const [detail, setDetail] = useState<T | null>(null)
  const [form, setForm] = useState<Record<string, unknown>>({})
  const [deleting, setDeleting] = useState<T | null>(null)
  const [busy, setBusy] = useState(false)

  const reload = useCallback(async () => {
    setLoading(true)
    setError('')
    try {
      const data = await list({ search, pageNumber: page, pageSize: 10 })
      setRows(data.items)
      setTotalPages(data.totalPages || 1)
      setDetail((prev) => {
        if (!prev) return null
        return data.items.find((x) => x.id === prev.id) ?? prev
      })
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Không tải được dữ liệu')
    } finally {
      setLoading(false)
    }
  }, [list, search, page])

  useEffect(() => {
    void reload()
  }, [reload])

  function openCreate() {
    const init: Record<string, unknown> = {}
    fields.forEach((f) => {
      init[f.name] = f.type === 'checkbox' ? true : f.type === 'number' ? 0 : ''
    })
    setForm(init)
    setEditing(null)
    setDetail(null)
    setFormOpen(true)
  }

  function openEdit(row: T) {
    setForm(toForm ? toForm(row) : ({ ...row } as Record<string, unknown>))
    setEditing(row)
    setFormOpen(true)
  }

  function openDetail(row: T) {
    setDetail(row)
  }

  function stopRowClick(e: MouseEvent) {
    e.stopPropagation()
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      const body = { ...form }
      fields.forEach((f) => {
        if (f.type === 'number' && body[f.name] !== '' && body[f.name] != null) {
          body[f.name] = Number(body[f.name])
        }
      })
      let saved: T | null = null
      if (editing && update) {
        saved = await update(editing.id, body)
        toast.success('Cập nhật thành công')
      } else if (create) {
        saved = await create(body)
        toast.success('Thêm mới thành công')
      }
      setFormOpen(false)
      await reload()
      if (saved) setDetail(saved)
      else if (editing) {
        // reload list then keep detail if still present
        const data = await list({ search, pageNumber: page, pageSize: 10 })
        const fresh = data.items.find((x) => x.id === editing.id)
        if (fresh) setDetail(fresh)
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Lưu thất bại'
      setError(msg)
      toast.error(msg)
    } finally {
      setBusy(false)
    }
  }

  async function confirmDelete() {
    if (!remove || !deleting) return
    setBusy(true)
    try {
      await remove(deleting.id)
      toast.success(`Đã xóa #${deleting.id}`)
      if (detail?.id === deleting.id) setDetail(null)
      setDeleting(null)
      await reload()
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Xóa thất bại'
      setError(msg)
      toast.error(msg)
    } finally {
      setBusy(false)
    }
  }

  const detailRows = detail
    ? [
        { label: 'ID', value: detail.id },
        ...fields.map((f) => {
          const raw = (detail as Record<string, unknown>)[f.name]
          if (f.type === 'select' && f.options) {
            const opt = f.options.find((o) => String(o.value) === String(raw))
            return { label: f.label, value: opt?.label ?? formatValue(raw) }
          }
          if (f.type === 'checkbox') return { label: f.label, value: formatValue(Boolean(raw)) }
          return { label: f.label, value: formatValue(raw) }
        }),
        ...columns
          .filter((c) => !fields.some((f) => f.name === c.key) && c.key !== 'id')
          .map((c) => ({
            label: c.header,
            value: c.render ? c.render(detail) : formatValue((detail as Record<string, unknown>)[c.key]),
          })),
      ]
    : []

  return (
    <Box>
      <PageHeader
        title={title}
        subtitle={subtitle ?? 'Bấm vào một dòng để xem chi tiết và thao tác'}
        icon={icon}
        actions={
          create ? (
            <Button startIcon={<AddIcon />} onClick={openCreate}>
              Thêm mới
            </Button>
          ) : null
        }
      />
      <Paper className="p-4" elevation={0} sx={{ borderRadius: 3, border: '1px solid', borderColor: 'divider' }}>
        {error ? <Alert>{error}</Alert> : null}
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} className="mb-3">
          <TextField
            size="small"
            placeholder="Tìm kiếm..."
            value={search}
            onChange={(e) => {
              setPage(1)
              setSearch(e.target.value)
            }}
            className="sm:max-w-xs"
            fullWidth
            slotProps={{
              input: {
                startAdornment: <SearchIcon fontSize="small" className="mr-1 text-slate-400" />,
              },
            }}
          />
          <Button startIcon={<RefreshIcon />} variant="outlined" onClick={() => void reload()} disabled={loading}>
            Làm mới
          </Button>
        </Stack>
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                {columns.map((c) => (
                  <TableCell key={c.key}>{c.header}</TableCell>
                ))}
                <TableCell align="right">Thao tác</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((row) => (
                <TableRow
                  key={row.id}
                  hover
                  selected={detail?.id === row.id}
                  onClick={() => openDetail(row)}
                  sx={{ cursor: 'pointer' }}
                >
                  {columns.map((c) => (
                    <TableCell key={c.key}>
                      {c.render
                        ? c.render(row)
                        : String((row as Record<string, unknown>)[c.key] ?? '')}
                    </TableCell>
                  ))}
                  <TableCell align="right" onClick={stopRowClick}>
                    <Stack direction="row" spacing={0.5} className="justify-end">
                      <Tooltip title="Chi tiết">
                        <IconButton size="small" onClick={() => openDetail(row)}>
                          <InfoOutlinedIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                      {update ? (
                        <Tooltip title="Sửa">
                          <IconButton size="small" color="primary" onClick={() => openEdit(row)}>
                            <EditOutlinedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      ) : null}
                      {remove ? (
                        <Tooltip title="Xóa">
                          <IconButton size="small" color="error" onClick={() => setDeleting(row)}>
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      ) : null}
                      {extraActions?.(row, () => void reload(), () => setDetail(null))}
                    </Stack>
                  </TableCell>
                </TableRow>
              ))}
              {!rows.length && !loading ? (
                <TableRow>
                  <TableCell colSpan={columns.length + 1}>Chưa có dữ liệu</TableCell>
                </TableRow>
              ) : null}
            </TableBody>
          </Table>
        </TableContainer>
        <Pager page={page} totalPages={totalPages} onChange={setPage} />
      </Paper>

      {detail ? (
        <Modal
          title={`Chi tiết #${detail.id}`}
          icon={<InfoOutlinedIcon color="primary" />}
          onClose={() => setDetail(null)}
          maxWidth="md"
          actions={
            <>
              <Button startIcon={<CloseIcon />} variant="outlined" onClick={() => setDetail(null)}>
                Đóng
              </Button>
              {extraActions?.(detail, () => void reload(), () => setDetail(null))}
              {update ? (
                <Button
                  startIcon={<EditOutlinedIcon />}
                  variant="outlined"
                  onClick={() => {
                    openEdit(detail)
                  }}
                >
                  Sửa
                </Button>
              ) : null}
              {remove ? (
                <Button
                  startIcon={<DeleteIcon />}
                  color="error"
                  onClick={() => setDeleting(detail)}
                >
                  Xóa
                </Button>
              ) : null}
            </>
          }
        >
          <Box className="space-y-3">
            <Typography variant="body2" color="text.secondary">
              Xem thông tin đầy đủ và thao tác trực tiếp trên bản ghi này.
            </Typography>
            <Divider />
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {detailRows.map((item) => (
                <Paper
                  key={`${item.label}`}
                  variant="outlined"
                  className="p-3"
                  sx={{ borderRadius: 2, bgcolor: 'background.paper' }}
                >
                  <Typography variant="caption" color="text.secondary" className="block">
                    {item.label}
                  </Typography>
                  <Typography variant="body1" sx={{ fontWeight: 600, mt: 0.5, wordBreak: 'break-word' }}>
                    {item.value as ReactNode}
                  </Typography>
                </Paper>
              ))}
            </div>
            {renderDetail?.(detail)}
          </Box>
        </Modal>
      ) : null}

      {formOpen ? (
        <Modal
          title={editing ? `Sửa #${editing.id}` : 'Thêm mới'}
          icon={editing ? <EditOutlinedIcon color="primary" /> : <AddIcon color="primary" />}
          onClose={() => setFormOpen(false)}
          maxWidth="md"
          actions={
            <>
              <Button startIcon={<CloseIcon />} variant="outlined" onClick={() => setFormOpen(false)} disabled={busy}>
                Hủy
              </Button>
              <Button startIcon={<SaveIcon />} form="resource-form" type="submit" disabled={busy}>
                {busy ? 'Đang lưu...' : 'Lưu'}
              </Button>
            </>
          }
        >
          <Box component="form" id="resource-form" onSubmit={onSubmit} className="pt-1">
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {fields.map((f) =>
                f.type === 'checkbox' ? (
                  <FormControlLabel
                    key={f.name}
                    className="sm:col-span-2"
                    control={
                      <Checkbox
                        checked={Boolean(form[f.name])}
                        onChange={(e) => setForm({ ...form, [f.name]: e.target.checked })}
                      />
                    }
                    label={f.label}
                  />
                ) : f.type === 'select' ? (
                  <TextField
                    key={f.name}
                    select
                    label={f.label}
                    required={f.required}
                    value={String(form[f.name] ?? '')}
                    onChange={(e) =>
                      setForm({
                        ...form,
                        [f.name]:
                          e.target.value === ''
                            ? ''
                            : Number.isNaN(Number(e.target.value))
                              ? e.target.value
                              : Number(e.target.value),
                      })
                    }
                    fullWidth
                    size="small"
                  >
                    <MenuItem value="">-- Chọn --</MenuItem>
                    {f.options?.map((o) => (
                      <MenuItem key={o.value} value={o.value}>
                        {o.label}
                      </MenuItem>
                    ))}
                  </TextField>
                ) : (
                  <TextField
                    key={f.name}
                    label={f.label}
                    type={f.type === 'number' ? 'number' : 'text'}
                    multiline={f.type === 'textarea'}
                    minRows={f.type === 'textarea' ? 3 : undefined}
                    required={f.required}
                    placeholder={f.placeholder}
                    value={String(form[f.name] ?? '')}
                    onChange={(e) => setForm({ ...form, [f.name]: e.target.value })}
                    fullWidth
                    size="small"
                    className={f.type === 'textarea' ? 'sm:col-span-2' : undefined}
                  />
                ),
              )}
            </div>
          </Box>
        </Modal>
      ) : null}

      <ConfirmDialog
        open={!!deleting}
        title="Xác nhận xóa"
        message={deleting ? `Bạn chắc chắn muốn xóa bản ghi #${deleting.id}? Thao tác không hoàn tác.` : ''}
        confirmLabel="Xóa"
        danger
        loading={busy}
        onClose={() => setDeleting(null)}
        onConfirm={() => void confirmDelete()}
      />
    </Box>
  )
}
