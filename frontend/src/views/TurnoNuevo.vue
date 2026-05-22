<template>
  <div class="card" style="max-width: 560px">
    <h2>Nuevo turno</h2>
    <form novalidate @submit.prevent="guardar">
      <div class="form-group">
        <label>Paciente *</label>
        <select
          v-model="form.pacienteId"
          :class="{ 'input-invalid': submitted && !form.pacienteId }"
          aria-required="true"
        >
          <option value="">Seleccioná un paciente</option>
          <option v-for="p in pacientes" :key="p.id" :value="p.id">
            {{ p.nombreCompleto }} — DNI {{ p.dni }}
          </option>
        </select>
        <p v-if="submitted && !form.pacienteId" class="field-error">Seleccioná un paciente.</p>
      </div>
      <div class="form-group">
        <label>Médico *</label>
        <select
          v-model="form.medicoId"
          :class="{ 'input-invalid': submitted && !form.medicoId }"
          aria-required="true"
        >
          <option value="">Seleccioná un médico</option>
          <option v-for="m in medicos" :key="m.id" :value="m.id">
            {{ m.nombreCompleto }} — {{ m.especialidad }}
          </option>
        </select>
        <p v-if="submitted && !form.medicoId" class="field-error">Seleccioná un médico.</p>
      </div>
      <div class="form-group">
        <label>Fecha y hora *</label>
        <input
          type="datetime-local"
          v-model="form.fechaHora"
          :class="{ 'input-invalid': submitted && !form.fechaHora }"
          aria-required="true"
        />
        <p v-if="submitted && !form.fechaHora" class="field-error">Ingresá fecha y hora.</p>
      </div>
      <div class="form-group">
        <label>Motivo</label>
        <input type="text" v-model="form.motivo" placeholder="Motivo de la consulta" />
      </div>
      <p class="required-note">* Campos obligatorios</p>
      <button type="submit">Confirmar turno</button>
    </form>
  </div>
</template>

<script>
import { turnosApi, pacientesApi, medicosApi } from '../services/api'
import { getErrorMessage } from '../services/errorMapper'
import { notifyError, notifySuccess } from '../services/notificationBus'

export default {
  name: 'TurnoNuevo',
  data() {
    return {
      form: {
        pacienteId: '',
        medicoId: '',
        fechaHora: '',
        motivo: ''
      },
      submitted: false,
      pacientes: [],
      medicos: []
    }
  },
  async mounted() {
    try {
      const [pRes, mRes] = await Promise.all([pacientesApi.getAll(), medicosApi.getAll()])
      this.pacientes = pRes.data
      this.medicos = mRes.data
    } catch (error) {
      notifyError(getErrorMessage(error))
    }
  },
  methods: {
    validarFormulario() {
      this.submitted = true
      return Boolean(this.form.pacienteId && this.form.medicoId && this.form.fechaHora)
    },
    async guardar() {
      if (!this.validarFormulario()) {
        return
      }

      try {
        await turnosApi.create({
          pacienteId: Number(this.form.pacienteId),
          medicoId: Number(this.form.medicoId),
          fechaHora: this.form.fechaHora,
          motivo: this.form.motivo
        })
        notifySuccess('Turno creado correctamente.')
        this.$router.push('/turnos')
      } catch (error) {
        notifyError(getErrorMessage(error))
      }
    }
  }
}
</script>

<style scoped>
.field-error {
  margin: 6px 0 0;
  color: #c62828;
  font-size: 12px;
  font-weight: 600;
}

.required-note {
  margin: 0 0 12px;
  color: #666;
  font-size: 12px;
}

.input-invalid {
  border-color: #c62828;
  box-shadow: 0 0 0 1px rgba(198, 40, 40, 0.2);
}

button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
</style>
