# frozen_string_literal: true

class ApplicationController < ActionController::API
  rescue_from StandardError, with: :handle_standard_error
  rescue_from ActiveRecord::RecordNotFound, with: :handle_not_found

  private

  def handle_standard_error(exception)
    Rails.logger.error "Error: #{exception.message}"
    Rails.logger.error exception.backtrace.join("\n")
    
    render json: {
      error: 'Internal server error',
      message: exception.message
    }, status: :internal_server_error
  end

  def handle_not_found(exception)
    render json: {
      error: 'Not found',
      message: exception.message
    }, status: :not_found
  end
end
