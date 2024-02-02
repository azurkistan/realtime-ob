<script setup lang="ts">
const route = useRoute()
import '~/assets/css/index.css';
let cards = [
  {
    title: "Realtime Orderbook",
    description: "Track orderbook updates in realtime",
    img: "_nuxt/assets/proj/ob.gif",
    link: "/ob"
  },
  {
    title: "Journal",
    description: "Online trading journal",
    img: "_nuxt/assets/proj/journal.png",
    link: "/journal"
  }
]
</script>

<template>
  <div class="flex flex-wrap m-4 mt-24 justify-center">
    <div v-for="card in cards" class="md:w-1/3 px-4 mb-8 max-w-128">
      <NuxtLink :to=card.link class="page-card"
        :style="`background-image: url('${card.img}');`">
        <div class="text-white absolute bottom-0 left-0 p-4">
          <h2 class="text-2xl font-bold">{{ card.title }}</h2>
          <p class="mt-2">{{ card.description }}</p>
        </div>
      </NuxtLink>
    </div>
  </div>
</template>
