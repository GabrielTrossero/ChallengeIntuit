const listeners = new Set()

export function subscribeToNotifications(listener) {
  listeners.add(listener)

  return () => {
    listeners.delete(listener)
  }
}

export function notifyError(message) {
  listeners.forEach(listener => listener({
    type: 'error',
    message
  }))
}

export function notifySuccess(message) {
  listeners.forEach(listener => listener({
    type: 'success',
    message
  }))
}
