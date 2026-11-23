import { useEffect, useRef, useState, type FormEvent } from 'react'
import {
  Avatar,
  Box,
  Button,
  CircularProgress,
  Divider,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import PersonIcon from '@mui/icons-material/Person'
import SaveIcon from '@mui/icons-material/Save'
import PhotoCameraIcon from '@mui/icons-material/PhotoCamera'
import LockResetIcon from '@mui/icons-material/LockReset'
import { storeApi } from '../../api/endpoints'
import { ApiError, mediaUrl } from '../../api/client'
import { useToast } from '../../components/ToastContext'
import { useAuth } from '../../auth/AuthContext'
import { Alert, Badge, PageHeader } from '../../components/ui'
import type { CustomerDto } from '../../types/api'

export function StoreProfile() {
  const toast = useToast()
  const { user } = useAuth()
  const fileRef = useRef<HTMLInputElement>(null)
  const [me, setMe] = useState<CustomerDto | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [form, setForm] = useState({
    name: '',
    contactName: '',
    phone: '',
    email: '',
    address: '',
    note: '',
    newPassword: '',
    confirmPassword: '',
  })

  async function load() {
    setError('')
    try {
      const data = await storeApi.me()
      setMe(data)
      setForm({
        name: data.name || '',
        contactName: data.contactName || '',
        phone: data.phone || '',
        email: data.email || '',
        address: data.address || '',
        note: data.note || '',
        newPassword: '',
        confirmPassword: '',
      })
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Không tải hồ sơ')
    }
  }

  useEffect(() => {
    void load()
  }, [])

  async function onSave(e: FormEvent) {
    e.preventDefault()
    if (form.newPassword && form.newPassword !== form.confirmPassword) {
      toast.error('Mật khẩu xác nhận không khớp')
      return
    }
    setBusy(true)
    try {
      const updated = await storeApi.updateMe({
        name: form.name.trim(),
        contactName: form.contactName.trim() || null,
        phone: form.phone.trim() || null,
        email: form.email.trim() || null,
        address: form.address.trim() || null,
        note: form.note.trim() || null,
        newPassword: form.newPassword || null,
      })
      setMe(updated)
      setForm((f) => ({ ...f, newPassword: '', confirmPassword: '' }))
      toast.success('Đã cập nhật hồ sơ')
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Cập nhật thất bại')
    } finally {
      setBusy(false)
    }
  }

  async function onAvatar(file: File) {
    setBusy(true)
    try {
      const updated = await storeApi.uploadAvatar(file)
      setMe(updated)
      toast.success('Đã cập nhật ảnh đại diện')
    } catch (err) {
      toast.error(err instanceof ApiError ? err.message : 'Upload avatar thất bại')
    } finally {
      setBusy(false)
    }
  }

  return (
    <Box>
      <PageHeader
        title="Tài khoản của tôi"
        subtitle="Hồ sơ khoa/phòng hoặc người nhận thuốc · ảnh đại diện · mật khẩu cổng nhà thuốc BV"
        icon={<PersonIcon color="primary" />}
      />

      {error ? <Alert>{error}</Alert> : null}

      {!me && !error ? (
        <Box className="flex justify-center py-10">
          <CircularProgress />
        </Box>
      ) : null}

      {me ? (
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-[280px_1fr]">
          <Paper className="p-4" sx={{ borderRadius: 3 }} variant="outlined">
            <Stack spacing={2} className="items-center text-center">
              <Avatar
                src={me.avatarUrl ? mediaUrl(me.avatarUrl) : undefined}
                sx={{ width: 112, height: 112, fontSize: 40 }}
              >
                {me.name?.[0] || user?.fullName?.[0] || '?'}
              </Avatar>
              <Box>
                <Typography variant="h6" sx={{ fontWeight: 700 }}>
                  {me.name}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  @{me.username || user?.username || '—'}
                </Typography>
              </Box>
              <Badge tone={me.isActive ? 'ok' : 'danger'}>
                {me.isActive ? 'Đang hoạt động' : 'Đã khóa'}
              </Badge>
              <Button
                startIcon={<PhotoCameraIcon />}
                variant="outlined"
                component="label"
                disabled={busy}
                fullWidth
              >
                Đổi ảnh đại diện
                <input
                  ref={fileRef}
                  type="file"
                  accept="image/*"
                  hidden
                  onChange={(e) => {
                    const file = e.target.files?.[0]
                    e.target.value = ''
                    if (file) void onAvatar(file)
                  }}
                />
              </Button>
              <Divider flexItem />
              <Stack spacing={0.5} className="w-full text-left">
                <Typography variant="caption" color="text.secondary">
                  Email đăng nhập
                </Typography>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {me.email || user?.email || '—'}
                </Typography>
                <Typography variant="caption" color="text.secondary" className="pt-2">
                  Vai trò
                </Typography>
                <Typography variant="body2" sx={{ fontWeight: 600 }}>
                  {me.roleName || 'Customer'}
                </Typography>
              </Stack>
            </Stack>
          </Paper>

          <Paper className="p-4" sx={{ borderRadius: 3 }} variant="outlined">
            <Typography variant="subtitle1" sx={{ fontWeight: 700, mb: 2 }}>
              Thông tin liên hệ
            </Typography>
            <Box component="form" onSubmit={onSave} className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <TextField
                label="Tên khách hàng / công ty"
                required
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                size="small"
                className="sm:col-span-2"
              />
              <TextField
                label="Người liên hệ"
                value={form.contactName}
                onChange={(e) => setForm({ ...form, contactName: e.target.value })}
                size="small"
              />
              <TextField
                label="Số điện thoại"
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
                size="small"
                placeholder="0912345678"
              />
              <TextField
                label="Email"
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                size="small"
                className="sm:col-span-2"
              />
              <TextField
                label="Địa chỉ"
                value={form.address}
                onChange={(e) => setForm({ ...form, address: e.target.value })}
                size="small"
                className="sm:col-span-2"
                multiline
                minRows={2}
              />
              <TextField
                label="Ghi chú"
                value={form.note}
                onChange={(e) => setForm({ ...form, note: e.target.value })}
                size="small"
                className="sm:col-span-2"
                multiline
                minRows={2}
              />

              <Divider className="sm:col-span-2" />
              <Stack direction="row" spacing={1} className="items-center sm:col-span-2">
                <LockResetIcon color="action" fontSize="small" />
                <Typography variant="subtitle2" sx={{ fontWeight: 700 }}>
                  Đổi mật khẩu (tuỳ chọn)
                </Typography>
              </Stack>
              <TextField
                label="Mật khẩu mới"
                type="password"
                value={form.newPassword}
                onChange={(e) => setForm({ ...form, newPassword: e.target.value })}
                size="small"
                helperText="Để trống nếu không đổi"
              />
              <TextField
                label="Xác nhận mật khẩu"
                type="password"
                value={form.confirmPassword}
                onChange={(e) => setForm({ ...form, confirmPassword: e.target.value })}
                size="small"
              />

              <Stack direction="row" spacing={1.5} className="justify-end sm:col-span-2 pt-1">
                <Button variant="outlined" disabled={busy} onClick={() => void load()}>
                  Hoàn tác
                </Button>
                <Button type="submit" startIcon={<SaveIcon />} variant="contained" disabled={busy}>
                  Lưu thay đổi
                </Button>
              </Stack>
            </Box>
          </Paper>
        </div>
      ) : null}
    </Box>
  )
}
