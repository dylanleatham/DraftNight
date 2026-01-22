import type { ReactNode } from 'react';
import styles from './BasePlayerPanel.module.css';

export type PlayerColor = 'blue' | 'red' | 'green' | 'purple' | 'orange' | 'teal';

interface BasePlayerPanelProps {
  /** Player display name */
  name: string;
  /** Panel color theme */
  color: PlayerColor;
  /** Whether the panel is inverted (rotated 180deg for face-to-face play) */
  inverted?: boolean;
  /** Content for the header area (right side, next to name) */
  headerContent?: ReactNode;
  /** Main content (life display, etc.) */
  children: ReactNode;
  /** Footer content (counters, etc.) */
  footerContent?: ReactNode;
  /** Click handler for the panel (used for expand/collapse in Commander) */
  onClick?: () => void;
}

export function BasePlayerPanel({
  name,
  color,
  inverted = false,
  headerContent,
  children,
  footerContent,
  onClick,
}: BasePlayerPanelProps) {
  return (
    <div
      className={`${styles.container} ${styles[color]} ${inverted ? styles.inverted : ''}`}
      onClick={onClick}
    >
      <div className={styles.header}>
        <span className={styles.playerName}>{name}</span>
        {headerContent && <div className={styles.headerContent}>{headerContent}</div>}
      </div>

      <div className={styles.main}>{children}</div>

      {footerContent && <div className={styles.footer}>{footerContent}</div>}
    </div>
  );
}
