//import { createWebHistory } from "vue-router";
import Vue from 'vue'
import Router from 'vue-router'
import api from '../store/api.js';

// ------ VIEWS ------
import LoginPage from '../views/LoginPage.vue'
import Main from '../views/Main.vue'

Vue.use(Router)

const routes = [
  { path: '', redirect: '/login' },
  { path: '/login', component: LoginPage },
  { path: '/app', component: Main}
];

// create router
const router = new Router({
  //history: createWebHistory(),
  routes,
});

// verify auth before each route
router.beforeEach(async(to, from, next) => {
  if (to.meta.requiresAuth) {
    // verify token with server
    const auth = await api.post('/auth')
      .then(() => true)
      .catch(error => {
        let msg = error.response.data.message;
        console.log(`Authorization failed: ${msg}`)
      });
    // Check if token exists and is valid
    if (auth) {
      next(); // Proceed to the route
    } else {
      next('/login'); // Redirect to login page if token is invalid or missing
    }
  } else {
    next(); // Allow access to routes that don't require authentication
  }
});

export default router;
  
