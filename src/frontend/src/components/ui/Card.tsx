import { type ReactNode } from 'react'
import styles from './Card.module.css'

interface CardProps {
  children: ReactNode
  className?: string
  highlight?: boolean
  onClick?: () => void
}

export function Card({
  children,
  className,
  highlight = false,
  onClick,
}: CardProps) {
  const Component = onClick ? 'button' : 'div'
  return (
    <Component
      className={`${styles.card} ${highlight ? styles.highlight : ''} ${onClick ? styles.clickable : ''} ${className ?? ''}`}
      onClick={onClick}
    >
      {children}
    </Component>
  )
}
