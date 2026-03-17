import { ref, computed, type Ref, type ComputedRef } from 'vue'

const currentToken: Ref<string | null> = ref(localStorage.getItem('libcalToken'))
const tokenExpiry: Ref<string | null> = ref(localStorage.getItem('tokenExpiry'))
const isRefreshing: Ref<boolean> = ref(false)
const initialized: Ref<boolean> = ref(false)

const OAUTH_URL = import.meta.env.VITE_LIBCAL_OAUTH_URL || '/oauth/token'
console.log('OAUTH_URL at runtime:', OAUTH_URL)
const CLIENT_ID = import.meta.env.VITE_LIBCAL_CLIENT_ID as string
const CLIENT_SECRET = import.meta.env.VITE_LIBCAL_CLIENT_SECRET as string

interface TokenResponse {
  access_token: string
  token_type: string
  expires_in: number
}

interface TokenManager {
  currentToken: ComputedRef<string | null>
  isTokenValid: ComputedRef<boolean>
  needsRefresh: ComputedRef<boolean>
  isRefreshing: ComputedRef<boolean>
  setToken: (token: string, expiresAt: string) => void
  refreshToken: () => Promise<string>
  getValidToken: () => Promise<string | null>
  initialize: () => Promise<void>
}

export const useTokenManager = (): TokenManager => {
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

  const setToken = (token: string, expiresAt: string): void => {
    currentToken.value = token
    tokenExpiry.value = expiresAt
    localStorage.setItem('libcalToken', token)
    localStorage.setItem('tokenExpiry', expiresAt)
    
    console.log('Token stored, expires at:', expiresAt)
  }

  const clearToken = (): void => {
    currentToken.value = null
    tokenExpiry.value = null
    localStorage.removeItem('libcalToken')
    localStorage.removeItem('tokenExpiry')
  }

  const refreshToken = async (): Promise<string> => {
    if (isRefreshing.value) {
      // Wait for the existing refresh to complete
      while (isRefreshing.value) {
        await new Promise(resolve => setTimeout(resolve, 100))
      }
      return currentToken.value as string
    }

    isRefreshing.value = true
    
    try {
      console.log('Fetching LibCal OAuth token...')
      
      const response = await fetch(OAUTH_URL, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: new URLSearchParams({
          grant_type: 'client_credentials',
          client_id: CLIENT_ID,
          client_secret: CLIENT_SECRET,
          scope: 'sp_r sp_w'
        })
      })

      if (!response.ok) {
        const text = await response.text()
        throw new Error(`LibCal OAuth error: ${response.status} ${text}`)
      }

      const data: TokenResponse = await response.json()
      
      if (!data.access_token) {
        throw new Error('No access_token in OAuth response')
      }

      // Calculate expiry time
      const expiresAt = new Date(Date.now() + data.expires_in * 1000).toISOString()
      setToken(data.access_token, expiresAt)
      
      console.log('LibCal token refreshed successfully')
      console.log('Token type:', data.token_type)
      console.log('Expires in:', data.expires_in, 'seconds')
      return data.access_token
    } catch (error) {
      console.error('Token refresh error:', error instanceof Error ? error.message : error)
      clearToken()
      throw error
    } finally {
      isRefreshing.value = false
    }
  }

  const initialize = async (): Promise<void> => {
    if (initialized.value) return
    
    console.log('Initializing LibCal token manager...')
    
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

  const getValidToken = async (): Promise<string | null> => {
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
