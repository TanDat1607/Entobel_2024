<template>
  <div class="main">
    <Header/>
    <v-divider/>
    <div class="display">
      <div class="operation">
        <CardOperation
          overline="Machine status"
          :headline="statusText"
          :headlineColor="statusColor"
          :icon="statusIcon"
          :iconColor="statusColor"
        >
        </CardOperation>

        <CardOperation
          overline="Current User"
          :headline="opData.user"
          subtitle=""
          icon="mdi-file-document-check-outline"
          iconColor="#F3BB45"
        >
        </CardOperation>

        <CardOperation
          overline="Larvae Capacity"
          :headline="String(opData.weight.toFixed(2) ?? 0)"
          subtitle="g/h"
          icon="mdi-scale"
          iconColor="#ff6384"
        >
        </CardOperation>

        <CardOperation
          overline="Cup Capacity"
          :headline="String(opData.capacity)"
          subtitle="cup/h"
          icon="mdi-cup-outline"
          iconColor="#76a1f5"
        >
        </CardOperation>
      </div>

    <div class="history">
        <div class="history-chart" v-for="chart in charts" :key="chart.id"
          :style="
            `grid-row-start: ${chart.rowStart}; 
            grid-row-end: ${chart.rowEnd};
            grid-column-start: ${chart.colStart};
            grid-column-end: ${chart.colEnd};`
          "
        >
          <div class="time-picker">
            <h2>{{chart.title}}</h2>
            <div>
              <v-menu
                v-model="chart.timeMenu"
                transition="slide-y-transition"
                offset-y
                left
                min-width="auto"
              >
                <template v-slot:activator="{ on, attrs }">
                  <v-btn
                    v-bind="attrs"
                    v-on="on"
                    icon
                    color="gray"
                    style=
                      "float: right;
                      margin-right: 10px;
                      top: 47%;
                      -ms-transform: translateY(-50%);
                      transform: translateY(-50%);"
                  >
                    <v-icon>mdi-menu</v-icon>
                  </v-btn>
                </template>
                <v-list style="padding: 10px;">
                  <v-list-item-group
                    v-model="chart.selectTimeRange"
                    color="primary"
                    @change="updateChart(chart.id, timeRange[chart.selectTimeRange].title, weightCmd[chart.selectWeight])"
                  >
                    <v-list-item
                      v-for="(item, index) in timeRange"
                      :key="index"
                      link
                    >
                      <v-list-item-title>{{ item.title }}</v-list-item-title>
                    </v-list-item>
                  </v-list-item-group>
                </v-list>
              </v-menu>

              <v-menu
                v-model="chart.dateMenu"
                :close-on-content-click="false"
                transition="slide-y-transition"
                offset-y
                left
                min-width="auto"
              >
                <template v-slot:activator="{ on, attrs }">
                  <v-btn
                    v-bind="attrs"
                    v-on="on"
                    icon
                    color="gray"
                    style=
                      "float: right;
                      margin-right: 10px;
                      top: 47%;
                      -ms-transform: translateY(-50%);
                      transform: translateY(-50%);"
                  >
                    <v-icon>mdi-calendar</v-icon>
                  </v-btn>
                </template>
                <v-date-picker
                  v-model="chart.dates"
                  @input="chart.dateMenu = checkSelectDates(chart)"
                  range
                ></v-date-picker>
              </v-menu>

              <v-menu
                v-model="chart.weightMenu"
                transition="slide-y-transition"
                offset-y
                left
                min-width="auto"
              >
                <template v-slot:activator="{ on, attrs }">
                  <v-btn
                    v-bind="attrs"
                    v-on="on"
                    icon
                    color="gray"
                    style=
                      "float: right;
                      margin-right: 10px;
                      top: 47%;
                      -ms-transform: translateY(-50%);
                      transform: translateY(-50%);"
                  >
                    {{weightCmd[chart.selectWeight].toFixed(1) ?? 0}}
                  </v-btn>
                </template>
                <v-list style="padding: 10px;">
                  <v-list-item-group
                    v-model="chart.selectWeight"
                    color="primary"
                    @change="updateChart(chart.id, timeRange[chart.selectTimeRange].title, weightCmd[chart.selectWeight])"
                  >
                    <v-list-item
                      v-for="item in weightCmd"
                      :key="item"
                      link
                    >
                      <v-list-item-title>{{ item.toFixed(1) ?? 0 }}</v-list-item-title>
                    </v-list-item>
                  </v-list-item-group>
                </v-list>
              </v-menu>

              <h5 v-if="chart.id != 'optime'" :class="chart.textReqId">Required weight:</h5>
            </div>
            
          </div>

          <LineChart v-if="chart.id == 'output' || chart.id == 'food'"
            :update="chart.isUpdate"
            :id="chart.id"
            :data="chart.data"
            :labels="chart.labels"
            :backgroundColor="chart.backgroundColor"
            :borderColor="chart.borderColor"
          />
          <BarChart v-else
            :update="chart.isUpdate"
            :id="chart.id"
            :data="chart.data"
            :stacked="chart.stacked"
            :backgroundColor="chart.backgroundColor"
            :borderColor="chart.borderColor"
          />
        </div>

        <EventsLog 
          :events="events"
          style="
            grid-row-start: 7; 
            grid-row-end: 9;
            grid-column-start: 1;
            grid-column-end: 3;
          "
        />
      </div>

    </div>
  </div>
</template>

<script>
import axios from 'axios'
import Header from '../layout/Header.vue'
import CardOperation from '../components/CardOperation.vue'
import EventsLog from '../components/EventsLog.vue'
import LineChart from '../charts/LineChart.vue'
import BarChart from '../charts/BarChart.vue'


export default {
  components: {
    Header,
    CardOperation,
    EventsLog,
    LineChart,
    BarChart
  },
  watch: {
    weightCmd: function() {
      // read db for charts
      for (var i = 0; i < this.charts.length; i++) {
        this.updateChart(this.charts[i].id, this.timeRange[this.charts[i].selectTimeRange].title, this.weightCmd[0])
      }
    }
  },

  methods: {
    checkMachineStatus(status) {
      if (status == 'ERROR') {
        this.statusText = 'Error'
        this.statusColor = 'Red'
        this.statusIcon = 'mdi-close-circle'
      }
      else if (status == 'RUN') {
        this.statusText = 'Run'
        this.statusColor = 'Green'
        this.statusIcon = 'mdi-check-circle'
      }
      else if (status == 'STOP') {
        this.statusText = 'Stop'
        this.statusColor = 'Gray'
        this.statusIcon = 'mdi-pause-circle'
      }
      else {
        this.statusText = 'Offline'
        this.statusColor = 'Gray'
        this.statusIcon = 'mdi-close-circle'
      }
    },
    //
    checkSelectDates(chart) {
      if (chart.dates[1] != null) {
        this.updateChart(chart.id, this.timeRange[chart.selectTimeRange].title, this.weightCmd[chart.selectWeight])
        return false
      }
      else return true
    },
    // get list of unique weights
    getWeightCmd() {
      axios.get(this.$store.state.controller + '/ListWeightCmd')
        .then(response => (this.weightCmd = response.data, console.log(response.data)))
    },
    // read db for charts
    updateChart(id, timeRange, weightCmd) {
      var i = 0
      // get matching array index
      for (i = 0; i < this.charts.length; i++) {
        if (this.charts[i].id == id) {
          break
        }
      }
      // init params
      var params = {}
      // reorder dates & init params query
      if (this.charts[i].dates[0] < this.charts[i].dates[1]) {
        params = {
          startDate: this.charts[i].dates[0],
          endDate: this.charts[i].dates[1],
          timeRange: timeRange,
          weightCmd: weightCmd,
          station: 1
        }
      }
      else {
        params = {
          startDate: this.charts[i].dates[1],
          endDate: this.charts[i].dates[0],
          timeRange: timeRange,
          weightCmd: weightCmd,
          station: 1
        }
      }
      // clear datasets
      for (var iSet = 0; iSet < this.charts[i].data.datasets.length; iSet++) 
        this.charts[i].data.datasets[iSet].data = new Array()
      this.charts[i].data.labels = new Array()
      console.log(this.charts[i].data)
      // check id chart
      var cmd = new Array()
      if (id == 'output') { cmd.push('/ListWeight/1', '/ListWeight/2') }
      else if (id == 'food') { cmd.push('/ListFoodWeight/1', '/ListFoodWeight/2') }
      else if (id == 'cups') { cmd.push('/ListCupOutput/1', '/ListCupOutput/2') }
      else if (id == 'optime') { cmd.push('/ListOptime', '/ListDowntime') }
      // init request array
      var request = new Array()
      for (var iReq = 0; iReq < this.charts[i].data.datasets.length; iReq++){
        request.push(axios.get(this.$store.state.controller + cmd[iReq], {params}))
      }
      // init response array
      Promise.all(request)
        .then(response => {
          // loop all datasets
          for (var iRes = 0; iRes < response.length; iRes++) {
            // loop each data in a dataset
            for (var iResData = 0; iResData < response[iRes].data.length; iResData++) {
              // check chart id
              var val = 0
              if (id == 'optime') { val = response[iRes].data[iResData].optime }
              else if (id == 'cups') { val = response[iRes].data[iResData].count }
              else if (id == 'output') { val = response[iRes].data[iResData].weight }
              else if (id == 'food') { val = response[iRes].data[iResData].weight }
              // push response data to array
              this.charts[i].data.datasets[iRes].data.push(val)
              // stop push labels if iSet > 0
              if (iRes == 0) {
                this.charts[i].data.labels.push(response[iRes].data[iResData].id)
              }
            }
          }
          console.log(this.charts[i].data)
          // update chart
          this.charts[i].isUpdate = !this.charts[i].isUpdate
        })

      // // loop every datasets
      // var iSet = 0;
      // var bFlag = false
      // while (iSet < this.charts[i].data.datasets.length)
      // {
      //   // only call request if flag not raised
      //   if (!bFlag) {
      //     // raise flag
      //     bFlag = true
      //     // send cmd
      //     axios.get(this.$store.state.controller + cmd[iSet], {params})
      //       .then(response => {
      //         for (var iRes = 0; iRes < response.data.length; iRes++) {
      //           // check id
      //           var val = 0
      //           if (id == 'optime') { val = response.data[iRes].optime }
      //           else if (id == 'cups') { val = response.data[iRes].count }
      //           else if (id == 'output') { val = response.data[iRes].weight }
      //           // push response data to array
      //           this.charts[i].data.datasets[iSet].data.push(val)
      //           //this.charts[i].data.datasets[1].data.push(24 - response.data[iRes].optime)
      //           // stop push labels if iSet > 0
      //           console.log(iSet)
      //           if (iSet == 0) {
      //             this.charts[i].data.labels.push(response.data[iRes].id)
      //           }
      //           console.log(this.charts[i].data)
      //         }
      //         this.charts[i].isUpdate = !this.charts[i].isUpdate
      //         // deraise flag
      //         bFlag = false
      //         iSet++
      //       })
      //   }
      // }
    },
    // read ADS events
    getEvents() {
      axios.get(this.$store.state.controller + '/ReadLoggedEvents')
        .then(response => (this.events = response.data))
    }
  },

  mounted() {
    // read all unique weight cmd
    this.getWeightCmd()
    // read events at start
    this.getEvents()
    // update realtime data every interval
    setInterval(() => {
      axios.get(this.$store.state.controller + '/GetProductionData')
        .then(response => {
          console.log(response.data)
          // update status if data read
          if (response.data != null) {
            this.opData = response.data
            this.checkMachineStatus(response.data.status)
          }
          // show offline if no data read
          else this.checkMachineStatus(-1)
        })
        .catch(err => { 
          console.log(err)
          this.checkMachineStatus(-1)
        })
    }, this.$store.state.updateRate)
    // update events data every interval
    setInterval(() => { this.getEvents() }, this.$store.state.updateRate*60)
  },

  data() {
    return {
      // data for status card
      statusText: 'Offline',
      statusColor: 'gray',
      statusIcon: 'mdi-close-circle',
      // data for rt cards
      opData: {
        status: 0,
        weight: 0,
        capacity: 0,
        power: 0
      },
      // cup weight cmd data in db
      weightCmd: [0],
      // data for charts
      charts: [{
          isUpdate: false,
          id: 'output',
          dateMenu: false,
          dates: [new Date(Date.now()-(24*60*60*1000)*29).toISOString().substr(0, 10), new Date(Date.now()+(24*60*60*1000)).toISOString().substr(0, 10)],
          timeMenu: false,
          selectTimeRange: 0,
          weightMenu: false,
          selectWeight: 0,
          title: "Larvae Production (g)",
          textReqId: "req-weight-output",
          // data: [],
          // labels: [],
          data: {
            labels: [],
            datasets: [ 
              {
                label: 'Station 1',
                data: [],
                backgroundColor: 'rgba(54, 162, 235, 0.2)',
                borderColor: '#76a1f5',
                borderWidth: 3,
                lineTension: 0,
              },
              {
                label: 'Station 2',
                data: [],
                backgroundColor: 'rgba(255, 99, 132, 0.2)',
                borderColor: '#eb6767',
                borderWidth: 3,
                lineTension: 0,
              }
            ]
          },
          //backgroundColor: 'rgb(151, 164, 189, 0.5)',
          //borderColor: '#76a1f5',
          rowStart: 1,
          rowEnd: 4,
          colStart: 1,
          colEnd: 3,
        },{
          isUpdate: false,
          id: 'food',
          dateMenu: false,
          dates: [new Date(Date.now()-(24*60*60*1000)*29).toISOString().substr(0, 10), new Date(Date.now()+(24*60*60*1000)).toISOString().substr(0, 10)],
          timeMenu: false,
          selectTimeRange: 0,
          weightMenu: false,
          selectWeight: 0,
          title: "Food Production (g)",
          textReqId: "req-weight-output",
          // data: [],
          // labels: [],
          data: {
            labels: [],
            datasets: [ 
              {
                label: 'Station 1',
                data: [],
                backgroundColor: 'rgba(54, 162, 235, 0.2)',
                borderColor: '#76a1f5',
                borderWidth: 3,
                lineTension: 0,
              },
              {
                label: 'Station 2',
                data: [],
                backgroundColor: 'rgba(255, 99, 132, 0.2)',
                borderColor: '#eb6767',
                borderWidth: 3,
                lineTension: 0,
              }
            ]
          },
          //backgroundColor: 'rgb(151, 164, 189, 0.5)',
          //borderColor: '#76a1f5',
          rowStart: 4,
          rowEnd: 7,
          colStart: 1,
          colEnd: 3,
        }, {
          isUpdate: false,
          id: 'cups',
          dateMenu: false,
          dates: [new Date(Date.now()-(24*60*60*1000)*29).toISOString().substr(0, 10), new Date(Date.now()+(24*60*60*1000)).toISOString().substr(0, 10)],
          timeMenu: false,
          selectTimeRange: 0,
          weightMenu: false,
          selectWeight: 0,
          stacked: false,
          title: "Cups Production",
          textReqId: "req-weight-cups",
          //data: [],
          //labels: [],
          data: {
            labels: [],
            datasets: [
              {
                label: 'Station 1',
                data: [],
                backgroundColor: '#76a1f5',
              },
              {
                label: 'Station 2',
                data: [],
                backgroundColor: '#eb6767',
              }
            ]
          },
          //backgroundColor: 'rgb(169, 212, 183, 0.5)',
          borderColor: '',
          rowStart: 1,
          rowEnd: 5,
          colStart: 3,
          colEnd: 4,
        }, {
          isUpdate: false,
          id: 'optime',
          dateMenu: false,
          dates: [new Date(Date.now()-(24*60*60*1000)*29).toISOString().substr(0, 10), new Date(Date.now()+(24*60*60*1000)).toISOString().substr(0, 10)],
          timeMenu: false,
          selectTimeRange: 0,
          stacked: true,
          title: "Op. Time (h)",
          //data: [],
          //labels: [],
          data: {
            labels: [],
            datasets: [
              {
                label: 'Operation time',
                data: [],
                backgroundColor: '#70cf87',
              },
              {
                label: 'Downtime',
                data: [],
                backgroundColor: '#f27474',
              }
            ]
          },
          borderColor: '',
          rowStart: 5,
          rowEnd: 8,
          colStart: 3,
          colEnd: 4,
        }
      ],
      // time menu
      timeRange: [
        { title: 'Day' },
        { title: 'Month' },
        { title: 'Year' }
      ],
      // date picker
      // menu2: null,
      // date: (new Date(Date.now() - (new Date()).getTimezoneOffset() * 60000)).toISOString().substr(0, 10),
      // endDate: new Date(Date.now()+(24*60*60*1000)).toISOString().substr(0, 10),
      // startDate: new Date(Date.now()-(24*60*60*1000)*29).toISOString().substr(0, 10), 
      // dates: ['2022-06-16', '2022-07-16'],
      // events log
      events: []
    }
  }
}
</script>

<style scoped>
.main {
  background: rgb(231, 231, 231);
  height: 100%;
  width: auto;
  display: grid;
  grid-template-rows: 50px 1px 1fr;
}
.display {
  height: auto;
  width: auto;
  display: grid;
  margin: 10px;
  grid-template-rows: 120px 1fr;
  gap: 15px;
}
.operation {
  width: auto;
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 15px;
}
.time-picker {
  height: 100%;
  width: 100%;
  overflow: hidden;
  display: grid;
  grid-template-columns: 220px 1fr;
  gap: 10px;
}
h2 {
  font-size: 14px;
  color: #6e6e6e;
  line-height: 10px;
  padding: 20px;
  text-align: left;
}
h5 {
  color: #6e6e6e;
  text-align: right;
  float: right;
  margin: 0;
  padding: 15px;
}
.history {
  width: 100%;
  display: grid;
  grid-template-rows: repeat(8, 100px);
  grid-template-columns: 1fr 1fr 1fr;
  gap: 15px;
}
.history-chart {
  width: 100%;
  background: white;
  border-radius: 3px;
  display: grid;
  grid-template-rows: 50px 1fr;
}

@media only screen and (max-width: 1200px) {
  .req-weight-cups {
    display: none;
  }
}
</style>