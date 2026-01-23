import { type InputHTMLAttributes, forwardRef } from 'react'
import styles from './Input.module.css'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  fullWidth?: boolean
}

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, fullWidth = true, className, id, ...props }, ref) => {
    const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-')

    return (
      <div
        className={`${styles.container} ${fullWidth ? styles.fullWidth : ''}`}
      >
        {label && (
          <label htmlFor={inputId} className={styles.label}>
            {label}
          </label>
        )}
        <input
          ref={ref}
          id={inputId}
          className={`${styles.input} ${error ? styles.inputError : ''} ${className ?? ''}`}
          {...props}
        />
        {error && <span className={styles.error}>{error}</span>}
      </div>
    )
  }
)

Input.displayName = 'Input'
