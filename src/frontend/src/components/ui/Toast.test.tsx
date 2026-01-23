import { render, screen, act } from '@testing-library/react'
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest'
import { ToastContainer, type ToastData } from './Toast'

describe('ToastContainer', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.runOnlyPendingTimers()
    vi.useRealTimers()
  })

  it('renders nothing when toasts array is empty', () => {
    const { container } = render(
      <ToastContainer toasts={[]} onDismiss={vi.fn()} />
    )

    expect(container).toBeEmptyDOMElement()
  })

  it('renders a toast with title and message', () => {
    const toasts: ToastData[] = [
      {
        id: 'toast-1',
        variant: 'info',
        title: 'Test Title',
        message: 'Test message',
      },
    ]

    render(<ToastContainer toasts={toasts} onDismiss={vi.fn()} />)

    expect(screen.getByText('Test Title')).toBeInTheDocument()
    expect(screen.getByText('Test message')).toBeInTheDocument()
  })

  it('renders a toast without message', () => {
    const toasts: ToastData[] = [
      {
        id: 'toast-1',
        variant: 'success',
        title: 'Success!',
      },
    ]

    render(<ToastContainer toasts={toasts} onDismiss={vi.fn()} />)

    expect(screen.getByText('Success!')).toBeInTheDocument()
  })

  it('renders multiple toasts', () => {
    const toasts: ToastData[] = [
      { id: 'toast-1', variant: 'info', title: 'First' },
      { id: 'toast-2', variant: 'success', title: 'Second' },
      { id: 'toast-3', variant: 'error', title: 'Third' },
    ]

    render(<ToastContainer toasts={toasts} onDismiss={vi.fn()} />)

    expect(screen.getByText('First')).toBeInTheDocument()
    expect(screen.getByText('Second')).toBeInTheDocument()
    expect(screen.getByText('Third')).toBeInTheDocument()
  })

  it('calls onDismiss when close button is clicked', async () => {
    // Use real timers for this test to handle the animation naturally
    vi.useRealTimers()

    const onDismiss = vi.fn()
    const toasts: ToastData[] = [
      { id: 'toast-1', variant: 'info', title: 'Test', duration: 0 }, // Disable auto-dismiss
    ]

    render(<ToastContainer toasts={toasts} onDismiss={onDismiss} />)

    const closeButton = screen.getByRole('button', { name: /dismiss/i })

    await act(async () => {
      closeButton.click()
      // Wait for exit animation (200ms) + buffer
      await new Promise((resolve) => setTimeout(resolve, 300))
    })

    expect(onDismiss).toHaveBeenCalledWith('toast-1')

    // Restore fake timers for other tests
    vi.useFakeTimers()
  })

  it('auto-dismisses after duration', () => {
    const onDismiss = vi.fn()
    const toasts: ToastData[] = [
      { id: 'toast-1', variant: 'info', title: 'Test', duration: 3000 },
    ]

    render(<ToastContainer toasts={toasts} onDismiss={onDismiss} />)

    expect(onDismiss).not.toHaveBeenCalled()

    // Advance past the duration
    act(() => {
      vi.advanceTimersByTime(3000)
    })

    // Wait for exit animation
    act(() => {
      vi.advanceTimersByTime(200)
    })

    expect(onDismiss).toHaveBeenCalledWith('toast-1')
  })

  it('uses default duration of 5000ms', () => {
    const onDismiss = vi.fn()
    const toasts: ToastData[] = [
      { id: 'toast-1', variant: 'info', title: 'Test' },
    ]

    render(<ToastContainer toasts={toasts} onDismiss={onDismiss} />)

    // After 4 seconds, should not be dismissed
    act(() => {
      vi.advanceTimersByTime(4000)
    })
    expect(onDismiss).not.toHaveBeenCalled()

    // After 5 seconds + animation, should be dismissed
    act(() => {
      vi.advanceTimersByTime(1000)
    })
    act(() => {
      vi.advanceTimersByTime(200)
    })

    expect(onDismiss).toHaveBeenCalledWith('toast-1')
  })

  it('does not auto-dismiss when duration is 0', () => {
    const onDismiss = vi.fn()
    const toasts: ToastData[] = [
      { id: 'toast-1', variant: 'info', title: 'Test', duration: 0 },
    ]

    render(<ToastContainer toasts={toasts} onDismiss={onDismiss} />)

    // Advance a long time
    act(() => {
      vi.advanceTimersByTime(60000)
    })

    expect(onDismiss).not.toHaveBeenCalled()
  })

  it('has correct accessibility attributes', () => {
    const toasts: ToastData[] = [
      { id: 'toast-1', variant: 'info', title: 'Accessible Toast' },
    ]

    render(<ToastContainer toasts={toasts} onDismiss={vi.fn()} />)

    const alert = screen.getByRole('alert')
    expect(alert).toHaveAttribute('aria-live', 'polite')
  })

  it('renders different variants correctly', () => {
    const toasts: ToastData[] = [
      { id: 'toast-1', variant: 'success', title: 'Success' },
      { id: 'toast-2', variant: 'error', title: 'Error' },
      { id: 'toast-3', variant: 'warning', title: 'Warning' },
      { id: 'toast-4', variant: 'info', title: 'Info' },
    ]

    render(<ToastContainer toasts={toasts} onDismiss={vi.fn()} />)

    const alerts = screen.getAllByRole('alert')
    expect(alerts).toHaveLength(4)
  })
})
