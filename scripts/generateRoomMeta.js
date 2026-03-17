#!/usr/bin/env node
// Script to fetch room capacities from LibCal API and persist to src/roomMeta.json
// Usage: node scripts/generateRoomMeta.js

const fs = require('fs')
const path = require('path')

const envPath = path.resolve(process.cwd(), '.env')
let env = {}
if (fs.existsSync(envPath)) {
  const content = fs.readFileSync(envPath, 'utf8')
  content.split(/\r?\n/).forEach(line => {
    const m = line.match(/^\s*([A-Za-z0-9_]+)\s*=\s*(.*)\s*$/)
    if (m) {
      let [, key, value] = m
      value = value.replace(/^"|"$/g, '')
      env[key] = value
    }
  })
}

const token = env.VITE_LIBCAL_TOKEN
let base = env.VITE_LIBCAL_BASE_URL
if (!base || !base.startsWith('http')) {
  base = 'https://uri.libcal.com/api/1.1'
}
const roomItemIds = env.VITE_ROOM_ITEM_IDS ? env.VITE_ROOM_ITEM_IDS.split(',').map(s => s.trim()).filter(Boolean) : []

if (!token) {
  console.error('No VITE_LIBCAL_TOKEN found in .env. Please add your LibCal token to .env and retry.')
  process.exit(1)
}
if (!roomItemIds.length) {
  console.error('No VITE_ROOM_ITEM_IDS found in .env. Please configure the room ids.')
  process.exit(1)
}

(async () => {
  const out = {}
  for (const id of roomItemIds) {
    try {
      const url = `/api/v1/rooms/${id}`
      console.log('Fetching', url)
      const res = await fetch(url, { headers: { 'Authorization': `Bearer ${token}` } })
      if (!res.ok) {
        console.warn('Failed to fetch item', id, 'status', res.status)
        continue
      }
      const data = await res.json()
      const obj = Array.isArray(data) ? data[0] : data
      if (obj && obj.capacity != null) {
        out[id] = Number(obj.capacity)
        console.log('Got capacity for', id, out[id])
      } else {
        console.warn('No capacity found for', id)
      }
    } catch (e) {
      console.warn('Error fetching item', id, e.message)
    }
  }

  const dest = path.resolve(process.cwd(), 'src', 'roomMeta.json')
  fs.writeFileSync(dest, JSON.stringify(out, null, 2))
  console.log('Wrote', dest)
})()
