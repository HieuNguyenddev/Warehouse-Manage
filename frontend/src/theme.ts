import { createTheme } from '@mui/material/styles'

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#1565c0', dark: '#0d47a1', light: '#42a5f5' },
    secondary: { main: '#00897b' },
    success: { main: '#2e7d32' },
    warning: { main: '#ed6c02' },
    error: { main: '#d32f2f' },
    background: { default: '#f8fafc', paper: '#ffffff' },
  },
  shape: { borderRadius: 10 },
  typography: {
    fontFamily: '"Be Vietnam Pro", sans-serif',
    h4: { fontWeight: 700 },
    h5: { fontWeight: 700 },
    h6: { fontWeight: 600 },
  },
  components: {
    MuiButton: {
      defaultProps: { variant: 'contained', disableElevation: true },
      styleOverrides: { root: { textTransform: 'none', fontWeight: 600 } },
    },
    MuiCard: {
      defaultProps: { variant: 'outlined' },
      styleOverrides: { root: { borderColor: '#e2e8f0' } },
    },
    MuiDrawer: {
      styleOverrides: {
        paper: { borderRight: '1px solid #e2e8f0' },
      },
    },
  },
})
