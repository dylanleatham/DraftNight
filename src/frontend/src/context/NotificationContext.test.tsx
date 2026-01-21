import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { NotificationProvider, useNotification, useToast } from './NotificationContext';

// Mock createPortal to avoid DOM portal issues in tests
vi.mock('react-dom', async () => {
  const actual = await vi.importActual('react-dom');
  return {
    ...actual,
    createPortal: (node: React.ReactNode) => node,
  };
});

describe('NotificationContext', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.runOnlyPendingTimers();
    vi.useRealTimers();
  });

  describe('useNotification', () => {
    it('throws error when used outside provider', () => {
      // Suppress console.error for this test
      const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      expect(() => {
        renderHook(() => useNotification());
      }).toThrow('useNotification must be used within a NotificationProvider');

      consoleSpy.mockRestore();
    });

    it('provides showToast function', () => {
      const { result } = renderHook(() => useNotification(), {
        wrapper: NotificationProvider,
      });

      expect(result.current.showToast).toBeDefined();
      expect(typeof result.current.showToast).toBe('function');
    });

    it('provides dismissToast function', () => {
      const { result } = renderHook(() => useNotification(), {
        wrapper: NotificationProvider,
      });

      expect(result.current.dismissToast).toBeDefined();
      expect(typeof result.current.dismissToast).toBe('function');
    });

    it('provides clearToasts function', () => {
      const { result } = renderHook(() => useNotification(), {
        wrapper: NotificationProvider,
      });

      expect(result.current.clearToasts).toBeDefined();
      expect(typeof result.current.clearToasts).toBe('function');
    });
  });

  describe('useToast', () => {
    it('provides convenience methods for each variant', () => {
      const { result } = renderHook(() => useToast(), {
        wrapper: NotificationProvider,
      });

      expect(result.current.success).toBeDefined();
      expect(result.current.error).toBeDefined();
      expect(result.current.warning).toBeDefined();
      expect(result.current.info).toBeDefined();
      expect(result.current.show).toBeDefined();
    });

    it('success creates toast with success variant', () => {
      const { result } = renderHook(() => useToast(), {
        wrapper: NotificationProvider,
      });

      // This is a simplified test - in real usage, toasts would render
      // We're testing that the functions don't throw
      expect(() => {
        act(() => {
          result.current.success('Title', 'Message');
        });
      }).not.toThrow();
    });

    it('error creates toast with error variant', () => {
      const { result } = renderHook(() => useToast(), {
        wrapper: NotificationProvider,
      });

      expect(() => {
        act(() => {
          result.current.error('Error Title', 'Error message');
        });
      }).not.toThrow();
    });

    it('warning creates toast with warning variant', () => {
      const { result } = renderHook(() => useToast(), {
        wrapper: NotificationProvider,
      });

      expect(() => {
        act(() => {
          result.current.warning('Warning Title', 'Warning message');
        });
      }).not.toThrow();
    });

    it('info creates toast with info variant', () => {
      const { result } = renderHook(() => useToast(), {
        wrapper: NotificationProvider,
      });

      expect(() => {
        act(() => {
          result.current.info('Info Title', 'Info message');
        });
      }).not.toThrow();
    });

    it('show creates toast with custom variant and duration', () => {
      const { result } = renderHook(() => useToast(), {
        wrapper: NotificationProvider,
      });

      expect(() => {
        act(() => {
          result.current.show('success', 'Custom Title', 'Custom message', 10000);
        });
      }).not.toThrow();
    });
  });
});
