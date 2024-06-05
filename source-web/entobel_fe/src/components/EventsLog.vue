<template>
  <div class="table-events">
    <v-data-table
      :headers="headers"
      :items="events"
      :items-per-page="3"
      :hide-default-footer="true"
      style="height: 100%;"
    >
    </v-data-table>
  </div>
</template>

<script>
export default {
  props: {
    events: Array,
    reset: Boolean
  },
  methods: {
  },
  data() {
    return {
      // for table
      headers: [
        {
          text: 'Event ID',
          align: 'start',
          sortable: true,
          value: 'eventId'
        },
        { text: 'Event Class', value: 'eventClass', width: '250', sortable: true },
        { text: 'Severity', value: 'severity', width: '150', sortable: true },
        { text: 'Text', value: 'text', sortable: true },
        { text: 'Source', value: 'source', sortable: true },
        { text: 'Time Raised', value: 'timeRaised', sortable: true }
      ],
    }
  },
  created() {
  }

}
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
.table-events {
  height: 100%;
  width: 100%;
}

</style>
