# Startup initializer - link LibCal token on app boot
Rails.application.config.after_initialize do
  begin
    token_manager = TokenManagerService.new
    
    # Link token with initial access token
    access_token = "ec048acd6c943b97204b839efcc8e57759ec5178"
    expires_at = 1.hour.from_now
    
    linked = token_manager.link_token(access_token, expires_at)
    
    if linked
      Rails.logger.info "LibCal token linked successfully at startup"
    else
      Rails.logger.error "Failed to link LibCal token at startup"
    end
  rescue => e
    Rails.logger.error "Error linking token at startup: #{e.message}"
  end
end
