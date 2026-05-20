<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { ScheduleService } from '@/services/ScheduleService'
import LoadingSpinner from '@/components/LoadingSpinner.vue'
import ErrorAlert from '@/components/ErrorAlert.vue'
import type { IScheduleDay, IBreak } from '@/types/ISchedule'

const { t } = useI18n()

const DAY_NAMES: Record<number, string> = { 1: 'Monday', 2: 'Tuesday', 3: 'Wednesday', 4: 'Thursday', 5: 'Friday', 6: 'Saturday', 0: 'Sunday' }

const schedule = ref<IScheduleDay[]>([])
const breaks = ref<IBreak[]>([])
const isLoading = ref(true)
const saveErrors = ref<string[]>([])
const savingSchedule = ref(false)
const savingBreaks = ref(false)
const scheduleSuccess = ref(false)
const breaksSuccess = ref(false)

const fromDate = ref('')
const toDate = ref('')
const generating = ref(false)
const generateResult = ref<string | null>(null)
const generateError = ref<string | null>(null)

onMounted(async () => {
  const [schedResult, breakResult] = await Promise.all([
    ScheduleService.getSchedule(),
    ScheduleService.getBreaks(),
  ])
  if (schedResult.data) schedule.value = schedResult.data
  if (breakResult.data) breaks.value = breakResult.data
  isLoading.value = false
})

async function saveSchedule() {
  saveErrors.value = []
  scheduleSuccess.value = false
  savingSchedule.value = true
  const result = await ScheduleService.saveSchedule(schedule.value)
  savingSchedule.value = false
  if (result.errors) saveErrors.value = result.errors
  else scheduleSuccess.value = true
}

async function saveBreaks() {
  breaksSuccess.value = false
  savingBreaks.value = true
  const result = await ScheduleService.saveBreaks(
    breaks.value.map(({ dayOfWeek, startTime, endTime, label }) => ({ dayOfWeek, startTime, endTime, label })),
  )
  savingBreaks.value = false
  if (result.data) { breaks.value = result.data; breaksSuccess.value = true }
}

function addBreak() {
  breaks.value.push({ dayOfWeek: 1, startTime: '12:00:00', endTime: '13:00:00', label: '' })
}

function removeBreak(i: number) {
  breaks.value.splice(i, 1)
}

async function generateSlots() {
  generateResult.value = null
  generateError.value = null
  generating.value = true
  const result = await ScheduleService.generateSlots({ fromDate: fromDate.value, toDate: toDate.value })
  generating.value = false
  if (result.errors) generateError.value = result.errors.join(', ')
  else generateResult.value = `${result.data!.slotsGenerated} ${t('schedule.slotsGenerated')}`
}
</script>

<template>
  <div class="container py-4">
    <h2 class="mb-4">{{ t('schedule.title') }}</h2>
    <LoadingSpinner v-if="isLoading" />
    <div v-else>
      <!-- Weekly schedule -->
      <ErrorAlert v-if="saveErrors.length" :errors="saveErrors" />
      <div v-if="scheduleSuccess" class="alert alert-success">{{ t('common.saved') }}</div>
      <div class="table-responsive mb-3">
        <table class="table align-middle">
          <thead class="table-light">
            <tr>
              <th>{{ t('schedule.day') }}</th>
              <th>{{ t('schedule.working') }}</th>
              <th>{{ t('schedule.start') }}</th>
              <th>{{ t('schedule.end') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="day in schedule" :key="day.dayOfWeek">
              <td>{{ DAY_NAMES[day.dayOfWeek] }}</td>
              <td>
                <div class="form-check">
                  <input v-model="day.isWorkingDay" type="checkbox" class="form-check-input" />
                </div>
              </td>
              <td>
                <input v-model="day.startTime" type="time" class="form-control form-control-sm" :disabled="!day.isWorkingDay" style="max-width:120px" />
              </td>
              <td>
                <input v-model="day.endTime" type="time" class="form-control form-control-sm" :disabled="!day.isWorkingDay" style="max-width:120px" />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <button class="btn btn-primary mb-5" :disabled="savingSchedule" @click="saveSchedule">
        <span v-if="savingSchedule" class="spinner-border spinner-border-sm me-1" />
        {{ t('schedule.saveSchedule') }}
      </button>

      <!-- Breaks -->
      <h4>{{ t('schedule.breaks') }}</h4>
      <div v-if="breaksSuccess" class="alert alert-success">{{ t('common.saved') }}</div>
      <div v-for="(brk, i) in breaks" :key="i" class="row g-2 align-items-end mb-2">
        <div class="col-2">
          <label class="form-label small">{{ t('schedule.day') }}</label>
          <select v-model.number="brk.dayOfWeek" class="form-select form-select-sm">
            <option v-for="(name, d) in DAY_NAMES" :key="d" :value="Number(d)">{{ name }}</option>
          </select>
        </div>
        <div class="col-2">
          <label class="form-label small">{{ t('schedule.start') }}</label>
          <input v-model="brk.startTime" type="time" class="form-control form-control-sm" />
        </div>
        <div class="col-2">
          <label class="form-label small">{{ t('schedule.end') }}</label>
          <input v-model="brk.endTime" type="time" class="form-control form-control-sm" />
        </div>
        <div class="col-3">
          <label class="form-label small">{{ t('schedule.breakLabel') }}</label>
          <input v-model="brk.label" type="text" class="form-control form-control-sm" />
        </div>
        <div class="col-1">
          <button class="btn btn-sm btn-outline-danger" @click="removeBreak(i)">✕</button>
        </div>
      </div>
      <div class="d-flex gap-2 mb-5">
        <button class="btn btn-outline-secondary btn-sm" @click="addBreak">+ {{ t('schedule.addBreak') }}</button>
        <button class="btn btn-primary btn-sm" :disabled="savingBreaks" @click="saveBreaks">
          <span v-if="savingBreaks" class="spinner-border spinner-border-sm me-1" />
          {{ t('schedule.saveBreaks') }}
        </button>
      </div>

      <!-- Generate slots -->
      <h4>{{ t('schedule.generateSlots') }}</h4>
      <div v-if="generateResult" class="alert alert-success">{{ generateResult }}</div>
      <div v-if="generateError" class="alert alert-danger">{{ generateError }}</div>
      <div class="row g-2 align-items-end">
        <div class="col-md-3">
          <label class="form-label">{{ t('schedule.fromDate') }}</label>
          <input v-model="fromDate" type="date" class="form-control" required />
        </div>
        <div class="col-md-3">
          <label class="form-label">{{ t('schedule.toDate') }}</label>
          <input v-model="toDate" type="date" class="form-control" required />
        </div>
        <div class="col-md-2">
          <button class="btn btn-success w-100" :disabled="generating || !fromDate || !toDate" @click="generateSlots">
            <span v-if="generating" class="spinner-border spinner-border-sm me-1" />
            {{ t('schedule.generate') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
