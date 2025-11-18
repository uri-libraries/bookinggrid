# frozen_string_literal: true

# Service for managing LibCal API token lifecycle
class TokenManagerService
  include HTTParty
  base_uri ENV['AUTH_SERVICE_BASE_URL']

  TOKEN_CACHE_KEY = 'libcal_token'
  REFRESH_TOKEN_CACHE_KEY = 'libcal_refresh_token'
  EXPIRY_CACHE_KEY = 'libcal_token_expiry'

  def initialize
    @static_token = ENV['LIBCAL_STATIC_TOKEN']
    @refresh_token = ENV['LIBCAL_REFRESH_TOKEN']
    @user_id = ENV['LIBCAL_USER_ID']
    @token_refresh_url = ENV['AUTH_SERVICE_TOKEN_REFRESH_URL']
    @token_link_url = ENV['AUTH_SERVICE_TOKEN_LINK_URL']
  end

  # Get a valid token (cached or refreshed)
  def get_valid_token
    cached_token = Rails.cache.read(TOKEN_CACHE_KEY)
    cached_expiry = Rails.cache.read(EXPIRY_CACHE_KEY)

    # Return cached token if valid
    if cached_token && cached_expiry && cached_expiry > 10.minutes.from_now
      return cached_token
    end

    # Try to refresh token
    if @token_refresh_url.present?
      begin
        refreshed_token = refresh_token
        return refreshed_token if refreshed_token
      rescue => e
        Rails.logger.warn "Token refresh failed: #{e.message}, falling back to static token"
      end
    end

    # Fall back to static token
    Rails.logger.info "Using static token as fallback"
    @static_token
  end

  # Refresh the LibCal token
  def refresh_token
    current_refresh_token = Rails.cache.read(REFRESH_TOKEN_CACHE_KEY) || @refresh_token

    return nil if current_refresh_token.blank?

    Rails.logger.info "Attempting to refresh LibCal token"

    request_body = {
      user_id: @user_id,
      refresh_token: current_refresh_token,
      metadata: {}
    }

    response = self.class.post(
      @token_refresh_url,
      body: request_body.to_json,
      headers: { 'Content-Type' => 'application/json' }
    )

    unless response.success?
      Rails.logger.error "Token refresh failed with status: #{response.code}, body: #{response.body}"
      return nil
    end

    token_data = JSON.parse(response.body)

    if token_data['access_token']
      expiry_time = Time.parse(token_data['expires_at'])
      
      # Cache the new access token
      Rails.cache.write(TOKEN_CACHE_KEY, token_data['access_token'], expires_in: expiry_time - 5.minutes)
      Rails.cache.write(EXPIRY_CACHE_KEY, expiry_time)

      Rails.logger.info "Token refreshed successfully, expires at #{expiry_time}"
      return token_data['access_token']
    end

    Rails.logger.error "Invalid token response received"
    nil
  end

  # Link LibCal token to user
  def link_token(access_token, expires_at, metadata = {})
    Rails.logger.info "Linking LibCal token for user_id: #{@user_id}"

    request_body = {
      user_id: @user_id,
      refresh_token: @refresh_token,
      access_token: access_token,
      expires_at: expires_at.iso8601,
      metadata: metadata
    }

    response = self.class.post(
      @token_link_url,
      body: request_body.to_json,
      headers: { 'Content-Type' => 'application/json' }
    )

    if response.success?
      Rails.logger.info "Token linked successfully: #{response.body}"
      true
    else
      Rails.logger.error "Failed to link token: #{response.code}, #{response.body}"
      false
    end
  end

  # Clear cached token
  def clear_token
    Rails.cache.delete(TOKEN_CACHE_KEY)
    Rails.cache.delete(EXPIRY_CACHE_KEY)
    Rails.logger.info "Token cache cleared"
  end
end
