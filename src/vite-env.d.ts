/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_LIBCAL_CLIENT_ID: string
  readonly VITE_LIBCAL_CLIENT_SECRET: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
