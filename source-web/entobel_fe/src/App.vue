<template>
  <v-app>
    <div id="app">
      <router-view/>
    </div>
  </v-app>
</template>

<script>
//import Main from './views/Main.vue'

export default {
  name: 'App',
  components: {
    //Main
  }
}
</script>

<style>
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  color: #2c3e50;
  margin: 0px;
}
</style>
