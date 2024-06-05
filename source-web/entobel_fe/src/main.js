import Vue from 'vue'
import App from './App.vue'
import vuetify from './plugins/vuetify'
import router from './router/index.js'
import store from './store/store'
import './main.css'
import 'material-design-icons-iconfont/dist/material-design-icons.css'

Vue.config.productionTip = false

new Vue({
  router,
  store,
  vuetify,
  render: h => h(App)
}).$mount('#app')
