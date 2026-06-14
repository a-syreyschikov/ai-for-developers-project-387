import { defineStore } from 'pinia'
import type { Booking, EventType, Owner, Slot } from '@/api/calendar'

interface BookingState {
  owner: Owner | null
  eventType: EventType | null
  selectedSlot: Slot | null
  createdBooking: Booking | null
}

export const useBookingStore = defineStore('booking', {
  state: (): BookingState => ({
    owner: null,
    eventType: null,
    selectedSlot: null,
    createdBooking: null,
  }),
  actions: {
    setSelection(owner: Owner, eventType: EventType, slot: Slot) {
      this.owner = owner
      this.eventType = eventType
      this.selectedSlot = slot
    },
    setCreatedBooking(booking: Booking) {
      this.createdBooking = booking
    },
    clearCreatedBooking() {
      this.createdBooking = null
    },
  },
})
