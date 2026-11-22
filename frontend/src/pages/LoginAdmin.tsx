import { useState, type FormEvent } from 'react'
import { Link as RouterLink, Navigate, useNavigate } from 'react-router-dom'
import {
  Box,
  Button,
  Card,
  CardContent,
  Link,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import WarehouseIcon from '@mui/icons-material/Warehouse'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Alert } from '../components/ui'

export function LoginAdmin() {
  const { loginAdmin, isAdmin } = useAuth()
  const nav = useNavigate()
  const [username, setUsername] = useState('admin')
  const [password, setPassword] = useState('123456')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

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
    <Box className="flex min-h-screen items-center justify-center bg-gradient-to-br from-slate-100 via-blue-50 to-teal-50 p-4">
      <Card className="w-full max-w-md shadow-lg">
        <CardContent className="p-6 sm:p-8">
          <Stack spacing={2} className="mb-4 items-center">
            <WarehouseIcon color="primary" sx={{ fontSize: 40 }} />
            <Typography variant="h5" sx={{ fontWeight: 700 }}>
              Admin Portal
            </Typography>
            <Typography variant="body2" color="text.secondary" align="center">
              Đăng nhập quản trị / quản lý / nhân viên kho
            </Typography>
          </Stack>
          {error ? <Alert>{error}</Alert> : null}
          <Box component="form" onSubmit={onSubmit} className="space-y-3">
            <TextField
              label="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              fullWidth
            />
            <TextField
              label="Password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              fullWidth
            />
            <Button type="submit" fullWidth disabled={loading} size="large">
              {loading ? 'Đang đăng nhập...' : 'Đăng nhập'}
            </Button>
          </Box>
          <Typography variant="body2" color="text.secondary" align="center" className="mt-4">
            Bạn là khách hàng?{' '}
            <Link component={RouterLink} to="/store/login">
              Đăng nhập Store
            </Link>
          </Typography>
        </CardContent>
      </Card>
    </Box>
  )
}
