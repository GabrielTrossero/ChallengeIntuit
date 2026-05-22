<template>
  <div class="toast-container">
    <div
      v-for="toast in toasts"
      :key="toast.id"
      class="toast"
      :class="`toast-${toast.type}`"
    >
      {{ toast.message }}
    </div>
  </div>
</template>

<script>
import { subscribeToNotifications } from '../services/notificationBus'

export default {
  name: 'ToastNotifications',
  data() {
    return {
      toasts: [],
      unsubscribe: null
    }
  },
  mounted() {
    this.unsubscribe = subscribeToNotifications(this.pushToast)
  },
  beforeUnmount() {
    if (this.unsubscribe) {
      this.unsubscribe()
    }
  },
  methods: {
    pushToast(toast) {
      const item = {
        id: Date.now() + Math.random(),
        type: toast.type || 'error',
        message: toast.message
      }

      this.toasts.push(item)

      setTimeout(() => {
        this.toasts = this.toasts.filter(current => current.id !== item.id)
      }, 3500)
    }
  }
}
</script>

<style scoped>
.toast-container {
  position: fixed;
  top: 16px;
  right: 16px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  z-index: 9999;
  max-width: 360px;
}

.toast {
  border-radius: 8px;
  padding: 12px 14px;
  color: #fff;
  font-size: 14px;
  line-height: 1.35;
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.18);
  animation: toast-enter 0.18s ease-out;
}

.toast-error {
  background: #c62828;
}

.toast-success {
  background: #2e7d32;
}

@keyframes toast-enter {
  from {
    transform: translateY(-8px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

@media (max-width: 640px) {
  .toast-container {
    left: 12px;
    right: 12px;
    max-width: none;
  }
}
</style>
