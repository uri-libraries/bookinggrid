# frozen_string_literal: true

module Api
  module V1
    class TokensController < ApplicationController
      before_action :set_token_manager

      # POST /api/v1/token/refresh
      def refresh
        user_id = params[:user_id]
        refresh_token = params[:refresh_token]

        if user_id.blank? || refresh_token.blank?
          render json: { error: 'user_id and refresh_token are required' }, status: :unprocessable_entity
          return
        end

        # In a real implementation, you would validate the refresh token
        # For now, we'll just attempt to refresh using the TokenManagerService
        token = @token_manager.refresh_token

        if token
          render json: {
            access_token: token,
            expires_at: (Rails.cache.read('libcal_token_expiry') || 1.hour.from_now).iso8601,
            token_type: 'Bearer'
          }
        else
          render json: { error: 'Token refresh failed' }, status: :unauthorized
        end
      end

      # POST /api/v1/token/link
      def link
        access_token = params[:access_token]
        expires_at = params[:expires_at]

        if access_token.blank? || expires_at.blank?
          render json: { error: 'access_token and expires_at are required' }, status: :unprocessable_entity
          return
        end

        expiry_time = Time.parse(expires_at)
        metadata = params[:metadata] || {}

        success = @token_manager.link_token(access_token, expiry_time, metadata)

        if success
          render json: {
            message: 'Token linked successfully',
            user_id: ENV['LIBCAL_USER_ID'],
            provider: 'libcal'
          }, status: :created
        else
          render json: { error: 'Failed to link token' }, status: :unprocessable_entity
        end
      end

      private

      def set_token_manager
        @token_manager = TokenManagerService.new
      end
    end
  end
end
