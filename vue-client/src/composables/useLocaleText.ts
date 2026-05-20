import { useI18n } from 'vue-i18n'

export function useLocaleText() {
  const { locale } = useI18n()
  return (en?: string | null, et?: string | null) =>
    locale.value === 'et' ? (et ?? en ?? '') : (en ?? et ?? '')
}
