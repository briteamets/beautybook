import { createI18n } from 'vue-i18n'
import en from './en'
import et from './et'

const saved = localStorage.getItem('locale') as 'en' | 'et' | null

export default createI18n({
  legacy: false,
  locale: saved ?? 'en',
  fallbackLocale: 'en',
  messages: { en, et },
})
