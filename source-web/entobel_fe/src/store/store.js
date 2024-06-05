import Vue from 'vue'
import Vuex from 'vuex'

Vue.use(Vuex)

// init api dafault link
let api = 'http://localhost:31195/api'
//let api = 'http://'+window.location.hostname+':31195/api'
// Create a new store instance.
const store = new Vuex.Store({
  state: {
    controller: api + '/Http',
    updateRate: 1000
  },

  getters: {
    getMyState: state => state.dataRtRms
  },

  mutations: {
    increment (state) {
      state.count++
    }
  }
})

export default store