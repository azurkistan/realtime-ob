export default defineNuxtConfig({
  ssr: true,
  devtools: { enabled: true },
  modules: ['@nuxtjs/tailwindcss'],
  imports: {
    dirs: ['types/*.ts']
  }
})
