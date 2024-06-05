<template>
  <div class="chart-container">
    <canvas :class="id"></canvas>
  </div>
</template>

<script>
import Chart from 'chart.js'

export default {
  props: {
    update: Boolean,
    id: String,
    data: Object,
    //labels: Array,
    backgroundColor: {
      type: String,
      default: 'rgb(196, 175, 180, 0.5)'
    },
    borderColor: {
      type: String,
      default: '#ff6384'
    },
    stepSize: Number,
    stacked: {
      type: Boolean,
      default: false
    }
  },
  watch: {
    update: function() {this.chart.update()}
  },
  data() {
    return {
      chart: Chart,
      // chart configs
      chartData: {
        type: "bar",
        data: this.data,
        options: {
          responsive: true,
          maintainAspectRatio: false,
          legend: { 
            display: false
          },
          scales: {
            xAxes: [{
              stacked: this.stacked
            }],
            yAxes: [
              {
                stacked: this.stacked,
                ticks: {
                  beginAtZero: true,
                  padding: 25,
                  stepSize: 200
                },
              }
            ]
          }
        }
      }
    }
  },
  mounted() {
    const ctx = document.getElementsByClassName(this.id);
    this.chart = new Chart(ctx, this.chartData);
  }
}
</script>

<style scoped>
.chart-container {
  width: 100%;
  height: 100%;
}
</style>