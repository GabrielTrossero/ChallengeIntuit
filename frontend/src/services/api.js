import axios from 'axios'
import { mapApiError } from './errorMapper'

const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'

const apiClient = axios.create({
  baseURL: BASE_URL
})

apiClient.interceptors.response.use(
  response => response,
  error => {
    error.normalizedError = mapApiError(error)
    return Promise.reject(error)
  }
)

export const turnosApi = {
  getAll:           ()          => apiClient.get('/turnos'),
  getById:          (id)        => apiClient.get(`/turnos/${id}`),
  create:           (data)      => apiClient.post('/turnos', data),
  cancelar:         (id)        => apiClient.put(`/turnos/${id}/cancelar`),
  marcarAusencia:   (id)        => apiClient.post(`/turnos/${id}/ausencia`),
  actualizarEstado: (id, data)  => apiClient.put(`/turnos/${id}/estado`, data)
}

export const pacientesApi = {
  getAll:  ()          => apiClient.get('/pacientes'),
  getById: (id)        => apiClient.get(`/pacientes/${id}`),
  create:  (data)      => apiClient.post('/pacientes', data),
  update:  (id, data)  => apiClient.put(`/pacientes/${id}`, data),
  delete:  (id)        => apiClient.delete(`/pacientes/${id}`)
}

export const medicosApi = {
  getAll: () => apiClient.get('/medicos')
}

export const sucursalesApi = {
  getAll: () => apiClient.get('/sucursales')
}
