import { createTheme } from '@mui/material/styles'
import type { AppearanceMode } from './components/AppearanceContext'

export function createAppTheme(mode: AppearanceMode) {
  return createTheme({
    palette: {
      mode,
      primary: {
        main: mode === 'dark' ? '#78aef5' : '#1565c0',
        dark: '#0d47a1',
        light: '#42a5f5',
      },
      secondary: { main: '#00897b' },
      success: { main: '#2e7d32' },
      warning: { main: '#ed6c02' },
      error: { main: '#d32f2f' },
      background:
        mode === 'dark'
          ? { default: '#111827', paper: '#1b2533' }
          : { default: '#f8fafc', paper: '#ffffff' },
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
        styleOverrides: { root: ({ theme }) => ({ borderColor: theme.palette.divider }) },
      },
      MuiDrawer: {
        styleOverrides: {
          paper: ({ theme }) => ({ borderRight: `1px solid ${theme.palette.divider}` }),
        },
      },
    },
  })
}
