import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { Alert, Snackbar } from '@mui/material'

type Severity = 'success' | 'error' | 'info' | 'warning'

interface ToastState {
  open: boolean
  message: string
  severity: Severity
}

interface ToastApi {
  success: (message: string) => void
  error: (message: string) => void
  info: (message: string) => void
  warning: (message: string) => void
}

const ToastContext = createContext<ToastApi | null>(null)

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toast, setToast] = useState<ToastState>({ open: false, message: '', severity: 'info' })

  const show = useCallback((message: string, severity: Severity) => {
    setToast({ open: true, message, severity })
  }, [])

  const api = useMemo<ToastApi>(
    () => ({
      success: (m) => show(m, 'success'),
      error: (m) => show(m, 'error'),
      info: (m) => show(m, 'info'),
      warning: (m) => show(m, 'warning'),
    }),
    [show],
  )

  return (
    <ToastContext.Provider value={api}>
      {children}
      <Snackbar
        open={toast.open}
        autoHideDuration={3200}
        onClose={() => setToast((t) => ({ ...t, open: false }))}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert
          severity={toast.severity}
          variant="filled"
          onClose={() => setToast((t) => ({ ...t, open: false }))}
          sx={{ width: '100%', boxShadow: 3 }}
        >
          {toast.message}
        </Alert>
      </Snackbar>
    </ToastContext.Provider>
  )
}

export function useToast() {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast must be used within ToastProvider')
  return ctx
}
