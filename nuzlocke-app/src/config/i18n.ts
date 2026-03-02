import { createI18n } from 'vue-i18n'
import en from '../locales/en.json'
import es from '../locales/es.json'

// Tipo para el esquema de mensajes de idiomas
type MessageSchema = typeof en

const i18n = createI18n<[MessageSchema], 'en' | 'es'>({
    legacy: false, // Uso explícito de Composition API
    locale: 'es', // Idioma por defecto
    fallbackLocale: 'en',
    messages: {
        en,
        es
    }
})

export default i18n
