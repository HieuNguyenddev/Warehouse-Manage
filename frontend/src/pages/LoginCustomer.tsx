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
import StorefrontIcon from '@mui/icons-material/Storefront'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import { Alert } from '../components/ui'

export function LoginCustomer() {
  const { loginCustomer, isCustomer } = useAuth()
  const nav = useNavigate()
  const [username, setUsername] = useState('customer1')
  const [password, setPassword] = useState('123456')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  if (isCustomer) return <Navigate to="/store" replace />

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError('')
    try {
      await loginCustomer(username, password)
      nav('/store')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Đăng nhập thất bại')
    } finally {
      setLoading(false)
    }
  }

  return (
    <Box className="flex min-h-screen items-center justify-center bg-gradient-to-br from-teal-50 via-slate-50 to-blue-50 p-4">
      <Card className="w-full max-w-md shadow-lg">
        <CardContent className="p-6 sm:p-8">
          <Stack spacing={2} className="mb-4 items-center">
            <StorefrontIcon color="secondary" sx={{ fontSize: 40 }} />
            <Typography variant="h5" sx={{ fontWeight: 700 }}>
              Nhà thuốc BV
            </Typography>
            <Typography variant="body2" color="text.secondary" align="center">
              Đăng nhập khoa/phòng hoặc người nhận thuốc
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
            <Button type="submit" fullWidth disabled={loading} size="large" color="secondary">
              {loading ? 'Đang đăng nhập...' : 'Đăng nhập'}
            </Button>
          </Box>
          <Typography variant="body2" color="text.secondary" align="center" className="mt-4">
            Nhân viên kho dược?{' '}
            <Link component={RouterLink} to="/admin/login">
              Đăng nhập Kho Dược
            </Link>
          </Typography>
        </CardContent>
      </Card>
    </Box>
  )
}
