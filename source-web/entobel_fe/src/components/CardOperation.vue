<template>
    <v-card
      class="mx-auto"
      outlined
      style="width: 100%; padding-left: 10px; padding-right: 10px;"
    >
      <v-list-item three-line>
        <v-list-item-content>
          <div class="text-overline mb-4" style="line-height: normal;">
            {{overline}}
          </div>
          <v-list-item-title class="text-h5 mb-1" :style="`color: ${headlineColor}`">
            {{headline}}
          </v-list-item-title>
          <v-list-item-subtitle>{{subtitle}}</v-list-item-subtitle>
        </v-list-item-content>

        <v-list-item-avatar
          tile
          size="80"
        ><v-icon size="60" :style="`color: ${iconColor}`">{{icon}}</v-icon></v-list-item-avatar>
      </v-list-item>

    </v-card>
</template>

<script>

export default {
  components: {
  },
  props: {
    overline: String,
    headline: String,
    headlineColor: {
      type: String,
      default: "black"
    },
    subtitle: String,
    icon: String,
    iconColor: String,
  },
}
</script>

<style scope>
</style>