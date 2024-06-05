<template>
	<!-- ====== Forms Section Start -->
	<div class="flex h-fit lg:h-screen w-full justify-center items-center"
		:style="{ backgroundImage: `url(${require('@/assets/login-wallpaper.jpg')})` }">
		<div class="w-full md:w-1/2 lg:w-1/3 mx-auto my-10">
			<div class="flex flex-wrap items-stretch">
				<div class="w-full bg-slate-50">
					<div class="w-full py-14 px-6 sm:p-[70px] sm:px-12 xl:px-[90px]">
						<h2 class="mb-10 text-[32px] font-bold text-dark">Sign In</h2>
						<form>
							<div class="mb-8">
								<label class="mb-3 block text-xs text-body-color">
									User Name
								</label>
								<div class="w-full rounded-md border border-[#E9EDF4] py-3 px-[14px] text-body-color">
									<input class="outline-none focus:border-primary focus-visible:shadow-none"
									type="email"
									v-model="username"
									/>
								</div>
							</div>
							<div class="mb-8">
								<label
									class="mb-3 block border-[#ACB6BE] text-xs text-body-color"
								>
									Password
								</label>
								<div class="w-full rounded-md border border-[#E9EDF4] py-3 px-[14px] text-body-color">
									<input
										type="password"
										class="outline-none focus:border-primary focus-visible:shadow-none"
										v-model="password"
										/>
								</div>
							</div>
							<div class="mb-8">
								<input
									type="submit"
									value="Sign In"
									class="w-full cursor-pointer rounded-md border border-primary bg-primary py-3 px-[14px] text-white transition hover:bg-opacity-90"
									@click="login"
									/>
							</div>
						</form>
						<!-- <div class="flex flex-wrap justify-between">
							<a
								href="javascript:void(0)"
								class="mb-2 mr-2 inline-block text-base text-[#adadad] hover:text-primary hover:underline"
							>
								Forget Password?
							</a>
							<p class="mb-2 text-base text-[#adadad]">
								Not a member yet?
								<a
									href="javascript:void(0)"
									class="text-primary hover:underline"
								>
									Sign Up
								</a>
							</p>
						</div> -->
					</div>
				</div>
				<!-- <div class="flex w-full lg:w-1/2 bg-black/50">
					<div class="flex relative h-full w-full overflow-hidden justify-center items-center">
						<img :src="bgImg">
						
					</div>
				</div> -->
			</div>
		</div>
	</div>
	<!-- ====== Forms Section End -->

</template>

<script>
import api from '../store/api.js';

export default {
	components: {},

	methods: {
		login(event) {
      event.preventDefault();
			// init params
      var params = { username: this.username, password: this.password }
      api.post('/Http/login', null, {params})
        .then(response => {
          const accessToken = response.data.token;
					if (accessToken) {
						localStorage.setItem('accessToken', accessToken);
						api.defaults.headers.common['Authorization'] = `Bearer ${accessToken}`;
					}
          this.$router.push('/app');
        })
        .catch(error => {
          this.error = error.response.data.message;
        });
    }
	},

	data() {
    return {
			// interval
      interval : '',
			// auth data
			username: '',
      password: '',
      error: '',
			// bg image
			//bgImg: require('@/assets/img/3.png')
		}
	}
}
</script>