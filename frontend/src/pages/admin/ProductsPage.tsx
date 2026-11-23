import { useEffect, useRef, useState, type FormEvent } from 'react'
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
import Inventory2Icon from '@mui/icons-material/Inventory2'
import AddIcon from '@mui/icons-material/Add'
import RefreshIcon from '@mui/icons-material/Refresh'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import DeleteIcon from '@mui/icons-material/Delete'
import ImageIcon from '@mui/icons-material/Image'
import CloudUploadIcon from '@mui/icons-material/CloudUpload'
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined'
import CloseIcon from '@mui/icons-material/Close'
import StarIcon from '@mui/icons-material/Star'
import StarBorderIcon from '@mui/icons-material/StarBorder'
import SwapHorizIcon from '@mui/icons-material/SwapHoriz'
import { categoriesApi, productsApi, unitsApi } from '../../api/endpoints'
import { ApiError, mediaUrl } from '../../api/client'
import { useToast } from '../../components/ToastContext'
import { Alert, Badge, ConfirmDialog, Modal, PageHeader, Pager, money } from '../../components/ui'
import type { CategoryDto, ProductDto, ProductImageDto, UnitDto } from '../../types/api'

function ProductThumb({
  src,
  size = 72,
  label,
}: {
  src?: string | null
  size?: number
  label?: string | null
}) {
  const [broken, setBroken] = useState(false)
  const url = mediaUrl(src)

  return (
    <Box
      className="relative overflow-hidden rounded-lg bg-slate-100"
      sx={{ width: size, height: size, flexShrink: 0 }}
    >
      {!broken && url ? (
        <img
          src={url}
          alt=""
          width={size}
          height={size}
          className="h-full w-full object-cover"
          onError={() => setBroken(true)}
        />
      ) : (
        <Box className="flex h-full w-full flex-col items-center justify-center gap-0.5 text-slate-400">
          <ImageIcon fontSize="small" color="disabled" />
          <Typography variant="caption" color="text.disabled" sx={{ fontSize: 10 }}>
            Không có ảnh
          </Typography>
        </Box>
      )}
      {label ? (
        <Box
          className="absolute left-1 top-1 rounded px-1 text-[10px] font-semibold"
          sx={{ bgcolor: 'rgba(255,255,255,0.92)', color: 'warning.dark', lineHeight: 1.4 }}
        >
          {label}
        </Box>
      ) : null}
    </Box>
  )
}

function primaryImageId(images?: ProductImageDto[] | null) {
  if (!images?.length) return null
  const primary = images.find((x) => x.isPrimary)
  return primary?.id ?? images[0]?.id ?? null
}

const empty = {
  sku: '',
  name: '',
  description: '',
  price: 0,
  minStock: 0,
  categoryId: 0,
  unitId: 0,
  isActive: true,
}

export function ProductsPage() {
  const toast = useToast()
  const fileRef = useRef<HTMLInputElement>(null)
  const [rows, setRows] = useState<ProductDto[]>([])
  const [cats, setCats] = useState<CategoryDto[]>([])
  const [units, setUnits] = useState<UnitDto[]>([])
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [error, setError] = useState('')
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<ProductDto | null>(null)
  const [form, setForm] = useState({ ...empty })
  const [pendingFiles, setPendingFiles] = useState<File[]>([])
  const [pendingPreviews, setPendingPreviews] = useState<string[]>([])
  const [imgBusy, setImgBusy] = useState(false)
  const [deleting, setDeleting] = useState<ProductDto | null>(null)
  const [viewing, setViewing] = useState<ProductDto | null>(null)

  async function reload() {
    setError('')
    try {
      const data = await productsApi.list({ search, pageNumber: page, pageSize: 10 })
      setRows(data.items)
      setTotalPages(data.totalPages || 1)
      return data.items
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Lỗi tải sản phẩm')
      return [] as ProductDto[]
    }
  }

  function clearPendingFiles() {
    pendingPreviews.forEach((url) => URL.revokeObjectURL(url))
    setPendingFiles([])
    setPendingPreviews([])
    if (fileRef.current) fileRef.current.value = ''
  }

  function openCreate() {
    clearPendingFiles()
    setEditing(null)
    setForm({
      ...empty,
      categoryId: cats[0]?.id || 0,
      unitId: units[0]?.id || 0,
    })
    setOpen(true)
  }

  function openEdit(product: ProductDto) {
    clearPendingFiles()
    setEditing(product)
    setForm({
      sku: product.sku,
      name: product.name,
      description: product.description || '',
      price: product.price,
      minStock: product.minStock,
      categoryId: product.categoryId,
      unitId: product.unitId,
      isActive: product.isActive,
    })
    setOpen(true)
  }

  async function openView(product: ProductDto) {
    setViewing(product)
    try {
      const fresh = await productsApi.get(product.id)
      setViewing(fresh)
      setRows((list) => list.map((r) => (r.id === fresh.id ? fresh : r)))
    } catch {
      // giữ bản đang có nếu reload lỗi
    }
  }

  function syncProduct(updated: ProductDto) {
    setEditing((cur) => (cur?.id === updated.id ? updated : cur))
    setViewing((cur) => (cur?.id === updated.id ? updated : cur))
    setRows((list) => list.map((r) => (r.id === updated.id ? updated : r)))
  }

  useEffect(() => {
    void (async () => {
      const [c, u] = await Promise.all([
        categoriesApi.list({ pageNumber: 1, pageSize: 100 }),
        unitsApi.list({ pageNumber: 1, pageSize: 100 }),
      ])
      setCats(c.items)
      setUnits(u.items)
    })()
  }, [])

  useEffect(() => {
    void reload()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [search, page])

  useEffect(() => {
    return () => {
      pendingPreviews.forEach((url) => URL.revokeObjectURL(url))
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function onPickFiles(files: FileList | File[] | null) {
    const list = Array.from(files || []).filter((f) => f.type.startsWith('image/'))
    if (!list.length) return
    const previews = list.map((f) => URL.createObjectURL(f))
    setPendingFiles((prev) => [...prev, ...list])
    setPendingPreviews((prev) => [...prev, ...previews])
  }

  async function uploadToProduct(productId: number, files: File[]) {
    if (!files.length) return null
    setImgBusy(true)
    try {
      const updated = await productsApi.uploadImages(productId, files)
      syncProduct(updated)
      return updated
    } finally {
      setImgBusy(false)
    }
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    try {
      const body = { ...form, price: Number(form.price), minStock: Number(form.minStock) }
      if (editing) {
        const updated = await productsApi.update(editing.id, body)
        let finalProduct = updated
        if (pendingFiles.length) {
          finalProduct = (await uploadToProduct(editing.id, pendingFiles)) || updated
          clearPendingFiles()
        } else {
          // giữ images hiện có từ editing nếu update không trả images đầy đủ
          finalProduct = { ...updated, images: updated.images?.length ? updated.images : editing.images }
          syncProduct(finalProduct)
        }
        toast.success('Cập nhật sản phẩm thành công')
        setEditing(finalProduct)
        await reload()
      } else {
        const created = await productsApi.create(body)
        if (pendingFiles.length) {
          await uploadToProduct(created.id, pendingFiles)
          clearPendingFiles()
        }
        toast.success('Thêm sản phẩm thành công')
        setOpen(false)
        await reload()
      }
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Lưu thất bại'
      setError(msg)
      toast.error(msg)
    }
  }

  async function setPrimary(imageId: number) {
    if (!editing) return
    setImgBusy(true)
    try {
      const updated = await productsApi.setPrimary(editing.id, imageId)
      syncProduct(updated)
      toast.success('Đã đặt ảnh chính')
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Không đặt được ảnh chính')
    } finally {
      setImgBusy(false)
    }
  }

  async function removeImage(imageId: number) {
    if (!editing) return
    setImgBusy(true)
    try {
      const updated = await productsApi.deleteImage(editing.id, imageId)
      syncProduct(updated)
      toast.success('Đã xóa ảnh')
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Xóa ảnh thất bại')
    } finally {
      setImgBusy(false)
    }
  }

  async function replaceImage(imageId: number, file: File) {
    if (!editing) return
    setImgBusy(true)
    try {
      const updated = await productsApi.replaceImage(editing.id, imageId, file)
      syncProduct(updated)
      toast.success('Đã thay ảnh')
      await reload()
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Thay ảnh thất bại')
    } finally {
      setImgBusy(false)
    }
  }

  async function uploadNow() {
    if (!editing || !pendingFiles.length) return
    try {
      await uploadToProduct(editing.id, pendingFiles)
      clearPendingFiles()
      toast.success('Đã cập nhật ảnh')
      await reload()
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Upload ảnh thất bại')
    }
  }

  return (
    <Box>
      <PageHeader
        title="Thuốc / vật tư"
        subtitle="Danh mục thuốc & vật tư kho dược · bấm dòng để xem · sửa kèm ảnh"
        icon={<Inventory2Icon color="primary" />}
        actions={
          <Button startIcon={<AddIcon />} onClick={openCreate}>
            Thêm SP
          </Button>
        }
      />
      <Paper className="p-4" sx={{ borderRadius: 3 }}>
        {error ? <Alert>{error}</Alert> : null}
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} className="mb-3">
          <TextField
            size="small"
            placeholder="Tìm SKU / tên"
            value={search}
            onChange={(e) => {
              setPage(1)
              setSearch(e.target.value)
            }}
            className="sm:max-w-xs"
            fullWidth
          />
          <Button startIcon={<RefreshIcon />} variant="outlined" onClick={() => void reload()}>
            Làm mới
          </Button>
        </Stack>
        <TableContainer>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Ảnh</TableCell>
                <TableCell>SKU</TableCell>
                <TableCell>Tên</TableCell>
                <TableCell>Giá</TableCell>
                <TableCell>Danh mục</TableCell>
                <TableCell>ĐVT</TableCell>
                <TableCell>TT</TableCell>
                <TableCell align="right">Thao tác</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.map((r) => {
                const img = r.images?.find((x) => x.isPrimary) || r.images?.[0]
                return (
                  <TableRow
                    key={r.id}
                    hover
                    selected={viewing?.id === r.id}
                    sx={{ cursor: 'pointer' }}
                    onClick={() => void openView(r)}
                  >
                    <TableCell>
                      {img ? <ProductThumb src={img.imageUrl} size={40} /> : '—'}
                    </TableCell>
                    <TableCell>{r.sku}</TableCell>
                    <TableCell>{r.name}</TableCell>
                    <TableCell>{money(r.price)}</TableCell>
                    <TableCell>{r.categoryName}</TableCell>
                    <TableCell>{r.unitSymbol}</TableCell>
                    <TableCell>
                      <Badge tone={r.isActive ? 'ok' : 'danger'}>{r.isActive ? 'Active' : 'Off'}</Badge>
                    </TableCell>
                    <TableCell align="right" onClick={(e) => e.stopPropagation()}>
                      <Stack direction="row" spacing={0.5} className="justify-end">
                        <Tooltip title="Chi tiết">
                          <IconButton size="small" onClick={() => void openView(r)}>
                            <InfoOutlinedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Sửa + ảnh">
                          <IconButton size="small" color="primary" onClick={() => openEdit(r)}>
                            <EditOutlinedIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                        <Tooltip title="Xóa">
                          <IconButton size="small" color="error" onClick={() => setDeleting(r)}>
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      </Stack>
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </TableContainer>
        <Pager page={page} totalPages={totalPages} onChange={setPage} />
      </Paper>

      {open ? (
        <Modal
          title={editing ? `Sửa · ${editing.sku}` : 'Thêm sản phẩm'}
          icon={<Inventory2Icon color="primary" />}
          onClose={() => {
            clearPendingFiles()
            setOpen(false)
          }}
          maxWidth="md"
          actions={
            <>
              <Button
                variant="outlined"
                startIcon={<CloseIcon />}
                onClick={() => {
                  clearPendingFiles()
                  setOpen(false)
                }}
              >
                {editing ? 'Đóng' : 'Hủy'}
              </Button>
              <Button type="submit" form="product-form" variant="contained" disabled={imgBusy}>
                {editing ? 'Lưu thông tin' : 'Tạo sản phẩm'}
              </Button>
            </>
          }
        >
          <Stack spacing={2.5} component="form" id="product-form" onSubmit={onSubmit}>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <TextField
                label="SKU"
                required
                value={form.sku}
                onChange={(e) => setForm({ ...form, sku: e.target.value })}
                size="small"
              />
              <TextField
                label="Tên"
                required
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                size="small"
              />
              <TextField
                label="Giá"
                type="number"
                value={form.price}
                onChange={(e) => setForm({ ...form, price: Number(e.target.value) })}
                size="small"
              />
              <TextField
                label="Min stock"
                type="number"
                value={form.minStock}
                onChange={(e) => setForm({ ...form, minStock: Number(e.target.value) })}
                size="small"
              />
              <TextField
                select
                label="Category"
                value={form.categoryId}
                onChange={(e) => setForm({ ...form, categoryId: Number(e.target.value) })}
                size="small"
              >
                {cats.map((c) => (
                  <MenuItem key={c.id} value={c.id}>
                    {c.name}
                  </MenuItem>
                ))}
              </TextField>
              <TextField
                select
                label="Unit"
                value={form.unitId}
                onChange={(e) => setForm({ ...form, unitId: Number(e.target.value) })}
                size="small"
              >
                {units.map((u) => (
                  <MenuItem key={u.id} value={u.id}>
                    {u.name} ({u.symbol})
                  </MenuItem>
                ))}
              </TextField>
              <TextField
                label="Mô tả"
                multiline
                minRows={3}
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
                className="sm:col-span-2"
                size="small"
              />
              <FormControlLabel
                className="sm:col-span-2"
                control={
                  <Checkbox
                    checked={form.isActive}
                    onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                  />
                }
                label="Active"
              />
            </div>

            <Paper variant="outlined" className="p-3" sx={{ borderRadius: 2 }}>
              <Stack direction="row" spacing={1} className="mb-2 items-center justify-between">
                <Stack direction="row" spacing={1} className="items-center">
                  <ImageIcon color="secondary" fontSize="small" />
                  <Typography sx={{ fontWeight: 700 }}>
                    {editing ? 'Hình ảnh sản phẩm' : 'Ảnh kèm theo (upload sau khi tạo)'}
                  </Typography>
                </Stack>
                <Stack direction="row" spacing={1}>
                  <Button
                    startIcon={<CloudUploadIcon />}
                    variant="outlined"
                    size="small"
                    component="label"
                    disabled={imgBusy}
                  >
                    Chọn ảnh
                    <input
                      ref={fileRef}
                      type="file"
                      accept="image/*"
                      multiple
                      hidden
                      onChange={(e) => {
                        onPickFiles(e.target.files)
                        e.target.value = ''
                      }}
                    />
                  </Button>
                  {editing && pendingFiles.length ? (
                    <Button
                      size="small"
                      variant="contained"
                      color="secondary"
                      disabled={imgBusy}
                      onClick={() => void uploadNow()}
                    >
                      Upload ngay ({pendingFiles.length})
                    </Button>
                  ) : null}
                </Stack>
              </Stack>

              {editing?.images?.length ? (
                <div className="mb-3 grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-4">
                  {(() => {
                    const mainId = primaryImageId(editing.images)
                    return [...editing.images]
                      .sort(
                        (a, b) =>
                          Number(b.id === mainId) - Number(a.id === mainId) ||
                          a.sortOrder - b.sortOrder,
                      )
                      .map((img) => {
                        const isMain = img.id === mainId
                        return (
                          <Paper
                            key={img.id}
                            variant="outlined"
                            className="overflow-hidden"
                            sx={{ borderRadius: 2 }}
                          >
                            <Box className="flex justify-center bg-slate-50 p-2">
                              <ProductThumb
                                src={img.imageUrl}
                                size={112}
                                label={isMain ? 'Chính' : 'Phụ'}
                              />
                            </Box>
                            <Stack spacing={0.75} className="p-1.5">
                              {isMain ? <Badge tone="ok">Ảnh chính</Badge> : <Badge>Ảnh phụ</Badge>}
                              <Stack direction="row" spacing={0.5} className="flex-wrap">
                                <Tooltip title={isMain ? 'Đang là ảnh chính' : 'Đặt làm ảnh chính'}>
                                  <span>
                                    <IconButton
                                      size="small"
                                      color="warning"
                                      disabled={imgBusy || isMain}
                                      onClick={() => void setPrimary(img.id)}
                                    >
                                      {isMain ? (
                                        <StarIcon fontSize="small" />
                                      ) : (
                                        <StarBorderIcon fontSize="small" />
                                      )}
                                    </IconButton>
                                  </span>
                                </Tooltip>
                                <Tooltip title={isMain ? 'Thay ảnh chính' : 'Thay ảnh phụ'}>
                                  <span>
                                    <IconButton
                                      size="small"
                                      color="primary"
                                      disabled={imgBusy}
                                      component="label"
                                    >
                                      <SwapHorizIcon fontSize="small" />
                                      <input
                                        type="file"
                                        accept="image/*"
                                        hidden
                                        onChange={(e) => {
                                          const file = e.target.files?.[0]
                                          e.target.value = ''
                                          if (file) void replaceImage(img.id, file)
                                        }}
                                      />
                                    </IconButton>
                                  </span>
                                </Tooltip>
                                <Tooltip title="Xóa ảnh">
                                  <IconButton
                                    size="small"
                                    color="error"
                                    disabled={imgBusy}
                                    onClick={() => void removeImage(img.id)}
                                  >
                                    <DeleteIcon fontSize="small" />
                                  </IconButton>
                                </Tooltip>
                              </Stack>
                            </Stack>
                          </Paper>
                        )
                      })
                  })()}
                </div>
              ) : (
                <Typography variant="body2" color="text.secondary" className="mb-2">
                  {editing ? 'Chưa có ảnh. Chọn ảnh rồi bấm Upload ngay hoặc Lưu thông tin.' : 'Có thể chọn ảnh trước; hệ thống sẽ upload sau khi tạo SP.'}
                </Typography>
              )}

              {pendingPreviews.length ? (
                <Box>
                  <Typography variant="caption" color="text.secondary" className="mb-1 block">
                    Ảnh chờ upload ({pendingPreviews.length})
                  </Typography>
                  <div className="flex flex-wrap gap-2">
                    {pendingPreviews.map((src, idx) => (
                      <Paper key={src} variant="outlined" className="relative overflow-hidden" sx={{ borderRadius: 2 }}>
                        <img src={src} alt="" width={72} height={72} className="object-cover" />
                        <IconButton
                          size="small"
                          color="error"
                          sx={{ position: 'absolute', top: 0, right: 0, bgcolor: 'background.paper' }}
                          onClick={() => {
                            URL.revokeObjectURL(src)
                            setPendingFiles((files) => files.filter((_, i) => i !== idx))
                            setPendingPreviews((urls) => urls.filter((_, i) => i !== idx))
                          }}
                        >
                          <CloseIcon fontSize="small" />
                        </IconButton>
                      </Paper>
                    ))}
                  </div>
                </Box>
              ) : null}
            </Paper>
          </Stack>
        </Modal>
      ) : null}

      {viewing ? (
        <Modal
          title={`${viewing.sku} · ${viewing.name}`}
          icon={<InfoOutlinedIcon color="primary" />}
          onClose={() => setViewing(null)}
          maxWidth="md"
          actions={
            <>
              <Button startIcon={<CloseIcon />} variant="outlined" onClick={() => setViewing(null)}>
                Đóng
              </Button>
              <Button
                startIcon={<EditOutlinedIcon />}
                onClick={() => {
                  openEdit(viewing)
                }}
              >
                Sửa / Ảnh
              </Button>
              <Button startIcon={<DeleteIcon />} color="error" onClick={() => setDeleting(viewing)}>
                Xóa
              </Button>
            </>
          }
        >
          <Stack spacing={2}>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              {[
                ['SKU', viewing.sku],
                ['Tên', viewing.name],
                ['Giá', money(viewing.price)],
                ['Min stock', viewing.minStock],
                ['Danh mục', viewing.categoryName],
                ['ĐVT', `${viewing.unitName || ''} (${viewing.unitSymbol || ''})`],
              ].map(([label, value]) => (
                <Paper key={String(label)} variant="outlined" className="p-3" sx={{ borderRadius: 2 }}>
                  <Typography variant="caption" color="text.secondary">
                    {label}
                  </Typography>
                  <Typography sx={{ fontWeight: 600, wordBreak: 'break-word' }}>{value}</Typography>
                </Paper>
              ))}
              <Paper variant="outlined" className="p-3 sm:col-span-2" sx={{ borderRadius: 2 }}>
                <Typography variant="caption" color="text.secondary">
                  Trạng thái
                </Typography>
                <Box className="mt-1">
                  <Badge tone={viewing.isActive ? 'ok' : 'danger'}>
                    {viewing.isActive ? 'Active' : 'Off'}
                  </Badge>
                </Box>
              </Paper>
              <Paper variant="outlined" className="p-3 sm:col-span-2" sx={{ borderRadius: 2 }}>
                <Typography variant="caption" color="text.secondary">
                  Mô tả
                </Typography>
                <Typography sx={{ mt: 0.5 }}>{viewing.description || '—'}</Typography>
              </Paper>
            </div>

            <Paper variant="outlined" className="p-3" sx={{ borderRadius: 2 }}>
              <Typography variant="subtitle2" sx={{ fontWeight: 700, mb: 1.5 }}>
                Hình ảnh
              </Typography>
              {viewing.images?.length ? (
                <div className="flex flex-wrap gap-2">
                  {(() => {
                    const mainId = primaryImageId(viewing.images)
                    return viewing.images.map((img) => (
                      <ProductThumb
                        key={img.id}
                        src={img.imageUrl}
                        size={88}
                        label={img.id === mainId ? 'Chính' : 'Phụ'}
                      />
                    ))
                  })()}
                </div>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  Chưa có ảnh — mở Sửa / Ảnh để thêm hoặc thay ảnh.
                </Typography>
              )}
            </Paper>
          </Stack>
        </Modal>
      ) : null}

      <ConfirmDialog
        open={!!deleting}
        title="Xóa sản phẩm"
        message={deleting ? `Xóa sản phẩm ${deleting.sku} - ${deleting.name}?` : ''}
        confirmLabel="Xóa"
        danger
        onClose={() => setDeleting(null)}
        onConfirm={async () => {
          if (!deleting) return
          try {
            await productsApi.remove(deleting.id)
            toast.success('Đã xóa sản phẩm')
            setDeleting(null)
            setViewing(null)
            if (editing?.id === deleting.id) {
              setOpen(false)
              setEditing(null)
            }
            await reload()
          } catch (err) {
            toast.error(err instanceof ApiError ? err.message : 'Xóa thất bại')
          }
        }}
      />
    </Box>
  )
}
