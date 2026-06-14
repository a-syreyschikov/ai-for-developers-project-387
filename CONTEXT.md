# Calendar

Calendar is a simplified appointment scheduling context for one calendar owner and guests who book meetings with that owner.

## Language

**Owner**:
The single preconfigured calendar owner who offers meeting times.
_Avoid_: account, admin user, host

**Guest**:
A person without an account who selects a meeting type, chooses a free slot, and creates a booking.
_Avoid_: customer, client, user

**EventType**:
A type of meeting a guest can book, defined by title, description, and duration.
_Avoid_: service, appointment kind

**Schedule**:
The owner's weekly availability template in the owner's time zone.
_Avoid_: availability window, calendar rule

**Slot**:
A computed free interval for one event type. A slot is not stored as a resource.
_Avoid_: appointment, availability window

**Booking**:
A stored guest reservation for a selected slot.
_Avoid_: slot, meeting request
