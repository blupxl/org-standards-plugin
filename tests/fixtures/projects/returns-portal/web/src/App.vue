<script setup>
import { ref } from "vue";

const reason = ref("");
</script>

<template>
  <form>
    <label>Why are you returning this item?
      <input v-model="reason" />
    </label>
  </form>
</template>
