import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { CssBaseline, ThemeProvider } from '@mui/material'
import { useMemo } from 'react'
import { AuthProvider } from './auth/AuthContext'
import { ToastProvider } from './components/ToastContext'
import { AppearanceProvider, useAppearance } from './components/AppearanceContext'
import { createAppTheme } from './theme'
import { AdminLayout } from './layouts/AdminLayout'
import { CustomerLayout } from './layouts/CustomerLayout'
import { LoginAdmin } from './pages/LoginAdmin'
import { LoginCustomer } from './pages/LoginCustomer'
import { AdminDashboard } from './pages/admin/Dashboard'
import {
  CategoriesPage,
  RolesPage,
  SuppliersPage,
  UnitsPage,
  UsersPage,
  WarehousesPage,
} from './pages/admin/SimplePages'
import { CustomersPage } from './pages/admin/CustomersPage'
import { ProductsPage } from './pages/admin/ProductsPage'
import { StocksPage } from './pages/admin/StocksPage'
import { ExportsPage, ImportsPage } from './pages/admin/ReceiptsPage'
import { StocktakesPage } from './pages/admin/StocktakesPage'
import { TransactionsPage } from './pages/admin/TransactionsPage'
import { ReportsPage } from './pages/admin/ReportsPage'
import { StoreHome } from './pages/store/StoreHome'
import { StoreProducts } from './pages/store/StoreProducts'
import { StoreProductDetail } from './pages/store/StoreProductDetail'
import { StoreProfile } from './pages/store/StoreProfile'
import { StoreCart } from './pages/store/StoreCart'
import { StoreOrders } from './pages/store/StoreOrders'
import { StoreInvoice } from './pages/store/StoreInvoice'
import { RequireAdmin, RequireCustomer } from './routes/Protected'
import { CartProvider } from './store/CartContext'

export default function App() {
  return (
    <AppearanceProvider>
      <AppWithAppearance />
    </AppearanceProvider>
  )
}

function AppWithAppearance() {
  const { mode } = useAppearance()
  const theme = useMemo(() => createAppTheme(mode), [mode])

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <ToastProvider>
      <AuthProvider>
        <BrowserRouter>
          <CartProvider>
          <Routes>
            <Route path="/" element={<Navigate to="/store" replace />} />

            <Route path="/admin/login" element={<LoginAdmin />} />
            <Route path="/store/login" element={<LoginCustomer />} />

            <Route
              path="/admin"
              element={
                <RequireAdmin>
                  <AdminLayout />
                </RequireAdmin>
              }
            >
              <Route index element={<AdminDashboard />} />
              <Route path="roles" element={<RolesPage />} />
              <Route path="users" element={<UsersPage />} />
              <Route path="categories" element={<CategoriesPage />} />
              <Route path="units" element={<UnitsPage />} />
              <Route path="suppliers" element={<SuppliersPage />} />
              <Route path="customers" element={<CustomersPage />} />
              <Route path="warehouses" element={<WarehousesPage />} />
              <Route path="products" element={<ProductsPage />} />
              <Route path="stocks" element={<StocksPage />} />
              <Route path="imports" element={<ImportsPage />} />
              <Route path="exports" element={<ExportsPage />} />
              <Route path="stocktakes" element={<StocktakesPage />} />
              <Route path="transactions" element={<TransactionsPage />} />
              <Route path="reports" element={<ReportsPage />} />
            </Route>

            <Route path="/store" element={<CustomerLayout />}>
              <Route index element={<StoreHome />} />
              <Route path="products" element={<StoreProducts />} />
              <Route path="products/:id" element={<StoreProductDetail />} />
              <Route path="cart" element={<StoreCart />} />
              <Route
                path="orders"
                element={
                  <RequireCustomer>
                    <StoreOrders />
                  </RequireCustomer>
                }
              />
              <Route
                path="invoices/:id"
                element={
                  <RequireCustomer>
                    <StoreInvoice />
                  </RequireCustomer>
                }
              />
              <Route
                path="profile"
                element={
                  <RequireCustomer>
                    <StoreProfile />
                  </RequireCustomer>
                }
              />
            </Route>

            <Route path="*" element={<Navigate to="/store" replace />} />
          </Routes>
          </CartProvider>
        </BrowserRouter>
      </AuthProvider>
      </ToastProvider>
    </ThemeProvider>
  )
}
