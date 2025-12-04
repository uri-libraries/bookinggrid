import { ref, computed } from 'vue'

const currentToken = ref(localStorage.getItem('libcalToken') || import.meta.env.VITE_LIBCAL_TOKEN)
const tokenExpiry = ref(localStorage.getItem('tokenExpiry'))
const isRefreshing = ref(false)

export const useTokenManager = () => {
  const isTokenValid = computed(() => {
    if (!tokenExpiry.value) return false // Need to fetch token if no expiry set
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
      const apiKey = import.meta.env.VITE_AUTH_SERVICE_API_KEY
      
      console.log('Refreshing token...')
      console.log('URL:', url)
      console.log('API Key:', apiKey ? `${apiKey.substring(0, 20)}...` : 'MISSING')
      
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${apiKey}`
        },
        body: JSON.stringify({
          user_id: 'libcal'
        })
      })

      if (!response.ok) {
        throw new Error(`Token refresh failed: ${response.status}`)
      }

      const data = await response.json()
      
      // Calculate expires_in from expires_at timestamp
      const expiresAt = new Date(data.expires_at)
      const now = new Date()
      const expiresIn = Math.floor((expiresAt - now) / 1000)
      
      setToken(data.access_token, expiresIn)
      
      console.log('Token refreshed successfully')
      return data.access_token
    } catch (error) {
      console.error('Token refresh failed:', error)
      // Fall back to original token from env
      const fallbackToken = import.meta.env.VITE_LIBCAL_TOKEN
      if (fallbackToken) {
        currentToken.value = fallbackToken
        console.log('Falling back to static token')
        return fallbackToken
      }
      throw error
    } finally {
      isRefreshing.value = false
    }
  }

  const getValidToken = async () => {
    if (!isTokenValid.value || needsRefresh.value) {
      try {
        return await refreshToken()
      } catch (error) {
        console.warn('Token refresh failed, using current token:', error.message)
        return currentToken.value
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
    getValidToken
  }
}