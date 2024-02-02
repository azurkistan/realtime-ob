<template #default="{ bg }">
    <div class="page-card card-ob">
        <div class="page-card-header"></div>
        <div class="page-card-description">
        </div>
        <img class="pointer-events-none" :src="`/assets/proj/${bg}`" />
    </div>
</template>

<script setup>

const props = defineProps(['bg'])

</script>