const FALLBACK_MESSAGES = {
  NETWORK_ERROR: 'No se pudo conectar con el servidor. Verifica tu conexion e intenta nuevamente.',
  TIMEOUT: 'La solicitud tardo demasiado. Intenta nuevamente en unos segundos.',
  NOT_FOUND: 'No se encontro el recurso solicitado.',
  SERVER_ERROR: 'Ocurrio un error interno del servidor. Intenta nuevamente mas tarde.',
  BAD_REQUEST: 'No se pudo procesar la solicitud.',
  UNKNOWN_ERROR: 'Error al procesar la solicitud.'
}

export function mapApiError(error) {
  const response = error?.response
  const status = response?.status
  const backendMessage = response?.data?.mensaje

  if (!response) {
    if (error?.code === 'ECONNABORTED') {
      return { code: 'TIMEOUT', message: FALLBACK_MESSAGES.TIMEOUT }
    }

    return { code: 'NETWORK_ERROR', message: FALLBACK_MESSAGES.NETWORK_ERROR }
  }

  if (backendMessage) {
    return { code: `HTTP_${status || 'ERROR'}`, message: backendMessage }
  }

  if (status === 404) {
    return { code: 'NOT_FOUND', message: FALLBACK_MESSAGES.NOT_FOUND }
  }

  if (status >= 500) {
    return { code: 'SERVER_ERROR', message: FALLBACK_MESSAGES.SERVER_ERROR }
  }

  if (status >= 400) {
    return { code: 'BAD_REQUEST', message: FALLBACK_MESSAGES.BAD_REQUEST }
  }

  return { code: 'UNKNOWN_ERROR', message: FALLBACK_MESSAGES.UNKNOWN_ERROR }
}

export function getErrorMessage(error) {
  if (error?.normalizedError?.message) {
    return error.normalizedError.message
  }

  return mapApiError(error).message
}
