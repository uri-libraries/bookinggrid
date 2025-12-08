import { ref, computed } from 'vue'

const currentToken = ref(localStorage.getItem('libcalToken'))
const tokenExpiry = ref(localStorage.getItem('tokenExpiry'))
const isRefreshing = ref(false)
const initialized = ref(false)

export const useTokenManager = () => {
  const isTokenValid = computed(() => {
    if (!currentToken.value || !tokenExpiry.value) return false
    return new Date() < new Date(tokenExpiry.value)
  })

  const needsRefresh = computed(() => {
    if (!tokenExpiry.value) return true
    const expiryTime = new Date(tokenExpiry.value)
    const now = new Date()
    // Refresh if token expires in the next 5 minutes
    return (expiryTime.getTime() - now.getTime()) < 5 * 60 * 1000
  })

  const setToken = (token, expiryInSeconds) => {
    currentToken.value = token
    localStorage.setItem('libcalToken', token)
    
    const expiry = new Date(Date.now() + (expiryInSeconds * 1000)).toISOString()
    tokenExpiry.value = expiry
    localStorage.setItem('tokenExpiry', expiry)
    
    console.log('Token stored, expires at:', expiry)
  }

  const clearToken = () => {
    currentToken.value = null
    tokenExpiry.value = null
    localStorage.removeItem('libcalToken')
    localStorage.removeItem('tokenExpiry')
  }

  const refreshToken = async () => {
    if (isRefreshing.value) {
      // Wait for the existing refresh to complete
      while (isRefreshing.value) {
        await new Promise(resolve => setTimeout(resolve, 100))
      }
      return currentToken.value
    }

    isRefreshing.value = true
    
    try {
      console.log('Fetching new LibCal OAuth token...')
      
      const clientId = import.meta.env.VITE_LIBCAL_CLIENT_ID
      const clientSecret = import.meta.env.VITE_LIBCAL_CLIENT_SECRET
      
      if (!clientId || !clientSecret) {
        throw new Error('LibCal OAuth credentials not configured')
      }

      const response = await fetch('/oauth/token', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `client_id=${clientId}&client_secret=${clientSecret}&grant_type=client_credentials&scope=sp_r sp_w`
      })

      if (!response.ok) {
        const text = await response.text()
        throw new Error(`OAuth token request failed: ${response.status} ${text}`)
      }

      const data = await response.json()
      
      if (!data.access_token) {
        throw new Error('No access_token in OAuth response')
      }

      // LibCal tokens typically expire in 3600 seconds (1 hour)
      const expiresIn = data.expires_in || 3600
      setToken(data.access_token, expiresIn)
      
      console.log('LibCal OAuth token refreshed successfully')
      console.log('Token scope:', data.scope)
      console.log('Expires in:', expiresIn, 'seconds')
      return data.access_token
    } catch (error) {
      console.error('Token refresh error:', error.message)
      clearToken()
      throw error
    } finally {
      isRefreshing.value = false
    }
  }

  const initialize = async () => {
    if (initialized.value) return
    
    console.log('Initializing LibCal OAuth token manager...')
    
    try {
      // If we have a valid token, use it; otherwise fetch a new one
      if (!isTokenValid.value) {
        await refreshToken()
      }
      initialized.value = true
      console.log('Token manager initialized successfully')
    } catch (error) {
      console.error('Failed to initialize token manager:', error)
      throw new Error('Failed to initialize authentication')
    }
  }

  const getValidToken = async () => {
    // Ensure we're initialized first
    if (!initialized.value) {
      await initialize()
    }
    
    // If token is invalid or needs refresh, get a new one
    if (!isTokenValid.value || needsRefresh.value) {
      console.log('Token invalid or needs refresh, fetching new token...')
      return await refreshToken()
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
