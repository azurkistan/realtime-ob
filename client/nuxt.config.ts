export default defineNuxtConfig({
  devtools: { enabled: true },
  modules: ['@nuxtjs/tailwindcss'],
  imports: {
    dirs: ['types/*.ts']
  }
})
