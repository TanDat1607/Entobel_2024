<template>
  <v-dialog
    v-model="dialog"
    transition="dialog-bottom-transition"
    max-width="300">

    <template>
      <v-card>
        <v-toolbar dark color="primary">
          <v-btn
            icon
            dark
            @click="dialog=false"
          >
            <v-icon>mdi-close</v-icon>
          </v-btn>
          <v-toolbar-title>Download Report</v-toolbar-title>
        </v-toolbar>

        <div class="container-dialog">
          <v-menu v-for="input in inputDates" :key="input.title"
            v-model="input.dateMenu"
            :close-on-content-click="true"
            transition="slide-y-transition"
            offset-y
            left
            min-width="auto"
          >
            <template v-slot:activator="{ on, attrs }">
              <v-text-field
                v-bind="attrs"
                v-on="on"
                v-model="input.date"
                :label="input.title"
                prepend-icon="mdi-calendar"
                readonly
                dense
              ></v-text-field>
            </template>
            <v-date-picker
              v-model="input.date"
            ></v-date-picker>
          </v-menu>

          <v-select v-for="input in inputTypes" :key="input.title"
            v-model="input.select"
            :label="input.title"
            :items="input.items"
            item-text="text"
            item-value="value"
            dense
          ></v-select>
        </div>

        <v-divider></v-divider>

        <v-card-actions>
          <v-btn
            color="primary"
            text
            @click="dialog=false, $emit('onDownload', form)"
          >
            DOWNLOAD
          </v-btn>
          <v-btn
            text
            @click="dialog=false"
          >
            CANCEL
          </v-btn>
        </v-card-actions>
      </v-card>
    </template>

  </v-dialog>

</template>

<script>
export default {
  props: {
    model: Boolean
  },
  watch: {
    model: function(newVal) {
      this.dialog = newVal
    },
    dialog: function(newVal) {
      if (newVal == false) {
        this.$emit('onCancel')
      }
    }
  },
  computed: {
    form: function() {
      return {
        startDate: this.inputDates[0].date,
        endDate: this.inputDates[1].date,
        timeRange: this.inputTypes[0].select,
        //type: this.inputTypes[1].select,
      }
    }
  },
  methods: {

  },
  data () {
    return {
      dialog: false,
      // for datepiacker
      inputDates: [{
          title: 'From date',
          dateMenu: false,
          date: new Date(Date.now()-(24*60*60*1000)*29).toISOString().substr(0, 10)
        }, {
          title: 'To date',
          dateMenu: false,
          date: new Date(Date.now()+(24*60*60*1000)).toISOString().substr(0, 10)
        }
      ],
      inputTypes: [{
          title: 'Select time group',
          select: '',
          items: [{
            text: 'All',
            value: 'All'
            },{
            text: 'Day',
            value: 'Day'
            },{
            text: 'Month',
            value: 'Month'
            },{
            text: 'Year',
            value: 'Year',
          }]}
        // }, {
        //   title: 'Select data type',
        //   select: '',
        //   items: [{
        //     text: 'Total Weights',
        //     value: 'weight'
        //     },{
        //     text: 'Total Cups',
        //     value: 'cups'
        //     },{
        //     text: 'Total Op. Hours',
        //     value: 'optime'
        //   }]
        // }
      ]
    }
  }
}
</script>

<style scoped>
.container-dialog {
  padding: 10px;
  background-color: white;
  border-radius: 10px;
}
</style>
