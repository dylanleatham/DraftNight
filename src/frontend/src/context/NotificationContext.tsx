/* eslint-disable react-refresh/only-export-components */
import {
  createContext,
  useContext,
  useCallback,
  useState,
  type ReactNode,
} from 'react'
import {
  ToastContainer,
  type ToastData,
  type ToastVariant,
} from '../components/ui/Toast'

type ToastInput = Omit<ToastData, 'id'>

interface NotificationContextValue {
  showToast: (toast: ToastInput) => void
  dismissToast: (id: string) => void
  clearToasts: () => void
}

const NotificationContext = createContext<NotificationContextValue | null>(null)

let toastIdCounter = 0

export function NotificationProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastData[]>([])

  const showToast = useCallback((toast: ToastInput) => {
    const id = `toast-${++toastIdCounter}`
    setToasts((prev) => [...prev, { ...toast, id }])
  }, [])

  const dismissToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  const clearToasts = useCallback(() => {
    setToasts([])
  }, [])

  return (
    <NotificationContext.Provider
      value={{ showToast, dismissToast, clearToasts }}
    >
      {children}
      <ToastContainer toasts={toasts} onDismiss={dismissToast} />
    </NotificationContext.Provider>
  )
}

export function useNotification(): NotificationContextValue {
  const context = useContext(NotificationContext)
  if (!context) {
    throw new Error(
      'useNotification must be used within a NotificationProvider'
    )
  }
  return context
}

// Convenience hooks for common toast types
export function useToast() {
  const { showToast } = useNotification()

  return {
    success: (title: string, message?: string) =>
      showToast({ variant: 'success', title, message }),
    error: (title: string, message?: string) =>
      showToast({ variant: 'error', title, message }),
    warning: (title: string, message?: string) =>
      showToast({ variant: 'warning', title, message }),
    info: (title: string, message?: string) =>
      showToast({ variant: 'info', title, message }),
    show: (
      variant: ToastVariant,
      title: string,
      message?: string,
      duration?: number
    ) => showToast({ variant, title, message, duration }),
  }
}
