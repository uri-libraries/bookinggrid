import { ref, computed } from 'vue'

const currentToken = ref(localStorage.getItem('libcalToken') || import.meta.env.VITE_LIBCAL_TOKEN)
const tokenExpiry = ref(localStorage.getItem('tokenExpiry'))
const isRefreshing = ref(false)
const initialized = ref(false)

export const useTokenManager = () => {
  const isTokenValid = computed(() => {
    if (!tokenExpiry.value) return false
    return new Date() < new Date(tokenExpiry.value)
  })

  const needsRefresh = computed(() => {
    if (!tokenExpiry.value) return false
    const expiryTime = new Date(tokenExpiry.value)
    const now = new Date()
    // Refresh if token expires in the next 10 minutes
    return (expiryTime.getTime() - now.getTime()) < 10 * 60 * 1000
  })

  const setToken = (token, expiryInSeconds = null) => {
    currentToken.value = token
    localStorage.setItem('libcalToken', token)
    
    if (expiryInSeconds) {
      const expiry = new Date(Date.now() + (expiryInSeconds * 1000)).toISOString()
      tokenExpiry.value = expiry
      localStorage.setItem('tokenExpiry', expiry)
    }
  }

  const refreshToken = async () => {
    if (isRefreshing.value) {
      // Already refreshing, wait for it
      return new Promise((resolve) => {
        const checkRefresh = () => {
          if (!isRefreshing.value) {
            resolve(currentToken.value)
          } else {
            setTimeout(checkRefresh, 100)
          }
        }
        checkRefresh()
      })
    }

    isRefreshing.value = true
    
    try {
      const url = import.meta.env.VITE_TOKEN_REFRESH_URL
      const authToken = import.meta.env.VITE_AUTH_SERVICE_TOKEN
      
      console.log('=== Token Refresh Debug ===')
      console.log('URL:', url)
      console.log('Auth token exists:', !!authToken)
      
      if (!url || !authToken) {
        throw new Error(`Missing configuration: url=${!!url}, authToken=${!!authToken}`)
      }
      
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${authToken}`
        },
        body: JSON.stringify({
          user_id: 'booking_grid',
          metadata: { source: 'booking-grid-app' }
        })
      })

      console.log('Response status:', response.status)
      
      if (!response.ok) {
        const errorText = await response.text()
        console.error('Token refresh failed:', response.status, errorText)
        throw new Error(`Token refresh failed: ${response.status} - ${errorText}`)
      }

      const data = await response.json()
      console.log('Token data received:', { expires_at: data.expires_at, link_id: data.link_id })
      
      // Calculate expires_in from expires_at timestamp
      const expiresAt = new Date(data.expires_at)
      const now = new Date()
      const expiresIn = Math.floor((expiresAt - now) / 1000)
      
      setToken(data.access_token, expiresIn)
      
      console.log('Token refreshed successfully, expires in:', expiresIn, 'seconds')
      return data.access_token
    } catch (error) {
      console.error('Token refresh error details:', error.message)
      throw error
    } finally {
      isRefreshing.value = false
    }
  }

  const initialize = async () => {
    if (initialized.value) return
    
    console.log('Initializing token manager...')
    
    // Always fetch a fresh token on startup to ensure we have a valid one
    try {
      await refreshToken()
      initialized.value = true
      console.log('Token manager initialized successfully')
    } catch (error) {
      console.error('Failed to initialize token manager:', error)
      // If we can't get a token, the app won't work
      throw new Error('Failed to initialize authentication')
    }
  }

  const getValidToken = async () => {
    // Ensure we're initialized first
    if (!initialized.value) {
      await initialize()
    }
    
    console.log('getValidToken called')
    console.log('isTokenValid:', isTokenValid.value)
    console.log('needsRefresh:', needsRefresh.value)
    
    if (!isTokenValid.value || needsRefresh.value) {
      try {
        return await refreshToken()
      } catch (error) {
        console.error('Token refresh failed:', error)
        throw error
      }
    }
    
    return currentToken.value
  }

  return {
    currentToken: computed(() => currentToken.value),
    isTokenValid,
    needsRefresh,
    isRefreshing: computed(() => isRefreshing.value),
    setToken,
    refreshToken,
    getValidToken,
    initialize
  }
}