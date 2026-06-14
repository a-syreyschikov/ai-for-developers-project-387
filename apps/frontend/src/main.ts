import './assets/main.css'
import 'primeicons/primeicons.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import ConfirmationService from 'primevue/confirmationservice'
import ToastService from 'primevue/toastservice'
import { definePreset } from '@primeuix/themes'
import Aura from '@primeuix/themes/aura'

import App from './App.vue'
import router from './router'
import { i18n } from './shared/i18n'
import { primeVueLocale } from './shared/primevue-locale'

const CalendarPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{indigo.50}',
      100: '{indigo.100}',
      200: '{indigo.200}',
      300: '{indigo.300}',
      400: '{indigo.400}',
      500: '{indigo.500}',
      600: '{indigo.600}',
      700: '{indigo.700}',
      800: '{indigo.800}',
      900: '{indigo.900}',
      950: '{indigo.950}',
    },
    colorScheme: {
      light: {
        primary: {
          color: '{indigo.600}',
          inverseColor: '#ffffff',
          hoverColor: '{indigo.700}',
          activeColor: '{indigo.800}',
        },
        highlight: {
          background: '{indigo.50}',
          focusBackground: '{indigo.100}',
          color: '{indigo.900}',
          focusColor: '{indigo.950}',
        },
        surface: {
          0: '#ffffff',
          50: '#f8fafc',
          100: '#f1f5f9',
          200: '#e2e8f0',
          300: '#cbd5e1',
          400: '#94a3b8',
          500: '#64748b',
          600: '#475569',
          700: '#334155',
          800: '#1e293b',
          900: '#0f172a',
          950: '#020617',
        },
      },
    },
    focusRing: {
      width: '2px',
      style: 'solid',
      color: '{primary.300}',
      offset: '2px',
    },
  },
  components: {
    button: {
      root: {
        borderRadius: '8px',
        roundedBorderRadius: '8px',
        label: {
          fontWeight: '600',
        },
      },
    },
    card: {
      root: {
        borderRadius: '12px',
        shadow: '0 14px 34px rgba(15, 23, 42, 0.08)',
      },
      body: {
        padding: '18px',
        gap: '12px',
      },
      title: {
        fontWeight: '650',
      },
    },
    datepicker: {
      panel: {
        borderRadius: '12px',
        shadow: 'none',
        padding: '10px',
      },
      date: {
        width: '2.7rem',
        height: '2.7rem',
        borderRadius: '6px',
        padding: '0.25rem',
      },
      today: {
        background: '{surface.100}',
        color: '{surface.950}',
      },
    },
    skeleton: {
      root: {
        borderRadius: '12px',
      },
    },
    tag: {
      root: {
        borderRadius: '6px',
        fontWeight: '600',
      },
      info: {
        background: '{indigo.50}',
        color: '{indigo.700}',
      },
      success: {
        background: '{indigo.50}',
        color: '{indigo.700}',
      },
      secondary: {
        background: '{surface.100}',
        color: '{surface.700}',
      },
    },
  },
})

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(i18n)
app.use(PrimeVue, {
  locale: primeVueLocale,
  ripple: true,
  theme: {
    preset: CalendarPreset,
    options: {
      darkModeSelector: false,
    },
  },
})
app.use(ConfirmationService)
app.use(ToastService)

app.mount('#app')
