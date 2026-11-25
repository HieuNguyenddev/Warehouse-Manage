import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'

export type AppearanceMode = 'light' | 'dark'

interface AppearanceContextValue {
  mode: AppearanceMode
  toggleMode: () => void
}

const STORAGE_KEY = 'warehouse-appearance'
const AppearanceContext = createContext<AppearanceContextValue | null>(null)

function getInitialMode(): AppearanceMode {
  return localStorage.getItem(STORAGE_KEY) === 'dark' ? 'dark' : 'light'
}

export function AppearanceProvider({ children }: { children: ReactNode }) {
  const [mode, setMode] = useState<AppearanceMode>(getInitialMode)

  useEffect(() => {
    document.documentElement.dataset.theme = mode
    localStorage.setItem(STORAGE_KEY, mode)
  }, [mode])

  const value = useMemo(
    () => ({
      mode,
      toggleMode: () => setMode((current) => (current === 'light' ? 'dark' : 'light')),
    }),
    [mode],
  )

  return <AppearanceContext.Provider value={value}>{children}</AppearanceContext.Provider>
}

export function useAppearance() {
  const context = useContext(AppearanceContext)
  if (!context) throw new Error('useAppearance must be used within AppearanceProvider')
  return context
}
