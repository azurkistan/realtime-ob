export default defineNuxtConfig({
  ssr: true,
  devtools: { enabled: true },
  modules: ['@nuxtjs/tailwindcss'],
  imports: {
    dirs: ['types/*.ts']
  },
  runtimeConfig: {
    public: {
      SERVER_URL: "http://localhost:5000",
      FPS: 30,
    }
  }
})
