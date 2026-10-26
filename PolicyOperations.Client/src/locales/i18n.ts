import { ref } from 'vue';

export type Locale = 'es' | 'en';

const STORAGE_KEY = 'nain_locale';
const EVENT_NAME = 'nain:locale-change';

function getInitialLocale(): Locale {
  if (typeof window === 'undefined') return 'es';
  const stored = localStorage.getItem(STORAGE_KEY);
  if (stored === 'es' || stored === 'en') return stored;
  return 'es';
}

export const currentLocale = ref<Locale>(getInitialLocale());

export function setLocale(locale: Locale) {
  if (currentLocale.value === locale) return;
  currentLocale.value = locale;
  if (typeof window !== 'undefined') {
    localStorage.setItem(STORAGE_KEY, locale);
    window.dispatchEvent(new CustomEvent(EVENT_NAME, { detail: { locale } }));
  }
}

if (typeof window !== 'undefined') {
  window.addEventListener(EVENT_NAME, (event: Event) => {
    const custom = event as CustomEvent<{ locale?: Locale }>;
    const target = custom.detail?.locale;
    if (target === 'es' || target === 'en') {
      currentLocale.value = target;
    }
  });
}

export const translations = {
  es: {
    eyebrow: 'Operaciones de pólizas · .NET 10 · SQL Server',
    tabBlueprint: 'Arquitectura',
    tabLifecycle: 'Ciclo de Vida',
    tabConcurrency: 'Concurrencia',
    tabAudit: 'Auditoría',
    tabResilience: 'Resiliencia',
    jwtConfigured: 'JWT Configurado',
    jwtMissing: 'Configurar JWT',
    openApi: 'OpenAPI',
    sourceCode: 'Código Fuente',
  },
  en: {
    eyebrow: 'Policy operations · .NET 10 · SQL Server',
    tabBlueprint: 'Blueprint',
    tabLifecycle: 'Lifecycle',
    tabConcurrency: 'Concurrency',
    tabAudit: 'Audit',
    tabResilience: 'Resilience',
    jwtConfigured: 'JWT Configured',
    jwtMissing: 'Configure JWT',
    openApi: 'OpenAPI',
    sourceCode: 'Source Code',
  },
} as const;

export type TranslationKey = keyof typeof translations.es;

export function t(key: TranslationKey): string {
  const loc = currentLocale.value;
  return translations[loc][key] ?? translations.es[key] ?? key;
}
