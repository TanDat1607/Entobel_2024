<template>
  <v-dialog
    v-model="dialog"
    transition="dialog-bottom-transition"
    max-width="400">

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
          <v-toolbar-title>Mail Settings</v-toolbar-title>
        </v-toolbar>

        <div class="container-dialog">
          <div class="input-mail">
            <v-text-field
              v-model="mailAddress"
              label="Mail address"
              prepend-icon="mdi-mail"
              dense
            ></v-text-field>
            <v-btn 
              text
              color="primary"
              @click="$emit('onAdd', mailAddress)"
            >
              ADD
            </v-btn>
          </div>

          <MailList 
            :mails="mailAccount"
            @onDeleteMail="deleteMail"
          />
        </div>

        <v-divider></v-divider>

        <v-card-actions>
          <v-btn
            text
            @click="dialog=false"
          >
            CLOSE
          </v-btn>
        </v-card-actions>
      </v-card>
    </template>

  </v-dialog>

</template>

<script>
import MailList from '../components/MailList.vue'

export default {
  components: {
    MailList
  },
  props: {
    model: Boolean,
    mails: Array
  },
  watch: {
    model: function(newVal) {
      this.dialog = newVal
    },
    mails: function(newVal) {
      this.mailAccount = newVal
    },
    dialog: function(newVal) {
      if (newVal == false) {
        this.$emit('onClose')
      }
    }
  },
  methods: {
    deleteMail(mail) {
      this.$emit('onDeleteMail', mail)
    }
  },
  data () {
    return {
      dialog: false,
      // settings
      mailAddress: '',
      mailAccount: []
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
.input-mail {
  display: grid;
  gap: 10px;
  grid-template-columns: 5fr 1fr;
}
</style>
