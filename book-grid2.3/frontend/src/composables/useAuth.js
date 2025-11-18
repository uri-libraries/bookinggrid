import { ref, computed } from 'vue'

// For the Rails backend, we don't need client-side token management
// The backend handles all LibCal token refresh internally
export const useTokenManager = () => {
  // No-op token manager since Rails backend handles everything
  const getValidToken = async () => {
    // Return empty string - Rails backend doesn't need client tokens
    return ''
  }

  return {
    currentToken: computed(() => ''),
    isTokenValid: computed(() => true),
    needsRefresh: computed(() => false),
    isRefreshing: computed(() => false),
    setToken: () => {},
    refreshToken: async () => '',
    getValidToken
  }
}