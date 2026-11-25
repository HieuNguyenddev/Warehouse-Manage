import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import {
  Box,
  Button,
  IconButton,
  TextField,
} from '@mui/material'
import VisibilityRoundedIcon from '@mui/icons-material/VisibilityRounded'
import VisibilityOffRoundedIcon from '@mui/icons-material/VisibilityOffRounded'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { AuthShell } from '../components/AuthShell'
import { Alert } from '../components/ui'

export function LoginAdmin() {
  const { loginAdmin, isAdmin } = useAuth()
  const nav = useNavigate()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const [showPassword, setShowPassword] = useState(false)

  if (isAdmin) return <Navigate to="/admin" replace />

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError('')
    try {
      await loginAdmin(username, password)
      nav('/admin')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Đăng nhập thất bại')
    } finally {
      setLoading(false)
    }
  }

  return (
    <AuthShell
      portal="admin"
      headline="Quản lý kho dược, bắt đầu tại đây."
      description="Một không gian tập trung để nhân viên theo dõi hàng hóa và vận hành kho thuốc bệnh viện."
      switchPrompt="Bạn là khách hàng?"
      switchLabel="Vào cổng nhà thuốc"
      switchTo="/store/login"
    >
      {error ? <Alert>{error}</Alert> : null}
      <Box component="form" onSubmit={onSubmit} className="auth-form">
        <TextField
          label="Tên đăng nhập"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          autoComplete="username"
          required
          fullWidth
        />
        <div className="auth-password-field">
          <TextField
            label="Mật khẩu"
            type={showPassword ? 'text' : 'password'}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
            required
            fullWidth
          />
          <IconButton
            className="auth-password-toggle"
            type="button"
            aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
            onClick={() => setShowPassword((visible) => !visible)}
            edge="end"
          >
            {showPassword ? <VisibilityOffRoundedIcon /> : <VisibilityRoundedIcon />}
          </IconButton>
        </div>
        <Button type="submit" fullWidth disabled={loading} className="auth-submit" size="large">
          {loading ? 'Đang đăng nhập...' : 'Đăng nhập'}
        </Button>
      </Box>
    </AuthShell>
  )
}
