import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react'
import type { ProductDto } from '../types/api'

export type CartItem = {
  productId: number
  sku: string
  name: string
  price: number
  unitSymbol?: string | null
  imageUrl?: string | null
  quantity: number
}

type CartContextValue = {
  items: CartItem[]
  totalQty: number
  totalAmount: number
  addItem: (product: ProductDto, qty?: number) => void
  setQty: (productId: number, quantity: number) => void
  removeItem: (productId: number) => void
  clear: () => void
}

const STORAGE_KEY = 'wm_store_cart'
const CartContext = createContext<CartContextValue | null>(null)

function loadCart(): CartItem[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return []
    const parsed = JSON.parse(raw) as CartItem[]
    return Array.isArray(parsed) ? parsed.filter((x) => x.productId && x.quantity > 0) : []
  } catch {
    return []
  }
}

function productImage(product: ProductDto) {
  const img = product.images?.find((x) => x.isPrimary) || product.images?.[0]
  return img?.imageUrl || null
}

export function CartProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<CartItem[]>(() => loadCart())

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items))
  }, [items])

  const value = useMemo<CartContextValue>(() => {
    const totalQty = items.reduce((s, i) => s + i.quantity, 0)
    const totalAmount = items.reduce((s, i) => s + i.quantity * i.price, 0)

    return {
      items,
      totalQty,
      totalAmount,
      addItem: (product, qty = 1) => {
        const add = Math.max(1, Math.floor(qty))
        setItems((prev) => {
          const found = prev.find((x) => x.productId === product.id)
          if (found) {
            return prev.map((x) =>
              x.productId === product.id ? { ...x, quantity: x.quantity + add } : x,
            )
          }
          return [
            ...prev,
            {
              productId: product.id,
              sku: product.sku,
              name: product.name,
              price: product.price,
              unitSymbol: product.unitSymbol,
              imageUrl: productImage(product),
              quantity: add,
            },
          ]
        })
      },
      setQty: (productId, quantity) => {
        const q = Math.floor(quantity)
        setItems((prev) => {
          if (q <= 0) return prev.filter((x) => x.productId !== productId)
          return prev.map((x) => (x.productId === productId ? { ...x, quantity: q } : x))
        })
      },
      removeItem: (productId) => setItems((prev) => prev.filter((x) => x.productId !== productId)),
      clear: () => setItems([]),
    }
  }, [items])

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>
}

export function useCart() {
  const ctx = useContext(CartContext)
  if (!ctx) throw new Error('useCart must be used within CartProvider')
  return ctx
}
