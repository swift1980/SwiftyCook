<template>
  <div class="app">
    <!-- Sidebar component -->
    <Sidebar
      :open="sidebarOpen"
      @update:open="sidebarOpen = $event"
    />

    <!-- Mobile toggle button -->
    <button
      class="mobile-toggle"
      @click="sidebarOpen = !sidebarOpen"
      :aria-expanded="sidebarOpen"
      aria-label="Toggle sidebar"
    >
      ☰
    </button>

    <!-- Main panel -->
	  <MainContent />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import Sidebar from './components/Sidebar.vue';
import MainContent from './components/MainContent.vue';

const sidebarOpen = ref(false);
</script>

<style lang="scss">
.app {
  display: flex;
  min-height: 100vh;
  position: relative;
}

/* Mobile toggle button */
.mobile-toggle {
  position: fixed;
  top: 0.5rem;
  left: 0.5rem;
  z-index: 60;
  background: #111827;
  color: white;
  border: none;
  padding: 0.5rem 0.75rem;
  border-radius: 0.25rem;
  cursor: pointer;

  @media (min-width: 640px) {
    display: none;
  }
}

.main {
  flex: 1;
  display: flex;
  flex-direction: column;
  margin-left: 0; // default for mobile
  transition: margin-left 0.3s ease;

  /* When sidebar is open on mobile, shift content slightly for smoother feel */
  &.sidebar-open {
    @media (max-width: 639px) {
      pointer-events: none; // prevent accidental clicks while sidebar open
    }
  }

  @media (min-width: 640px) {
    margin-left: 16rem; // space for desktop sidebar
  }
}
</style>
