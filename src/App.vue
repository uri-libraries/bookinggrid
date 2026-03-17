<template>
  <div id="app">
    <!-- Primo NDE Header -->
    <nav class="primo-header">
      <div class="primo-header-container">
        <div class="primo-logo">
          <img src="/library-logo.png" alt="URI Library" />
        </div>
        <button class="mobile-menu-toggle" @click="mobileMenuOpen = !mobileMenuOpen" aria-label="Toggle menu">
          <span class="hamburger-line"></span>
          <span class="hamburger-line"></span>
          <span class="hamburger-line"></span>
        </button>
        <ul class="primo-nav" :class="{ 'mobile-open': mobileMenuOpen }">
          <li><a href="https://uri.libguides.com/az/databases" target="_blank" @click="mobileMenuOpen = false">Databases @ URI</a></li>
          <li><a href="https://uri.primo.exlibrisgroup.com/nde/jsearch?vid=01URI_INST:01URI_INST_NDE&lang=en" target="_blank" @click="mobileMenuOpen = false">Journal Search</a></li>
          <li><a href="https://libkey.io/libraries/245/" target="_blank" @click="mobileMenuOpen = false">Get PDF By DOI/PMID</a></li>
          <li><a href="https://uri.primo.exlibrisgroup.com/nde/citationlinker?lang=en&vid=01URI_INST%3A01URI_INST_NDE" target="_blank" @click="mobileMenuOpen = false">Search by Citation</a></li>
          <li><a href="https://uri.illiad.oclc.org/illiad/" target="_blank" @click="mobileMenuOpen = false">ILL Request</a></li>
        </ul>
      </div>
    </nav>
    
    <img src="/URI_11-16_21-3.jpg" alt="Library Header" class="header-image" />
    <h1>Reserve a Study Room at the Carothers Library</h1>

    <div class="header">
      <p>Viewing: {{ formatDate(currentDate) }}</p>
      <div>
        <button v-if="isToday" @click="goToTomorrow">Tomorrow</button>
        <button v-if="!isToday" @click="goToToday">Today</button>
      </div>
    </div>

  <nav class="filters">
      <label>
        Floor
        <select v-model="selectedZone">
          <option v-for="zone in zones" :key="zone" :value="zone">{{ zone }}</option>
        </select>
      </label>
      <label>
        Room
        <select v-model="selectedRoomName">
          <option v-for="name in roomNames" :key="name" :value="name">{{ name }}</option>
        </select>
      </label>
      <label>
        Capacity
        <select v-model="selectedCapacity">
          <option value="All">All</option>
          <option v-for="cap in capacities" :key="cap" :value="cap">{{ cap }}</option>
        </select>
      </label>
    </nav>

    <div v-if="loading" class="rooms">
      <div v-for="n in 12" :key="n" class="room-card skeleton">
        <div class="skeleton-title"></div>
        <div class="skeleton-meta"></div>
        <div class="skeleton-timeline"></div>
      </div>
    </div>
    <div v-else-if="error">{{ error }}</div>

    <div v-else class="rooms">


      <div v-if="filteredSortedRooms.length === 0" class="no-rooms">No rooms available for this date.</div>
      <div v-for="room in filteredSortedRooms" :key="room.id" class="room-card" :class="{ expanded: expandedRooms.includes(room.id), closing: closingRooms.includes(room.id) }" @click="toggleExpanded(room.id)">
        <div v-if="isRoomAvailableNow(room)" class="availability-pill">
          <span class="green-dot"></span>Available Now
        </div>
        <h3>{{ room.name }}<span v-if="expandedRooms.includes(room.id)"> - {{ room.zone }} - {{ formatDate(currentDate) }}</span></h3>
        <div class="room-meta">Capacity: <strong>{{ room.capacity ?? 'Unknown' }}</strong></div>
        <img v-if="expandedRooms.includes(room.id)" src="/facade.jpg" alt="Library Facade" class="modal-facade" @click.stop />
        <div class="timeline" style="position: relative; height: 24px;" @mousemove="updateHoverTime" @mouseleave="clearHoverTime" @click.stop="onTimelineClick(room, $event)">
          <div class="time-label" style="left: 0%;">{{ formatTimeLabel('start') }}</div>
          <div class="time-label" style="left: 50%;">{{ formatTimeLabel('middle') }}</div>
          <div class="time-label" style="right: 0%;">{{ formatTimeLabel('end') }}</div>
          <div v-if="hoverTime && expandedRooms.includes(room.id)" class="hover-tooltip" :style="{ left: hoverLeft + 'px' }">{{ hoverTime }}</div>
          <div v-if="selectedTimes[room.id] && expandedRooms.includes(room.id)" class="selected-booking" :style="getSelectedBookingStyle(room.id)">{{ duration }} min</div>
          <div
            v-for="(segment, idx) in room.availability && Array.isArray(room.availability)
              ? getTimelineSegments(room.availability)
              : getBookingsForRoom(room.id)"
            :key="segment.id || idx"
            class="booking-segment"
            :style="getBookingStyle(segment)"
            :title="formatTime(segment.from || segment.fromDate) + ' - ' + formatTime(segment.to || segment.toDate)"
          ></div>
        </div>
        <div v-if="expandedRooms.includes(room.id)" class="time-selectors" @click.stop>
          <label>Start Time: 
            <select v-model="selectedStarts[room.id]" @change="updateSelection(room.id)">
              <option value="">Select Start</option>
              <option v-for="time in getAvailableStartTimes(room)" :value="time" :key="time">{{ time }}</option>
            </select>
          </label>
          <label>End Time: 
            <select v-model="selectedEnds[room.id]" @change="updateSelection(room.id)" :key="`end-${room.id}-${selectedStarts[room.id]}`">
              <option value="">Select End</option>
              <option v-for="time in getAvailableEndTimes(room, selectedStarts[room.id])" :value="time" :key="time">{{ time }}</option>
            </select>
          </label>
        </div>
        <div v-if="expandedRooms.includes(room.id) && selectedTimes[room.id]" @click.stop class="booking-container">
          <form @submit.prevent="bookRoom(room)" class="booking-form">
            <h4>Book this room from {{ formatTime(minutesToTime(selectedTimes[room.id])) }} to {{ formatTime(minutesToTime(selectedTimes[room.id] + duration)) }}</h4>
            <input v-model="fname" type="text" placeholder="First Name" required />
            <input v-model="lname" type="text" placeholder="Last Name" required />
            <input v-model="email" type="email" placeholder="URI.edu Email" required pattern=".*@uri\.edu$" />
            <div class="terms-checkbox">
              <input type="checkbox" :id="`terms-${room.id}`" v-model="termsAccepted[room.id]" required />
              <label :for="`terms-${room.id}`">I have read and agree to the Terms and Conditions</label>
            </div>
            <button type="submit" :disabled="!termsAccepted[room.id]">Book Now</button>
          </form>
          <div class="terms-box">
            <h4>Terms and Conditions</h4>
            <p><strong>Once you reserve a room, you will receive a confirmation email with a link and a confirmation code. You must check-in to the room upon arrival. If you do not check in within 15 minutes of arriving for your reservation, your reservation will be cancelled.</strong></p>
            <ol>
              <li>No curtains nor rolling white boards shall obstruct windows nor block the doors of the study room. No papers should be taped to the walls.</li>
              <li>No Library materials, including books, should be left in any Group Study Room.</li>
              <li>The Library is not responsible for loss, theft, or damage of any material left in the room.</li>
              <li>No electrical appliances are allowed in the Group Study Rooms.</li>
              <li>The individual to whom the Group Study Room is assigned is responsible for any infraction of the policies or damage to the room. Any damage must be reported to Circulation as soon as possible.</li>
              <li>The <a href="https://web.uri.edu/library/wp-content/uploads/sites/1549/Patron_Code_of_Conduct_2023.pdf" target="_blank">Library Patron Code of Conduct</a> will apply to all users of the Group Study Rooms.</li>
            </ol>
          </div>
        </div>
      </div>
      </div>

    <Transition name="fade">
      <div v-if="expandedRooms.length > 0" class="backdrop" @click="closeAllExpanded"></div>
    </Transition>

    <!-- Footer -->
    <footer class="uri-footer">
      <div class="footer-container">
        <div class="footer-links">
          <a href="https://web.uri.edu/library/about/hours/" target="_blank">Library Hours</a>
          <a href="https://web.uri.edu/library/contact/" target="_blank">Contact Us</a>
          <a href="https://web.uri.edu/library/help/reference-research-help/" target="_blank">Research Help</a>
          <a href="https://web.uri.edu/library/services/" target="_blank">Services</a>
          <a href="https://web.uri.edu/library/siteindex/" target="_blank">Site Index</a>
        </div>
        <div class="footer-info">
          <p>Robert L. Carothers Library &amp; Learning Commons</p>
          <p>15 Lippitt Road, Kingston, RI 02881</p>
        </div>
        <div class="footer-copyright">
          <p>&copy; {{ new Date().getFullYear() }} <a href="https://uri.edu" target="_blank">University of Rhode Island</a>. All Rights Reserved.</p>
        </div>
      </div>
    </footer>

    <!-- Custom Modal using Teleport to body -->
    <Teleport to="body">
      <div v-if="showModal" class="custom-modal-backdrop" @click="closeModal">
        <div class="custom-modal" @click.stop>
          <div class="modal-header">
            <h3>{{ modalTitle }}</h3>
            <button class="modal-close" @click="closeModal">×</button>
          </div>
          <div class="modal-body" v-html="modalMessage"></div>
          <div class="modal-footer">
            <button class="modal-button" @click="closeModal">OK</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>

</template>

<script setup>
import { useTokenManager } from './composables/useAuth'

// Use flexbox for timeline segments so they fill horizontally and stack correctly
function getBookingStyle(booking) {
  const start = new Date(booking.fromDate || booking.from || booking.start);
  const end = new Date(booking.toDate || booking.to || booking.end);
  const startMinutes = start.getHours() * 60 + start.getMinutes();
  const endMinutes = end.getHours() * 60 + end.getMinutes();
  const { openStart, openEnd } = getOpenRange(currentDate.value);
  const totalMinutes = openEnd - openStart;
  const adjStart = Math.max(startMinutes, openStart);
  const adjEnd = Math.min(endMinutes, openEnd);
  if (adjStart >= adjEnd) return { display: 'none' };
  const left = ((adjStart - openStart) / totalMinutes) * 100;
  const width = ((adjEnd - adjStart) / totalMinutes) * 100;
  const isApiAvailability = booking.isApiAvailability || (booking.fromDate && booking.toDate);
  return {
    position: 'absolute',
    left: `${left}%`,
    width: `${width}%`,
    background: isApiAvailability ? '#9e9e9e' : '#27ae60',
    borderRadius: '4px',
    height: '100%',
  };
}
import { ref, onMounted, computed, watch } from 'vue'
import roomZones from './roomZones.json'
// Given API availability segments, return a full timeline sequence covering open hours, with red for unavailable and green for available
function getTimelineSegments(availability) {
  const { openStart, openEnd } = getOpenRange(currentDate.value)
  // Convert to minutes
  const segments = []
  let cursor = openStart
  // Sort by start time
  const sorted = [...availability].sort((a, b) => {
    const aStart = parseTime(a.from)
    const bStart = parseTime(b.from)
    return aStart - bStart
  })
  for (const seg of sorted) {
    const segStart = parseTime(seg.from)
    const segEnd = parseTime(seg.to)
    // If there's a gap before this segment, add a red segment (unavailable)
    if (segStart > cursor) {
      segments.push({ from: minutesToTime(cursor), to: minutesToTime(segStart), isApiAvailability: true })
    }
    // Add the green segment for available
    segments.push({ from: minutesToTime(segStart), to: minutesToTime(segEnd), isApiAvailability: false })
    cursor = Math.max(cursor, segEnd)
  }
  // If there's time after the last segment, add a red segment (unavailable)
  if (cursor < openEnd) {
    segments.push({ from: minutesToTime(cursor), to: minutesToTime(openEnd), isApiAvailability: true })
  }
  return segments
}

function parseTime(str) {
  if (!str) return 0
  const d = new Date(str)
  return d.getHours() * 60 + d.getMinutes()
}

function minutesToTime(mins) {
  // Returns ISO string for today at mins
  const date = new Date(currentDate.value)
  date.setHours(Math.floor(mins / 60), mins % 60, 0, 0)
  return date.toISOString()
}

// Make getTimelineSegments available to template
defineExpose({ getTimelineSegments })

const libraryName = import.meta.env.VITE_LIBRARY_NAME
const baseUrl = import.meta.env.VITE_LIBCAL_BASE_URL
const token = import.meta.env.VITE_LIBCAL_TOKEN
const locationId = import.meta.env.VITE_LOCATION_ID
const roomItemIds = import.meta.env.VITE_ROOM_ITEM_IDS ? import.meta.env.VITE_ROOM_ITEM_IDS.split(',').map(id => Number(id.trim())) : []

// Simple in-memory cache for fetched item metadata to avoid refetching the same ids
const itemCache = {}

// Helpers to normalize booking and room shapes and to dedupe bookings
const normalizeBooking = (b) => {
  if (!b) return b
  const fromDate = b.fromDate || b.from || b.start || b.startDate || b.start_time || b.startTime
  const toDate = b.toDate || b.to || b.end || b.endDate || b.end_time || b.endTime
  const eid = (b.eid !== undefined && b.eid !== null) ? Number(b.eid) : (b.item_id !== undefined && b.item_id !== null ? Number(b.item_id) : undefined)
  const id = (b.id !== undefined && b.id !== null) ? Number(b.id) : undefined
  const status = (b.status !== undefined && b.status !== null) ? String(b.status) : undefined
  return { ...b, eid, id, status, fromDate, toDate }
}

const dedupeBookings = (arr) => {
  const seen = new Set()
  const out = []
  for (const b of (arr || [])) {
    const key = `${b.eid}|${b.fromDate}|${b.toDate}`
    if (seen.has(key)) continue
    seen.add(key)
    out.push(b)
  }
  return out
}

// Global request throttling wrapper
const REQUEST_DELAY_MS = 150 // spacing between requests
let _lastRequestAt = 0
const sleep = (ms) => new Promise(resolve => setTimeout(resolve, ms))
const { getValidToken } = useTokenManager()
const { refreshToken } = useTokenManager()

const apiFetch = async (input, init = {}) => {
  const now = Date.now()
  const since = now - _lastRequestAt
  if (since < REQUEST_DELAY_MS) {
    await sleep(REQUEST_DELAY_MS - since)
  }
  
  try {
    // Get a valid token (refreshes automatically if needed)
    const token = await getValidToken()
    
    // Add authorization header
    const headers = {
      ...init.headers,
      'Authorization': `Bearer ${token}`
    }
    
    const res = await fetch(input, { ...init, headers })
    _lastRequestAt = Date.now()
    
    if (res.status === 401 || res.status === 403) {
      // Token might be invalid, try refreshing once more
      try {
        console.log('Auth failed, forcing token refresh...')
        const freshToken = await refreshToken()
        const retryHeaders = {
          ...init.headers,
          'Authorization': `Bearer ${freshToken}`
        }
        const retryRes = await fetch(input, { ...init, headers: retryHeaders })
        if (retryRes.ok) {
          console.log('Retry with refreshed token succeeded')
          return retryRes
        }
      } catch (retryError) {
        console.warn('Token refresh retry failed:', retryError)
      }
      
      let txt = ''
      try { txt = await res.text() } catch (e) { /* ignore */ }
      const msg = `Authorization error (${res.status}). ${txt}`
      console.error(msg)
      if (typeof error !== 'undefined') error.value = 'API authorization error - token may need manual refresh'
      throw new Error(msg)
    }
    
    if (res.status >= 400 && res.status < 500) {
      // log client errors with body
      try {
        const txt = await res.text()
        console.warn(`apiFetch client error ${res.status} ${input}: ${txt}`)
      } catch (e) {
        console.warn(`apiFetch client error ${res.status} ${input}`)
      }
    }
    return res
  } catch (e) {
    _lastRequestAt = Date.now()
    throw e
  }
}

const rooms = ref([])
const bookings = ref([])
const loading = ref(true)
const error = ref('')
const expandedRooms = ref([])
const closingRooms = ref([])
const mobileMenuOpen = ref(false)
const showModal = ref(false)
const modalTitle = ref('')
const modalMessage = ref('')
const selectedTimes = ref({})
const duration = ref(60)
const currentDate = ref(new Date())
const selectedZone = ref('All')
const selectedRoomName = ref('All')
const selectedCapacity = ref('All')
const hoverTime = ref(null)
const hoverLeft = ref(0)
const fname = ref('')
const lname = ref('')
const email = ref('')
const termsAccepted = ref({})
const selectedStarts = ref({})
const selectedEnds = ref({})
const timeOptions = computed(() => {
  const { openStart, openEnd } = getOpenRange(currentDate.value)
  const options = []
  for (let min = openStart; min <= openEnd; min += 15) {
    const hours = Math.floor(min / 60)
    const minutes = min % 60
    const formattedTime = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`
    options.push(formattedTime)
  }
  return options
})

const mergeAdjacentSegments = (segments) => {
  if (!segments || segments.length === 0) return []
  
  // Sort by start time
  const sorted = [...segments].sort((a, b) => {
    return parseTime(a.from || a.start) - parseTime(b.from || b.start)
  })
  
  const merged = []
  let current = { ...sorted[0] }
  
  for (let i = 1; i < sorted.length; i++) {
    const currentEnd = parseTime(current.to || current.end)
    const nextStart = parseTime(sorted[i].from || sorted[i].start)
    const nextEnd = parseTime(sorted[i].to || sorted[i].end)
    
    // If segments are adjacent or overlapping (within 1 minute), merge them
    if (nextStart - currentEnd <= 1) {
      // Extend current segment
      current.to = sorted[i].to || sorted[i].end
    } else {
      // Gap found, save current and start new one
      merged.push(current)
      current = { ...sorted[i] }
    }
  }
  merged.push(current)
  
  return merged
}

const getAvailableStartTimes = (room) => {
  if (!room) return []
  
  if (!room.availability || !Array.isArray(room.availability)) {
    return timeOptions.value
  }
  
  // Merge adjacent 5-minute segments into continuous blocks
  const mergedSegments = mergeAdjacentSegments(room.availability)
  
  const availableTimes = []
  const { openStart, openEnd } = getOpenRange(currentDate.value)
  
  // For each 15-minute interval, check if it's within a merged segment
  for (let min = openStart; min < openEnd; min += 15) {
    const isAvailable = mergedSegments.some(seg => {
      const segStart = parseTime(seg.from || seg.start)
      const segEnd = parseTime(seg.to || seg.end)
      // Check if this time is within segment (with at least 30 min remaining)
      return min >= segStart && min + 30 <= segEnd
    })
    
    if (isAvailable) {
      const hours = Math.floor(min / 60)
      const minutes = min % 60
      availableTimes.push(`${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`)
    }
  }
  
  return availableTimes.length > 0 ? availableTimes : []
}

const getAvailableEndTimes = (room, startTime) => {
  if (!room || !startTime) return []
  
  const startMinutes = timeToMinutes(startTime)
  
  if (!room.availability || !Array.isArray(room.availability)) {
    return []
  }
  
  // Merge adjacent segments first
  const mergedSegments = mergeAdjacentSegments(room.availability)
  
  // Find which merged segment contains the start time
  const segment = mergedSegments.find(seg => {
    const segStart = parseTime(seg.from || seg.start)
    const segEnd = parseTime(seg.to || seg.end)
    return startMinutes >= segStart && startMinutes < segEnd
  })
  
  if (!segment) {
    return []
  }
  
  const segEnd = parseTime(segment.to || segment.end)
  const maxEndTime = Math.min(startMinutes + 180, segEnd) // Max 3 hours or end of segment
  
  const availableTimes = []
  for (let min = startMinutes + 30; min <= maxEndTime; min += 15) {
    const hours = Math.floor(min / 60)
    const minutes = min % 60
    availableTimes.push(`${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`)
  }
  
  return availableTimes
}

const timeToMinutes = (timeStr) => {
  if (!timeStr) return 0
  const [h, m] = timeStr.split(':').map(Number)
  return h * 60 + m
}

const updateSelection = (roomId) => {
  const startTime = selectedStarts.value[roomId]
  const endTime = selectedEnds.value[roomId]
  if (startTime && endTime) {
    const startMin = timeToMinutes(startTime)
    const endMin = timeToMinutes(endTime)
    if (endMin > startMin && endMin - startMin >= 30 && endMin - startMin <= 180) {
      const room = rooms.value.find(r => r.id == roomId)
      if (room && isPeriodAvailable(room, startMin, endMin)) {
        selectedTimes.value[roomId] = startMin
        duration.value = endMin - startMin
      } else {
        showCustomModal('Error', 'Selected time is not available')
      }
    } else {
      showCustomModal('Error', 'Invalid time range (30min-3hr)')
    }
  }
}
const dragging = ref(null)

const showCustomModal = (title, message) => {
  modalTitle.value = title
  modalMessage.value = message.replace(/\n/g, '<br>')
  showModal.value = true
}

const closeModal = () => {
  showModal.value = false
}

const fetchRooms = async () => {
  // Rooms will be set from bookings
}

const inferZoneFromName = (name) => {
  if (!name) return 'Uncategorized'
  const n = name.toLowerCase()
  if (n.includes('llsr') || n.includes('lower')) return 'Lower Level'
  const studyMatch = name.match(/study\s*(\d+)/i)
  if (studyMatch) {
    const num = parseInt(studyMatch[1], 10)
    if (!isNaN(num)) {
      if (num >= 300) return 'Third Floor'
      if (num >= 200) return 'Second Floor'
      return 'First Floor'
    }
  }
  // fallback to first-floor style if the name contains '1' or 'Main'
  if (n.match(/\b1\b/) || n.includes('main')) return 'First Floor'
  return 'Uncategorized'
}

const buildRoomsFromBookings = (bookingList) => {
  // Build a base rooms list from bookings (names/zones) and include env-configured item ids
  const bookingIds = Array.isArray(bookingList) ? bookingList.map(b => Number(b.eid)) : []
  const idSet = new Set([ ...(Array.isArray(roomItemIds) ? roomItemIds.map(Number) : []), ...bookingIds ])
  const ids = Array.from(idSet)
  const baseRooms = ids.map(id => {
    const b = (bookingList || []).find(x => Number(x.eid) === Number(id))
    return {
      id: Number(id),
      name: b ? b.item_name : '',  // Empty string instead of "Room {id}"
      zone: b ? b.category_name : 'Uncategorized',
      capacity: null
    }
  })
  rooms.value = baseRooms
  return ids
}

const fetchBookings = async () => {
  try {
    const dateStr = currentDate.value.toISOString().split('T')[0]
    console.log('Fetching bookings for', dateStr)
    const bookingsUrl = `${baseUrl}/space/bookings?lid=${locationId}&date=${dateStr}`
    console.log('API Request:', bookingsUrl)
    
    // apiFetch now handles authorization automatically
    const response = await apiFetch(bookingsUrl)
    console.log('Response status', response.status)
    if (!response.ok) {
      // No bookings; still build rooms from env ids and enrich
      bookings.value = []
      const ids = buildRoomsFromBookings([])
      try {
        await enrichRoomsWithAPIData(ids)
      } catch (e) {
        console.warn('Failed to enrich room metadata for empty bookings:', e)
      }
      return
    }
    const data = await response.json()
    console.log('Data length', data.length)
  const confirmedBookings = data.filter(booking => booking.status === 'Confirmed')
  // Normalize and dedupe bookings immediately
  const normalized = confirmedBookings.map(normalizeBooking)
  bookings.value = dedupeBookings(normalized)

  // Build rooms list from normalized bookings plus env IDs, then enrich with item details
  const ids = buildRoomsFromBookings(bookings.value)
    try {
      await enrichRoomsWithAPIData(ids)
    } catch (e) {
      console.warn('Failed to enrich room metadata:', e)
    }

    // Merge availability segments for ids so timelines reflect availability for env items too
    try {
      const avail = await fetchAvailabilityForIds(ids, dateStr)
      for (const seg of avail) {
        const segEid = Number(seg.eid)
        const normSeg = normalizeBooking({ ...seg, eid: segEid, status: seg.status || 'Confirmed' })
        const exists = bookings.value.some(b => b.eid === normSeg.eid && b.fromDate === normSeg.fromDate && b.toDate === normSeg.toDate)
        if (!exists) bookings.value.push(normSeg)
      }
      // Normalize and dedupe after merging availability
      bookings.value = dedupeBookings(bookings.value.map(normalizeBooking))
      // Re-run enrichment to pick up any metadata for ids added via availability
      try {
        await enrichRoomsWithAPIData(ids)
      } catch (e) {
        console.warn('Failed to re-enrich rooms after availability merge:', e)
      }
    } catch (e) {
      // ignore availability merge errors
    }
  } catch (err) {
    console.error('Fetch error', err)
    error.value = err.message
  }
}

const getBookingsForRoom = (roomId) => {
  const rid = Number(roomId)
  return bookings.value.filter(booking => Number(booking.eid) === rid && String(booking.status).toLowerCase() === 'confirmed')
}

const fetchItemDetailsWithAvailability = async (ids, dateStr) => {
  const map = {}
  const sanitized = (ids || []).map(id => Number(id)).filter(id => Number.isFinite(id) && id > 0)
  const invalid = (ids || []).filter(id => !sanitized.includes(Number(id)))
  if (invalid.length) console.warn('fetchItemDetailsWithAvailability: skipping invalid ids', invalid)
  
  // Check cache first
  for (const id of sanitized) {
    if (itemCache[id] && itemCache[id].availabilityDate === dateStr) {
      map[id] = itemCache[id]
    }
  }
  
  // Get IDs that need to be fetched
  const idsToFetch = sanitized.filter(id => !map[id])
  
  if (idsToFetch.length === 0) return map
  
  const todayStr = new Date().toISOString().split('T')[0]
  
  // Fetch all items in parallel with rate limiting
  // LibCal allows ~25 req/sec, so batch in groups of 20 with small delays
  const batchSize = 20
  const delayBetweenBatches = 1000 // 1 second between batches
  
  for (let i = 0; i < idsToFetch.length; i += batchSize) {
    const batch = idsToFetch.slice(i, i + batchSize)
    
    // Fetch all items in this batch in parallel
    await Promise.all(batch.map(async (id) => {
      try {
        let itemUrl
        if (dateStr === todayStr) {
          itemUrl = `${baseUrl}/space/item/${id}?availability`
        } else {
          itemUrl = `${baseUrl}/space/item/${id}?availability=${dateStr}`
        }
        
        const res = await apiFetch(itemUrl)
        
        if (res.ok) {
          const data = await res.json()
          const obj = Array.isArray(data) ? data[0] : data
          if (obj) {
            obj.availabilityDate = dateStr
            map[id] = obj
            itemCache[id] = obj
          }
        } else {
          console.warn(`fetchItemDetailsWithAvailability: id=${id} error ${res.status}`)
        }
      } catch (e) {
        console.warn(`fetchItemDetailsWithAvailability error id=${id}`, e)
      }
    }))
    
    // If there are more batches, wait before next batch
    if (i + batchSize < idsToFetch.length) {
      await new Promise(resolve => setTimeout(resolve, delayBetweenBatches))
    }
  }
  
  return map
}


const enrichRoomsWithAPIData = async (idsParam = null) => {
  const envIds = Array.isArray(roomItemIds) && roomItemIds.length ? roomItemIds.map(Number) : []
  const bookingIds = bookings.value.map(b => Number(b.eid))
  const existingIds = rooms.value.map(r => Number(r.id))
  let idSet
  if (Array.isArray(idsParam) && idsParam.length) {
    idSet = new Set(idsParam.map(Number))
  } else {
    idSet = new Set([ ...envIds, ...bookingIds, ...existingIds ])
  }
  const ids = Array.from(idSet)

  // Fetch item details + availability for these ids
  const dateStr = currentDate.value.toISOString().split('T')[0]
  const itemMap = await fetchItemDetailsWithAvailability(ids, dateStr)

  // Bulk endpoints removed: all metadata and availability now fetched per-id

  // Build rooms list from itemMap and bookings
  const newRooms = ids.map(id => {
    const numId = Number(id)
    const item = itemMap[numId]
    const booking = bookings.value.find(b => Number(b.eid) === numId)
    let name = (item && (item.name || item.item_name)) || (booking && booking.item_name) || `Room ${numId}`
    if (!name || String(name).trim() === '') name = `Room ${numId}`
    const zoneMapping = { "12348": "Lower Level" }
    let zone = (item && (item.zone || item.zone_name || item.location_name)) || (booking && booking.category_name) || inferZoneFromName(name)
    zone = zoneMapping[zone] || zone
    zone = roomZones[numId] || zone
    const capacity = (item && item.capacity != null) ? Number(item.capacity) : null
    // Extract availability segments from item API response
    let availability = null
    if (item && Array.isArray(item.availability)) {
      // LibCal returns array of segments with from/to or start/end
      availability = item.availability.map(seg => ({
        from: seg.from || seg.start || seg.fromDate || seg.startDate,
        to: seg.to || seg.end || seg.toDate || seg.endDate,
        id: seg.id || seg.bookId || undefined
      })).filter(seg => seg.from && seg.to)
    }
    return { id: numId, name, zone, capacity, availability }
  })
  // Ensure ids and capacities are normalized to numbers
  rooms.value = newRooms.map(r => ({ ...r, id: Number(r.id), capacity: r.capacity != null ? Number(r.capacity) : null }))
  console.log('Room metadata updated from API/items endpoint; items fetched:', Object.keys(itemMap).length)
}

const getOpenRange = (date) => {
  // Default weekday hours: 8:00 - 23:00
  // Weekend hours default to 10:00 - 18:00 (adjust if your library differs)
  const day = date.getDay() // 0 = Sunday, 6 = Saturday
  if (day === 0 || day === 6) {
    return { openStart: 10 * 60, openEnd: 18 * 60 }
  }
  return { openStart: 8 * 60, openEnd: 23 * 60 }
}

const getTotalBookedMinutes = (roomId) => {
  const roomBookings = getBookingsForRoom(roomId)
  const { openStart, openEnd } = getOpenRange(currentDate.value)
  return roomBookings.reduce((total, booking) => {
    const start = new Date(booking.fromDate)
    const end = new Date(booking.toDate)
    const startMinutes = start.getHours() * 60 + start.getMinutes()
    const endMinutes = end.getHours() * 60 + end.getMinutes()
    const adjStart = Math.max(startMinutes, openStart)
    const adjEnd = Math.min(endMinutes, openEnd)
    if (adjStart >= adjEnd) return total
    return total + (adjEnd - adjStart)
  }, 0)
}

const getAvailabilityPercent = (roomId) => {
  const { openStart, openEnd } = getOpenRange(currentDate.value)
  const totalOpen = Math.max(0, openEnd - openStart)
  if (totalOpen === 0) return 0
  
  const room = rooms.value.find(r => r.id === roomId)
  
  // If room has availability array (green segments), calculate from that
  if (room && room.availability && Array.isArray(room.availability)) {
    const totalAvailable = room.availability.reduce((total, seg) => {
      const start = new Date(seg.from || seg.start)
      const end = new Date(seg.to || seg.end)
      const startMinutes = start.getHours() * 60 + start.getMinutes()
      const endMinutes = end.getHours() * 60 + end.getMinutes()
      const adjStart = Math.max(startMinutes, openStart)
      const adjEnd = Math.min(endMinutes, openEnd)
      if (adjStart >= adjEnd) return total
      return total + (adjEnd - adjStart)
    }, 0)
    return (totalAvailable / totalOpen) * 100
  }
  
  // Otherwise calculate from bookings (red segments)
  const booked = getTotalBookedMinutes(roomId)
  const avail = Math.max(0, totalOpen - booked)
  return (avail / totalOpen) * 100
}

const sortedRooms = computed(() => {
  return rooms.value.slice().sort((a, b) => {
    const availA = getAvailabilityPercent(a.id)
    const availB = getAvailabilityPercent(b.id)
    const diff = availB - availA
    if (Math.abs(diff) > 0.0001) return diff
  // Use natural numeric-aware comparison for names (e.g., LLSR 2 before LLSR 10)
  const nameCmp = (a.name || '').localeCompare(b.name || '', undefined, { numeric: true, sensitivity: 'base' })
  if (nameCmp !== 0) return nameCmp
  return a.id - b.id
  })
})

const formatTime = (isoString) => {
  const date = new Date(isoString)
  return date.toLocaleTimeString([], { hour: 'numeric', minute: '2-digit', hour12: true })
}

const formatTimeLabel = (position) => {
  const { openStart, openEnd } = getOpenRange(currentDate.value)
  if (position === 'start') return formatTime(minutesToTime(openStart))
  if (position === 'middle') return formatTime(minutesToTime((openStart + openEnd) / 2))
  if (position === 'end') return formatTime(minutesToTime(openEnd))
  return ''
}

const updateHoverTime = (event) => {
  const rect = event.currentTarget.getBoundingClientRect()
  const x = event.clientX - rect.left
  const percent = Math.max(0, Math.min(1, x / rect.width))
  const { openStart, openEnd } = getOpenRange(currentDate.value)
  const minutes = openStart + percent * (openEnd - openStart)
  const minutesRounded = Math.round(minutes / 5) * 5
  hoverTime.value = formatTime(minutesToTime(minutesRounded))
  hoverLeft.value = x
}

const clearHoverTime = () => {
  hoverTime.value = null
}

const getSelectedBookingStyle = (id) => {
  const { openStart, openEnd } = getOpenRange(currentDate.value)
  const start = selectedTimes.value[id]
  const end = start + duration.value
  const percentStart = ((start - openStart) / (openEnd - openStart)) * 100
  const percentEnd = ((end - openStart) / (openEnd - openStart)) * 100
  return {
    left: `${percentStart}%`,
    width: `${percentEnd - percentStart}%`
  }
}

const startDrag = (id, event) => {
  const barRect = event.currentTarget.getBoundingClientRect()
  const clickX = event.clientX - barRect.left
  const barWidth = barRect.width
  let type = 'move'
  // Increase edge zones to 30% for easier resize grabbing
  if (clickX < barWidth * 0.3) {
    type = 'resize-start'
  } else if (clickX > barWidth * 0.7) {
    type = 'resize-end'
  }
  dragging.value = { 
    id, 
    startX: event.clientX, 
    initialStart: selectedTimes.value[id], 
    initialDuration: duration.value, 
    type,
    timelineRect: event.currentTarget.closest('.timeline').getBoundingClientRect()
  }
  event.preventDefault()
  event.stopPropagation()
}

const onTimelineClick = (room, event) => {
  // Get click position relative to timeline
  const rect = event.currentTarget.getBoundingClientRect()
  const clickX = event.clientX - rect.left
  const percent = clickX / rect.width
  
  const { openStart, openEnd } = getOpenRange(currentDate.value)
  const totalMinutes = openEnd - openStart
  const clickedMinutes = Math.floor(openStart + (percent * totalMinutes))
  
  // Check if this time is in an available (green) segment
  const segments = room.availability && Array.isArray(room.availability)
    ? getTimelineSegments(room.availability)
    : getBookingsForRoom(room.id)
  
  const clickedSegment = segments.find(seg => {
    const segStart = parseTime(seg.from || seg.fromDate)
    const segEnd = parseTime(seg.to || seg.toDate)
    return clickedMinutes >= segStart && clickedMinutes < segEnd
  })
  
  // Only proceed if clicking on an available (green) segment
  if (clickedSegment && !clickedSegment.isApiAvailability) {
    // Round to nearest 15 minutes
    const roundedStart = Math.floor(clickedMinutes / 15) * 15
    
    // Set the start time and show the booking form
    selectedTimes.value[room.id] = roundedStart
    selectedStarts.value[room.id] = `${Math.floor(roundedStart / 60).toString().padStart(2, '0')}:${(roundedStart % 60).toString().padStart(2, '0')}`
    
    // Set default end time (30 minutes later)
    const endMinutes = roundedStart + 30
    selectedEnds.value[room.id] = `${Math.floor(endMinutes / 60).toString().padStart(2, '0')}:${(endMinutes % 60).toString().padStart(2, '0')}`
    duration.value = 30
  }
  // If clicking on red segment, do nothing (which is what we want)
}

const onMouseMove = (event) => {
  if (dragging.value) {
    const deltaX = event.clientX - dragging.value.startX
    const rect = dragging.value.timelineRect
    const percent = deltaX / rect.width
    const { openStart, openEnd } = getOpenRange(currentDate.value)
    const deltaMinutes = percent * (openEnd - openStart)
    const room = rooms.value.find(r => r.id == dragging.value.id)
    
    if (dragging.value.type === 'move') {
      let newStart = dragging.value.initialStart + Math.round(deltaMinutes / 5) * 5
      newStart = Math.max(openStart, Math.min(openEnd - duration.value, newStart))
      const newEnd = newStart + duration.value
      
      // Check if the ENTIRE period is available (no red segments)
      if (room && isPeriodFullyAvailable(room, newStart, newEnd)) {
        selectedTimes.value[dragging.value.id] = newStart
        // Update the dropdown
        const hours = Math.floor(newStart / 60)
        const mins = newStart % 60
        selectedStarts.value[dragging.value.id] = `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}`
      }
    } else if (dragging.value.type === 'resize-end') {
      let newDuration = dragging.value.initialDuration + Math.round(deltaMinutes / 5) * 5
      newDuration = Math.max(30, Math.min(180, newDuration))
      if (room) {
        const maxAvail = getMaxDuration(room)
        newDuration = Math.min(newDuration, maxAvail)
        const start = selectedTimes.value[dragging.value.id]
        const newEnd = start + newDuration
        
        if (isPeriodFullyAvailable(room, start, newEnd)) {
          duration.value = newDuration
          // Update the end time dropdown
          const endMinutes = start + newDuration
          const hours = Math.floor(endMinutes / 60)
          const mins = endMinutes % 60
          selectedEnds.value[dragging.value.id] = `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}`
        }
      }
    } else if (dragging.value.type === 'resize-start') {
      let deltaMin = Math.round(deltaMinutes / 5) * 5
      let newStart = dragging.value.initialStart + deltaMin
      newStart = Math.max(openStart, newStart)
      let newDuration = dragging.value.initialDuration - deltaMin
      newDuration = Math.max(30, Math.min(180, newDuration))
      if (room) {
        const maxAvail = getMaxDuration(room)
        newDuration = Math.min(newDuration, maxAvail)
        const newEnd = newStart + newDuration
        
        if (isPeriodFullyAvailable(room, newStart, newEnd)) {
          selectedTimes.value[dragging.value.id] = newStart
          duration.value = newDuration
          // Update both dropdowns
          const hours = Math.floor(newStart / 60)
          const mins = newStart % 60
          selectedStarts.value[dragging.value.id] = `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}`
          
          const endMinutes = newStart + newDuration
          const endHours = Math.floor(endMinutes / 60)
          const endMins = endMinutes % 60
          selectedEnds.value[dragging.value.id] = `${endHours.toString().padStart(2, '0')}:${endMins.toString().padStart(2, '0')}`
        }
      }
    }
  }
}

const onMouseUp = () => {
  dragging.value = null
}

const getMaxDuration = (room) => {
  const start = selectedTimes.value[room.id]
  if (!room.availability || !Array.isArray(room.availability)) return 180
  let maxEnd = start + 180 // max 3 hours
  for (const seg of room.availability) {
    const segStart = new Date(seg.from || seg.start).getHours() * 60 + new Date(seg.from || seg.start).getMinutes()
    const segEnd = new Date(seg.to || seg.end).getHours() * 60 + new Date(seg.to || seg.end).getMinutes()
    if (segStart <= start && segEnd > start) {
      maxEnd = Math.min(maxEnd, segEnd)
    }
  }
  return Math.max(30, maxEnd - start)
}

const isPeriodAvailable = (room, start, end) => {
  const booked = room.availability && Array.isArray(room.availability) ? room.availability : getBookingsForRoom(room.id)
  console.log('Checking availability for room', room.id, 'start', start, 'end', end, 'booked length', booked.length)
  if (booked.length === 0) {
    console.log('No booked data, assume available')
    return true
  }
  for (const b of booked) {
    const bStartDate = new Date(b.fromDate || b.from || b.start || b.startDate || b.start_time || b.startTime)
    const bEndDate = new Date(b.toDate || b.to || b.end || b.endDate || b.end_time || b.endTime)
    const bStart = bStartDate.getHours() * 60 + bStartDate.getMinutes()
    const bEnd = bEndDate.getHours() * 60 + bEndDate.getMinutes()
    console.log('Available slot:', bStart, bEnd)
    if (bStart < end && bEnd > start) {
      console.log('Overlaps with available, available')
      return true
    }
  }
  console.log('No overlap with available, not available')
  return false
}

const isPeriodFullyAvailable = (room, start, end) => {
  // Check if the entire period from start to end is within available (green) segments
  if (!room.availability || !Array.isArray(room.availability)) {
    // No availability data, check against bookings
    const bookings = getBookingsForRoom(room.id)
    // Make sure period doesn't overlap with any booking
    return !bookings.some(b => {
      const bStart = new Date(b.fromDate).getHours() * 60 + new Date(b.fromDate).getMinutes()
      const bEnd = new Date(b.toDate).getHours() * 60 + new Date(b.toDate).getMinutes()
      return bStart < end && bEnd > start
    })
  }
  
  // Find which available segment(s) the period falls into
  for (const seg of room.availability) {
    const segStart = new Date(seg.from || seg.start).getHours() * 60 + new Date(seg.from || seg.start).getMinutes()
    const segEnd = new Date(seg.to || seg.end).getHours() * 60 + new Date(seg.to || seg.end).getMinutes()
    
    // Check if the entire period is within this available segment
    if (start >= segStart && end <= segEnd) {
      return true
    }
  }
  
  return false
}

const selectTime = (id, event) => {
  console.log('Timeline clicked for room', id)
  const rect = event.currentTarget.getBoundingClientRect()
  const x = event.clientX - rect.left
  const percent = Math.max(0, Math.min(1, x / rect.width))
  const { openStart, openEnd } = getOpenRange(currentDate.value)
  const minutes = openStart + percent * (openEnd - openStart)
  const minutesRounded = Math.round(minutes / 5) * 5 // round to 5 min
  const room = rooms.value.find(r => r.id == id)
  if (room && isPeriodAvailable(room, minutesRounded, minutesRounded + duration.value)) {
    selectedTimes.value[id] = minutesRounded
    duration.value = 60 // default 1 hour
    if (!expandedRooms.value.includes(id)) {
      toggleExpanded(id)
    }
    console.log('Selected time for room', id, ':', selectedTimes.value[id])
  } else {
    console.log('Cannot select time for room', id, ': not available')
  }
}

const isRoomAvailableNow = (room) => {
  const now = new Date()
  
  // Only show "Available Now" if we're viewing today
  const viewingToday = currentDate.value.toDateString() === now.toDateString()
  if (!viewingToday) return false
  
  const currentTime = now.getHours() * 60 + now.getMinutes()
  const { openStart, openEnd } = getOpenRange(currentDate.value)
  
  // Check if current time is within open hours
  if (currentTime < openStart || currentTime > openEnd) return false
  
  // Check if there's an available segment covering now (use merged segments)
  if (room.availability && Array.isArray(room.availability) && room.availability.length > 0) {
    const mergedSegments = mergeAdjacentSegments(room.availability)
    const isAvailable = mergedSegments.some(seg => {
      const startTime = parseTime(seg.from || seg.start)
      const endTime = parseTime(seg.to || seg.end)
      const inRange = currentTime >= startTime && currentTime < endTime
      return inRange
    })
    return isAvailable
  }
  
  // Fallback: if no availability data, assume available if within open hours
  return true
}

const bookRoom = async (room) => {
  const startTimeStr = selectedStarts.value[room.id]
  const endTimeStr = selectedEnds.value[room.id]
  
  if (!startTimeStr || !endTimeStr) {
    showCustomModal('Error', 'Please select start and end times')
    return
  }
  
  console.log('Booking room:', room.id, 'from', startTimeStr, 'to', endTimeStr)
  
  // Parse time strings (HH:mm format)
  const [startHour, startMin] = startTimeStr.split(':').map(Number)
  const [endHour, endMin] = endTimeStr.split(':').map(Number)
  
  // Create start and end dates in local timezone
  const startDate = new Date(currentDate.value)
  startDate.setHours(startHour, startMin, 0, 0)
  
  const endDate = new Date(currentDate.value)
  endDate.setHours(endHour, endMin, 0, 0)
  
  // Format as ISO string but preserve local time (LibCal expects local timezone)
  const formatLocalISO = (date) => {
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')
    const hours = String(date.getHours()).padStart(2, '0')
    const minutes = String(date.getMinutes()).padStart(2, '0')
    const seconds = String(date.getSeconds()).padStart(2, '0')
    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`
  }
  
  const payload = {
    start: formatLocalISO(startDate),
    fname: fname.value,
    lname: lname.value,
    email: email.value,
    adminbooking: 1,
    bookings: [
      {
        id: room.id,
        to: formatLocalISO(endDate)
      }
    ]
  }

  console.log('Sending payload:', JSON.stringify(payload, null, 2))

  try {
    // Get a valid OAuth token
    const token = await getValidToken()
    console.log('Using token:', token)
    console.log('Token starts with:', token.substring(0, 20))
    
    const response = await fetch('https://uri.libcal.com/api/1.1/space/reserve', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(payload)
    })

    console.log('Response status:', response.status)
    const responseText = await response.text()
    console.log('Response:', responseText)

    if (response.ok) {
      console.log('Booking successful, showing modal...')
      // Clean up form and room state first
      fname.value = ''
      lname.value = ''
      email.value = ''
      termsAccepted.value[room.id] = false
      delete selectedTimes.value[room.id]
      delete selectedStarts.value[room.id]
      delete selectedEnds.value[room.id]
      duration.value = 60
      
      // Close the expanded room
      const index = expandedRooms.value.indexOf(room.id)
      if (index > -1) {
        expandedRooms.value.splice(index, 1)
      }
      
      // Refresh bookings data
      fetchBookings()
      
      // Show modal AFTER everything else
      setTimeout(() => {
        showCustomModal('Thank you!', '✅ Booking Confirmed!\n\nPlease check your email for:\n• Confirmation details\n• Check-in instructions\n• Your confirmation code\n\nRemember: You must check in within 15 minutes of your reservation start time, or your booking will be cancelled.')
      }, 100)
      
      console.log('After showCustomModal call')
    } else {
      showCustomModal('Error', 'Booking failed: ' + responseText)
    }
  } catch (error) {
    console.error('Booking error:', error)
    showCustomModal('Error', 'Booking error: ' + error.message)
  }
}

const formatHalfHour = (i) => {
  const hour = 8 + Math.floor(i / 2)
  const min = (i % 2) * 30
  const displayHour = hour % 12 || 12
  const ampm = hour >= 12 ? 'PM' : 'AM'
  return `${displayHour}:${min.toString().padStart(2, '0')} ${ampm}`
}

const isToday = computed(() => currentDate.value.toDateString() === new Date().toDateString())

const formatDate = (date) => date.toLocaleDateString()

const goToTomorrow = () => {
  currentDate.value = new Date(currentDate.value.getTime() + 24 * 60 * 60 * 1000)
  loading.value = true
  fetchBookings().finally(() => {
    loading.value = false
  })
}

const goToToday = () => {
  currentDate.value = new Date()
  loading.value = true
  fetchBookings().finally(() => {
    loading.value = false
  })
}

// Filter state and derived lists
const zones = computed(() => {
  return ['All', 'Lower Level', 'First Floor', 'Second Floor', 'Third Floor']
})

const roomNames = computed(() => {
  let list = rooms.value
  if (selectedZone.value !== 'All') list = list.filter(r => r.zone === selectedZone.value)
  const set = new Set(list.map(r => r.name))
  return ['All', ...Array.from(set).sort((a, b) => a.localeCompare(b, undefined, { numeric: true, sensitivity: 'base' }))]
})

// Reset room filter if it doesn't exist for the selected zone
watch(selectedZone, () => {
  if (selectedRoomName.value !== 'All' && !roomNames.value.includes(selectedRoomName.value)) {
    selectedRoomName.value = 'All'
  }
})

const filteredSortedRooms = computed(() => {
  const filtered = rooms.value.filter(r => {
    if (selectedZone.value !== 'All' && r.zone !== selectedZone.value) return false
    if (selectedRoomName.value !== 'All' && r.name !== selectedRoomName.value) return false
    if (selectedCapacity.value !== 'All') {
      const minCap = Number(selectedCapacity.value)
      if (!r.capacity || Number(r.capacity) < minCap) return false
    }
    return true
  })
  
  const sorted = filtered.slice().sort((a, b) => {
    // 1. Available now comes first
    const aAvailNow = isRoomAvailableNow(a)
    const bAvailNow = isRoomAvailableNow(b)
    if (aAvailNow && !bAvailNow) return -1
    if (!aAvailNow && bAvailNow) return 1
    
    // 2. If both (or neither) are available now, sort by availability percentage (highest first)
    const availA = getAvailabilityPercent(a.id)
    const availB = getAvailabilityPercent(b.id)
    const availDiff = availB - availA
    if (Math.abs(availDiff) > 0.0001) return availDiff
    
    // 3. If same availability, sort alphabetically
    const nameCmp = (a.name || '').localeCompare(b.name || '', undefined, { numeric: true, sensitivity: 'base' })
    if (nameCmp !== 0) return nameCmp
    
    // 4. Finally by ID as tie-breaker
    return a.id - b.id
  })
  
  return sorted
})

const EXPECTED_CAPACITIES = [1, 2, 4]
const capacities = computed(() => {
  // Only expose the expected capacities (1,2,4) in the dropdown so sample or incorrect
  // values (e.g. 6,8) from the mapping file won't appear.
  const nums = rooms.value.map(r => Number(r.capacity)).filter(c => Number.isFinite(c))
  const uniq = Array.from(new Set(nums))
  const filtered = uniq.filter(n => EXPECTED_CAPACITIES.includes(n)).sort((a, b) => a - b)
  // If there are no mapped capacities, fall back to the expected list so the dropdown still shows options
  return filtered.length ? filtered : EXPECTED_CAPACITIES
})



const toggleExpanded = (id) => {
  const index = expandedRooms.value.indexOf(id)
  if (index > -1) {
    // Add to closing state for animation
    closingRooms.value.push(id)
    // After animation completes, remove from both arrays
    setTimeout(() => {
      expandedRooms.value.splice(expandedRooms.value.indexOf(id), 1)
      closingRooms.value.splice(closingRooms.value.indexOf(id), 1)
      delete selectedTimes.value[id]
      delete selectedStarts.value[id]
      delete selectedEnds.value[id]
    }, 250)
  } else {
    expandedRooms.value.push(id)
  }
}

const closeAllExpanded = () => {
  // Close all expanded rooms with animation
  expandedRooms.value.forEach(id => {
    if (!closingRooms.value.includes(id)) {
      toggleExpanded(id)
    }
  })
}

onMounted(async () => {
  try {
    await fetchBookings()
  } finally {
    loading.value = false
  }
  window.addEventListener('mousemove', onMouseMove)
  window.addEventListener('mouseup', onMouseUp)
})
</script>

<style>
/* Primo NDE Header - Exact Replication */
.primo-header {
  background-color: #ffffff;
  border-bottom: 1px solid #e0e0e0;
  padding: 0;
  margin: 0;
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 1000;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.primo-header-container {
  max-width: 1400px;
  margin: 0 auto;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 20px;
}

.primo-logo {
  display: flex;
  align-items: center;
}

.primo-logo img {
  height: 40px;
  width: auto;
}

.primo-nav {
  display: flex;
  list-style: none;
  margin: 0;
  padding: 0;
  gap: 24px;
  align-items: center;
}

.primo-nav li {
  margin: 0;
  padding: 0;
}

.primo-nav a {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  font-size: 14px;
  font-weight: 600;
  color: #2c3e50;
  text-decoration: none;
  transition: color 0.2s ease;
  white-space: nowrap;
}

.primo-nav a:hover {
  color: #0056b3;
  text-decoration: underline;
}

.mobile-menu-toggle {
  display: none;
  flex-direction: column;
  gap: 4px;
  background: none;
  border: none;
  cursor: pointer;
  padding: 8px;
  z-index: 1002;
}

.hamburger-line {
  width: 24px;
  height: 3px;
  background-color: #2c3e50;
  border-radius: 2px;
  transition: all 0.3s ease;
}

/* Mobile Styles */
@media (max-width: 768px) {
  .primo-header-container {
    padding: 8px 15px;
  }

  .mobile-menu-toggle {
    display: flex;
  }

  .primo-nav {
    position: fixed;
    top: 58px;
    right: -100%;
    width: 250px;
    height: calc(100vh - 58px);
    background-color: #ffffff;
    flex-direction: column;
    padding: 20px;
    gap: 0;
    box-shadow: -2px 0 8px rgba(0,0,0,0.1);
    transition: right 0.3s ease;
    overflow-y: auto;
  }

  .primo-nav.mobile-open {
    right: 0;
  }

  .primo-nav li {
    width: 100%;
    padding: 12px 0;
    border-bottom: 1px solid #e0e0e0;
  }

  .primo-nav li:last-child {
    border-bottom: none;
  }

  .primo-nav a {
    display: block;
    width: 100%;
    font-size: 16px;
  }

  #app {
    margin-top: 70px;
  }
}

#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
  margin-top: 120px; /* Increased to account for fixed header */
}

.header-image {
  width: 100%;
  max-height: 300px;
  object-fit: cover;
  border-radius: 8px;
  margin-bottom: 20px;
  box-shadow: 0 4px 8px rgba(0,0,0,0.1);
}

h1 {
  color: #002D5B;
  font-size: 2.5em;
  margin-bottom: 10px;
}

.header {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 20px;
  margin-bottom: 20px;
}

.header button {
  padding: 10px 20px;
  background-color: #002D5B;
  color: white;
  border: none;
  border-radius: 5px;
  cursor: pointer;
}

.header button:hover {
  background-color: #001F3F;
}

.availability-pill {
  position: absolute;
  top: 10px;
  right: 10px;
  background-color: #27ae60;
  color: white;
  padding: 4px 8px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: bold;
  z-index: 1;
  display: flex;
  align-items: center;
  gap: 4px;
}

.green-dot {
  width: 8px;
  height: 8px;
  background-color: #2ecc71;
  border-radius: 50%;
  display: inline-block;
  animation: pulse 2s infinite;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

.modal-facade {
  width: 100%;
  max-height: 200px;
  object-fit: cover;
  border-radius: 8px;
  margin: 20px 0;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.booking-instruction {
  text-align: center;
  color: #002D5B;
  font-weight: bold;
  margin: 20px 0;
}

.booking-container {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 20px;
  margin-top: 20px;
}

.booking-form {
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 20px;
  background: #f8f9fa;
  border-radius: 8px;
}

.booking-form h4 {
  margin: 0 0 10px 0;
  color: #002D5B;
}

.booking-form input[type="text"],
.booking-form input[type="email"] {
  padding: 8px;
  border: 1px solid #ccc;
  border-radius: 4px;
}

.booking-form button {
  padding: 10px;
  background-color: #002D5B;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.booking-form button:disabled {
  background-color: #ccc;
  cursor: not-allowed;
}

.booking-form button:hover:not(:disabled) {
  background-color: #001F3F;
}

.terms-checkbox {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 10px;
}

.terms-checkbox input[type="checkbox"] {
  width: 18px;
  height: 18px;
  cursor: pointer;
}

.terms-checkbox label {
  cursor: pointer;
  font-size: 14px;
  color: #333;
}

.terms-box {
  padding: 20px;
  background: #fff;
  border: 2px solid #002D5B;
  border-radius: 8px;
  max-height: 500px;
  overflow-y: auto;
}

.terms-box h4 {
  margin: 0 0 15px 0;
  color: #002D5B;
  font-size: 18px;
}

.terms-box p {
  margin: 0 0 15px 0;
  line-height: 1.6;
  font-size: 14px;
}

.terms-box ol {
  margin: 0;
  padding-left: 20px;
  line-height: 1.8;
  font-size: 14px;
}

.terms-box li {
  margin-bottom: 10px;
}

.terms-box a {
  color: #002D5B;
  text-decoration: underline;
}

.terms-box a:hover {
  color: #001F3F;
}

.filters {
  display: flex;
  justify-content: center;
  gap: 16px;
  margin-bottom: 12px;
  align-items: center;
}

.filters label {
  font-weight: 600;
}

.filters select {
  margin-left: 8px;
  padding: 6px 8px;
  border-radius: 4px;
  border: 1px solid #ccc;
}

.no-rooms {
  font-style: italic;
  color: #666;
  margin: 8px;
}

.rooms {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
}

.room-card {
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 20px 16px 16px 16px;
  margin: 16px;
  width: 300px;
  box-shadow: 0 2px 4px rgba(0,0,0,0.1);
  transition: transform 0.3s ease, box-shadow 0.3s ease, opacity 0.3s ease;
  cursor: pointer;
  position: relative;
}

.room-card:hover {
  transform: scale(1.02);
}

.room-card.expanded {
  position: fixed;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 80%;
  height: 80%;
  z-index: 1001;
  margin: 0;
  background: #e3f2fd;
  box-shadow: 0 20px 60px rgba(0,0,0,0.3);
  border: 3px solid #dee2e6;
  border-radius: 16px;
  padding: 32px;
  overflow: auto;
  animation: expandCard 0.3s ease forwards;
}

@keyframes expandCard {
  from {
    opacity: 0.8;
    transform: translate(-50%, -50%) scale(0.9);
  }
  to {
    opacity: 1;
    transform: translate(-50%, -50%) scale(1);
  }
}

.room-card.expanded.closing {
  animation: collapseCard 0.25s ease forwards;
}

@keyframes collapseCard {
  from {
    opacity: 1;
    transform: translate(-50%, -50%) scale(1);
  }
  to {
    opacity: 0;
    transform: translate(-50%, -50%) scale(0.9);
  }
}

.backdrop {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0,0,0,0.5);
  z-index: 1000;
  animation: fadeIn 0.2s ease forwards;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

/* Vue transition classes for backdrop */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.25s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.timeline {
  position: relative;
  height: 24px;
  background-color: #27ae60;
  border: 1px solid #ddd;
  border-radius: 12px;
  margin-top: 8px;
  cursor: pointer;
}

.hover-tooltip {
  position: absolute;
  top: -30px;
  transform: translateX(-50%);
  background: rgba(0, 0, 0, 0.8);
  color: white;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  white-space: nowrap;
  pointer-events: none;
  z-index: 10;
}

.selected-booking {
  position: absolute;
  top: 0;
  height: 100%;
  background: rgba(100, 100, 100, 0.7);
  border: 2px solid #555;
  border-radius: 4px;
  pointer-events: none;
  z-index: 5;
  display: flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 10px;
  font-weight: bold;
}

.time-selectors {
  margin-top: 30px;
  display: flex;
  gap: 10px;
  justify-content: center;
}

.time-selectors label {
  display: flex;
  flex-direction: column;
  font-size: 14px;
}

.time-selectors select {
  margin-top: 5px;
  padding: 5px;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.room-card.expanded .hover-tooltip {
  top: -35px;
}

.room-card.expanded .timeline {
  height: 50px;
  width: 80%;
  border-radius: 20px;
  margin-left: auto;
  margin-right: auto;
}

.booking-segment {
  position: absolute;
  top: 0;
  height: 100%;
  border-radius: 12px;
  box-shadow: inset 0 1px 2px rgba(0,0,0,0.1);
  pointer-events: none;
}

.room-card.expanded .booking-segment {
  border-radius: 20px;
}

.time-label {
  position: absolute;
  top: 26px;
  font-size: 10px;
  color: #666;
  opacity: 1;
  transition: opacity 0.2s;
  pointer-events: none;
  white-space: nowrap;
}

.room-card:hover .time-label,
.room-card.expanded .time-label {
  opacity: 1;
  font-weight: bold;
}

.room-card.expanded h3 {
  font-size: 32px;
  font-weight: bold;
}

.room-card.expanded .time-label {
  top: 42px;
  font-size: 10px;
}

.room-meta {
  font-size: 13px;
  color: #555;
  margin-top: 6px;
}

/* Skeleton loading styles */
.room-card.skeleton {
  pointer-events: none;
  animation: none;
}

.skeleton-title {
  height: 24px;
  background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: 4px;
  margin-bottom: 12px;
  width: 70%;
}

.skeleton-meta {
  height: 16px;
  background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: 4px;
  margin-bottom: 12px;
  width: 50%;
}

.skeleton-timeline {
  height: 24px;
  background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
  border-radius: 4px;
  width: 100%;
}

@keyframes shimmer {
  0% {
    background-position: -200% 0;
  }
  100% {
    background-position: 200% 0;
  }
}

/* Custom Modal Styles */
.custom-modal-backdrop {
  position: fixed !important;
  top: 0 !important;
  left: 0 !important;
  width: 100% !important;
  height: 100% !important;
  background: rgba(0, 0, 0, 0.6) !important;
  z-index: 2000 !important;
  display: flex !important;
  align-items: center !important;
  justify-content: center !important;
  animation: fadeIn 0.2s ease;
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

.custom-modal {
  background: white !important;
  border-radius: 8px;
  width: 90%;
  max-width: 500px;
  box-shadow: 0 4px 8px rgba(0,0,0,0.1);
  animation: slideIn 0.3s ease;
  position: relative !important;
}

.modal-header {
  background: #002D5B;
  color: white;
  padding: 20px;
  border-radius: 8px 8px 0 0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.modal-header h3 {
  margin: 0;
  font-size: 1.5em;
  font-weight: 600;
  color: white;
}

.modal-close {
  background: none;
  border: none;
  color: white;
  font-size: 28px;
  cursor: pointer;
  padding: 0;
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  line-height: 1;
  transition: transform 0.2s;
}

.modal-close:hover {
  transform: scale(1.2);
}

.modal-body {
  padding: 25px 20px;
  font-size: 14px;
  line-height: 1.6;
  color: #2c3e50;
  text-align: left;
}

.modal-footer {
  padding: 15px 20px;
  border-top: 1px solid #e0e0e0;
  display: flex;
  justify-content: flex-end;
}

.modal-button {
  background-color: #002D5B;
  color: white;
  border: none;
  border-radius: 5px;
  padding: 10px 20px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s;
  font-family: Avenir, Helvetica, Arial, sans-serif;
}

.modal-button:hover {
  background-color: #001F3F;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

@keyframes slideIn {
  from {
    transform: translateY(-50px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

/* URI Footer */
.uri-footer {
  background-color: #002D5B;
  color: #ffffff;
  padding: 40px 20px 30px;
  margin-top: 60px;
}

.footer-container {
  max-width: 1200px;
  margin: 0 auto;
  text-align: center;
}

.footer-links {
  display: flex;
  flex-wrap: wrap;
  justify-content: center;
  gap: 20px 40px;
  margin-bottom: 30px;
}

.footer-links a {
  color: #ffffff;
  text-decoration: none;
  font-size: 14px;
  font-weight: 500;
  transition: color 0.2s;
}

.footer-links a:hover {
  color: #7BAFD4;
  text-decoration: underline;
}

.footer-info {
  margin-bottom: 20px;
}

.footer-info p {
  margin: 5px 0;
  font-size: 14px;
  color: #d0d0d0;
}

.footer-copyright {
  border-top: 1px solid rgba(255, 255, 255, 0.2);
  padding-top: 20px;
}

.footer-copyright p {
  margin: 0;
  font-size: 13px;
  color: #b0b0b0;
}

.footer-copyright a {
  color: #7BAFD4;
  text-decoration: none;
}

.footer-copyright a:hover {
  text-decoration: underline;
}

@media (max-width: 600px) {
  .footer-links {
    flex-direction: column;
    gap: 15px;
  }
  
  .uri-footer {
    padding: 30px 15px 20px;
  }
}
</style>