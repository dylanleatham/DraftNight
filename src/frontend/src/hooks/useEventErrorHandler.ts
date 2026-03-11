import { useCallback } from 'react'
import { ApiError } from '../api/client'
import { useEvent } from '../context/EventContext'

/**
 * Shared error handler for event action hooks.
 * Dispatches a SET_ERROR action with an appropriate message.
 */
export function useEventErrorHandler() {
  const { dispatch } = useEvent()

  const handleError = useCallback(
    (err: unknown) => {
      if (err instanceof ApiError) {
        dispatch({ type: 'SET_ERROR', payload: err.message })
      } else if (err instanceof Error) {
        dispatch({ type: 'SET_ERROR', payload: err.message })
      } else {
        dispatch({
          type: 'SET_ERROR',
          payload: 'An unexpected error occurred',
        })
      }
    },
    [dispatch]
  )

  return handleError
}
