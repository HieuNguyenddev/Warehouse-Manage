import { forwardRef, type ReactElement, type ReactNode, type Ref } from 'react'
import {
  Alert as MuiAlert,
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Slide,
  Typography,
} from '@mui/material'
import type { TransitionProps } from '@mui/material/transitions'
import CloseIcon from '@mui/icons-material/Close'
import WarningAmberRoundedIcon from '@mui/icons-material/WarningAmberRounded'

const SlideUp = forwardRef(function SlideUp(
  props: TransitionProps & { children: ReactElement },
  ref: Ref<unknown>,
) {
  return <Slide direction="up" ref={ref} {...props} />
})

export function PageHeader({
  title,
  subtitle,
  actions,
  icon,
}: {
  title: string
  subtitle?: string
  actions?: ReactNode
  icon?: ReactNode
}) {
  return (
    <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex items-start gap-3">
        {icon ? (
          <Box className="mt-0.5 rounded-xl bg-blue-50 p-2 text-blue-700 shadow-sm">{icon}</Box>
        ) : null}
        <Box>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            {title}
          </Typography>
          {subtitle ? (
            <Typography variant="body2" color="text.secondary" className="mt-0.5">
              {subtitle}
            </Typography>
          ) : null}
        </Box>
      </div>
      {actions}
    </div>
  )
}

export function Alert({ children, ok }: { children: ReactNode; ok?: boolean }) {
  return (
    <MuiAlert severity={ok ? 'success' : 'error'} className="mb-3" variant="outlined">
      {children}
    </MuiAlert>
  )
}

export function Modal({
  title,
  children,
  onClose,
  maxWidth = 'sm',
  icon,
  actions,
}: {
  title: string
  children: ReactNode
  onClose: () => void
  maxWidth?: 'xs' | 'sm' | 'md' | 'lg'
  icon?: ReactNode
  actions?: ReactNode
}) {
  return (
    <Dialog
      open
      onClose={onClose}
      fullWidth
      maxWidth={maxWidth}
      slots={{ transition: SlideUp }}
      slotProps={{
        paper: {
          sx: {
            borderRadius: 3,
            overflow: 'hidden',
            maxHeight: '92vh',
            display: 'flex',
            flexDirection: 'column',
            boxShadow: '0 20px 50px rgba(15, 23, 42, 0.18)',
          },
        },
      }}
    >
      <DialogTitle className="flex items-center gap-2 pr-12" sx={{ flexShrink: 0 }}>
        {icon}
        <Typography component="span" variant="h6" sx={{ fontWeight: 700 }}>
          {title}
        </Typography>
        <IconButton
          aria-label="Đóng"
          onClick={onClose}
          size="small"
          sx={{ position: 'absolute', right: 12, top: 12 }}
        >
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>
      <DialogContent
        dividers
        sx={{
          bgcolor: 'grey.50',
          overflowY: 'auto',
          overflowX: 'hidden',
          flex: '1 1 auto',
          // pt đủ cho floating label OutlinedInput không đè nhau
          pt: 2.5,
        }}
      >
        {children}
      </DialogContent>
      {actions ? (
        <DialogActions sx={{ px: 3, py: 2, flexShrink: 0, gap: 1, flexWrap: 'wrap' }}>
          {actions}
        </DialogActions>
      ) : null}
    </Dialog>
  )
}

export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Xác nhận',
  danger,
  loading,
  onConfirm,
  onClose,
}: {
  open: boolean
  title: string
  message: string
  confirmLabel?: string
  danger?: boolean
  loading?: boolean
  onConfirm: () => void
  onClose: () => void
}) {
  if (!open) return null
  return (
    <Dialog
      open
      onClose={onClose}
      maxWidth="xs"
      fullWidth
      slots={{ transition: SlideUp }}
      slotProps={{ paper: { sx: { borderRadius: 3 } } }}
    >
      <DialogTitle className="flex items-center gap-2">
        <WarningAmberRoundedIcon color={danger ? 'error' : 'warning'} />
        {title}
      </DialogTitle>
      <DialogContent>
        <Typography color="text.secondary">{message}</Typography>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button variant="outlined" onClick={onClose} disabled={loading}>
          Hủy
        </Button>
        <Button color={danger ? 'error' : 'primary'} onClick={onConfirm} disabled={loading}>
          {loading ? 'Đang xử lý...' : confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  )
}

export function Badge({
  children,
  tone,
}: {
  children: ReactNode
  tone?: 'ok' | 'warn' | 'danger'
}) {
  const color = tone === 'ok' ? 'success' : tone === 'warn' ? 'warning' : tone === 'danger' ? 'error' : 'default'
  return <Chip size="small" label={children} color={color} variant={tone ? 'filled' : 'outlined'} />
}

export function Pager({
  page,
  totalPages,
  onChange,
}: {
  page: number
  totalPages: number
  onChange: (p: number) => void
}) {
  return (
    <div className="mt-3 flex items-center justify-end gap-3">
      <Button size="small" variant="outlined" disabled={page <= 1} onClick={() => onChange(page - 1)}>
        Trước
      </Button>
      <Typography variant="body2" color="text.secondary">
        Trang {page}/{Math.max(totalPages, 1)}
      </Typography>
      <Button
        size="small"
        variant="outlined"
        disabled={page >= totalPages}
        onClick={() => onChange(page + 1)}
      >
        Sau
      </Button>
    </div>
  )
}

export function money(n: number) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(n)
}

/** Hiển thị ngày giờ cụ thể theo locale VN */
export function formatDateTime(value?: string | null) {
  if (!value) return '—'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return String(value)
  return d.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  })
}

/** Giá trị cho input datetime-local (giờ máy local) */
export function toDateTimeLocalValue(date = new Date()) {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`
}

/** datetime-local → ISO gửi API */
export function fromDateTimeLocal(value: string) {
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return new Date().toISOString()
  return d.toISOString()
}

export function statusTone(status: string): 'ok' | 'warn' | 'danger' | undefined {
  if (status === 'Confirmed') return 'ok'
  if (status === 'Draft') return 'warn'
  if (status === 'Cancelled') return 'danger'
  return undefined
}
