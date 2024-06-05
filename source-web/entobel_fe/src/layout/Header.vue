<template>
  <div class="header">
    <img src="@/assets/plenma_logo.png" class="img">
    <img src="@/assets/s4m_logo.png" class="img">
    <h1>Larvae Check Weigher</h1>
    <div style="width: 100%;">

      <v-menu
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
            <v-icon>mdi-cog</v-icon>
          </v-btn>
        </template>
        <v-list style="padding: 10px;">
          <v-list-item-group
            v-model="selectSetting"
            color="primary"
          >
            <v-list-item
              v-for="item in settings"
              :key="item"
              @click="item.onClick"
            >
              <v-list-item-title>{{ item.title }}</v-list-item-title>
            </v-list-item>
          </v-list-item-group>
        </v-list>
      </v-menu>

      <v-tooltip bottom>
        <template v-slot:activator="{ on, attrs }">
          <v-btn
            v-bind="attrs"
            v-on="on"
            @click="onDownloadEvents"
            icon
            color="lightgray"
            style=
              "float: right;
              margin-right: 10px;
              top: 47%;
              -ms-transform: translateY(-50%);
              transform: translateY(-50%);"
          >
            <v-icon>mdi-file-download-outline</v-icon>
          </v-btn>
        </template>
        <span>Download events log</span>
      </v-tooltip>

      <v-tooltip bottom>
        <template v-slot:activator="{ on, attrs }">
          <v-btn
            v-bind="attrs"
            v-on="on"
            @click="dialogDownload = true"
            icon
            color="lightgray"
            style=
              "float: right;
              margin-right: 10px;
              top: 47%;
              -ms-transform: translateY(-50%);
              transform: translateY(-50%);"
          >
            <v-icon>mdi-tray-arrow-down</v-icon>
          </v-btn>
        </template>
        <span>Download report</span>
      </v-tooltip>

    </div>

    <DownloadReport
      :model="dialogDownload"  
      @onDownload="onDownload"
      @onCancel="onCancel"
    />
    <MailSetting
      :model="dialogMail"  
      :mails="listMail"
      @onAdd="onAddMail"
      @onClose="onCloseMail"
      @onDeleteMail="deleteMail"
    />
  </div>
</template>

<script>
import axios from 'axios'
import DownloadReport from '../components/DownloadReport.vue'
import MailSetting from '../components/MailSetting.vue'

export default {
  components: {
    DownloadReport,
    MailSetting
  },
  methods: {
    onDownload(form) {
      // init query
      var params = {
        startDate: form.startDate,
        endDate: form.endDate+'T23:59:59',
        timeRange: form.timeRange,
        //type: form.type
      }
      // init file name
      //var fileName = params.timeRange + '-' + params.type + ' ' + params.startDate + ' ' + params.endDate
      //var dateDispOffset = 1; //1 day
      var start = new Date(params.startDate)
      //start.setDate(start.getDate() - dateDispOffset)
      start = new Date(start).toISOString().substr(0, 10)
      var end = new Date(params.endDate)
      //end.setDate(end.getDate() - dateDispOffset)
      end = new Date(end).toISOString().substr(0, 10)
      var fileName = params.timeRange + ' ' + start + ' ' + end
      // send cmd
      axios.get(this.$store.state.controller + '/DownloadReport', {responseType: 'blob', params})
        .then(response => {
          var fileURL = window.URL.createObjectURL(new Blob([response.data]));
          var fileLink = document.createElement('a');
          fileLink.href = fileURL;
          fileLink.setAttribute('download', fileName + '.csv');
          document.body.appendChild(fileLink);
          fileLink.click();
        })
    },
    onCancel() {
      this.dialogDownload = false
    },
    onDownloadEvents() {
      axios.get(this.$store.state.controller + '/DownloadEvents')
        .then(response => {
          var fileURL = window.URL.createObjectURL(new Blob([response.data]));
          var fileLink = document.createElement('a');
          fileLink.href = fileURL;
          fileLink.setAttribute('download', 'eventslog.csv');
          document.body.appendChild(fileLink);
          fileLink.click();
        })
    },
    onAddMail(mail) {
      // init params
      var params = {mail: mail}
      // cmd
      axios.post(this.$store.state.controller + '/InsertMail', params)
        .then(() => (this.getListMail())) // update listmail when done
    },
    onCloseMail() {
      this.dialogMail = false
    },
    getListMail() {
      axios.get(this.$store.state.controller + '/ListMail')
        .then(response => {this.listMail = response.data})
    },
    deleteMail(mail) {
      axios.delete(this.$store.state.controller + '/DeleteMail/' + mail.id)
        .then(() => (this.getListMail())) // update listmail when done
    }
  },

  created() {
    // get list mail from db on created
    this.getListMail()
  },

  data() {
    var self = this;
    return{
      dialogDownload: false, //for downlaod report dialog
      dialogMail: false,
      // for mail table
      listMail: [],
      // setting menu
      selectSetting: '',
      settings: [{
        title: 'Mail Account',
        onClick: function() {
          self.dialogMail = true
        }
      }]
    }
  }
}
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
.header {
  background-color: #F4F3EF;
  margin:0px;
  overflow: hidden;
  display: grid;
  grid-template-columns: 120px 100px 300px 1fr;
  gap: 0px;
}
img {
  width: 100%;
  padding: 10px;
}
h1 {
  font-size: 18px;
  color: #6e6e6e;
  line-height: 10px;
  padding: 20px 0;
  margin-left: 10px;
}
  

</style>
