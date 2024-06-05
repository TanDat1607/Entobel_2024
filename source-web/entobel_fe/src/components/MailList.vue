<template>
  <div class="table-mails">
    <v-data-table
      :headers="headers"
      :items="mails"
      :items-per-page="5"
      :hide-default-header="true"
      style="height: 100%;"
    >
      <template v-slot:[`item.action`]="{ item }">
        <v-icon
          small
          @click="$emit('onDeleteMail', item)"
        >
          mdi-delete
        </v-icon>
      </template>
    </v-data-table>
  </div>
</template>

<script>
export default {
  props: {
    mails: Array,
    reset: Boolean
  },
  watch: {
    mails: function(newVal) {
      this.listMails = newVal
    }
  },
  methods: {
  },
  data() {
    return {
      // for table
      headers: [
        {
          text: 'Mail',
          align: 'start',
          sortable: true,
          value: 'mail'
        },
        { text: 'Action', value: 'action'}
      ],
      listMails: []
    }
  },
}
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
.table-mails {
  height: 100%;
  width: 100%;
}

</style>
