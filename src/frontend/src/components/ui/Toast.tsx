import { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import styles from './Toast.module.css';

export type ToastVariant = 'success' | 'warning' | 'error' | 'info';

export interface ToastData {
  id: string;
  variant: ToastVariant;
  title: string;
  message?: string;
  duration?: number;
}

interface ToastItemProps {
  toast: ToastData;
  onDismiss: (id: string) => void;
}

const icons: Record<ToastVariant, string> = {
  success: '\u2713', // checkmark
  warning: '\u26A0', // warning sign
  error: '\u2717',   // x mark
  info: '\u2139',    // info
};

function ToastItem({ toast, onDismiss }: ToastItemProps) {
  const [exiting, setExiting] = useState(false);
  const duration = toast.duration ?? 5000;

  useEffect(() => {
    if (duration <= 0) return;

    const timer = setTimeout(() => {
      setExiting(true);
    }, duration);

    return () => clearTimeout(timer);
  }, [duration]);

  useEffect(() => {
    if (exiting) {
      const timer = setTimeout(() => {
        onDismiss(toast.id);
      }, 200); // match animation duration
      return () => clearTimeout(timer);
    }
  }, [exiting, onDismiss, toast.id]);

  const handleDismiss = () => {
    setExiting(true);
  };

  return (
    <div
      className={`${styles.toast} ${styles[toast.variant]} ${exiting ? styles.exiting : ''}`}
      role="alert"
      aria-live="polite"
    >
      <span className={styles.icon} aria-hidden="true">
        {icons[toast.variant]}
      </span>
      <div className={styles.content}>
        <p className={styles.title}>{toast.title}</p>
        {toast.message && <p className={styles.message}>{toast.message}</p>}
      </div>
      <button
        className={styles.closeButton}
        onClick={handleDismiss}
        aria-label="Dismiss notification"
      >
        &times;
      </button>
    </div>
  );
}

interface ToastContainerProps {
  toasts: ToastData[];
  onDismiss: (id: string) => void;
}

export function ToastContainer({ toasts, onDismiss }: ToastContainerProps) {
  if (toasts.length === 0) return null;

  return createPortal(
    <div className={styles.container}>
      {toasts.map((toast) => (
        <ToastItem key={toast.id} toast={toast} onDismiss={onDismiss} />
      ))}
    </div>,
    document.body
  );
}
