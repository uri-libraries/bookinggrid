# frozen_string_literal: true

# Service for interacting with LibCal API
class LibcalService
  include HTTParty
  base_uri ENV['LIBCAL_BASE_URL']

  def initialize
    @token_manager = TokenManagerService.new
    @location_id = ENV['LIBCAL_LOCATION_ID']
    @room_item_ids = ENV['LIBCAL_ROOM_ITEM_IDS']&.split(',')&.map(&:to_i) || []
  end

  # Fetch bookings for a specific date
  def fetch_bookings(date = Date.today)
    Rails.logger.info "Fetching bookings for #{date}"
    
    token = @token_manager.get_valid_token
    formatted_date = date.strftime('%Y-%m-%d')

    response = self.class.get(
      "/space/bookings",
      query: { lid: @location_id, date: formatted_date },
      headers: { 'Authorization' => "Bearer #{token}" }
    )

    if response.success?
      bookings = JSON.parse(response.body)
      Rails.logger.info "Retrieved #{bookings.size} bookings for #{date}"
      bookings
    else
      Rails.logger.error "Failed to fetch bookings: #{response.code}"
      []
    end
  end

  # Fetch room details with availability
  def fetch_room_details(room_id, date = Date.today)
    token = @token_manager.get_valid_token
    formatted_date = date.strftime('%Y-%m-%d')

    response = self.class.get(
      "/space/item/#{room_id}",
      query: { availability: formatted_date },
      headers: { 'Authorization' => "Bearer #{token}" }
    )

    if response.success?
      rooms = JSON.parse(response.body)
      room = rooms.is_a?(Array) ? rooms.first : rooms
      
      if room
        room['zone'] = get_room_zone(room_id)
        Rails.logger.info "Retrieved details for room #{room_id}"
      end
      
      room
    else
      Rails.logger.error "Failed to fetch room #{room_id}: #{response.code}"
      nil
    end
  end

  # Fetch all rooms with their details and availability
  def fetch_all_rooms(date = Date.today)
    Rails.logger.info "Fetching fresh room data for #{date}"
    
    bookings = fetch_bookings(date)
    rooms = []

    @room_item_ids.each do |room_id|
      room = fetch_room_details(room_id, date)
      if room
        # Add bookings for this room
        room['bookings'] = bookings.select { |b| b['eid'] == room_id }
        rooms << room
      end
    end

    Rails.logger.info "Retrieved #{rooms.size} rooms for #{date}"
    rooms
  end

  # Get zone for a room based on room ID
  def get_room_zone(room_id)
    zone_map = {
      211617 => 'Main Floor', 211618 => 'Main Floor', 211619 => 'Main Floor',
      211620 => 'Main Floor', 211621 => 'Main Floor', 211623 => 'Basement',
      211624 => 'Basement', 211625 => 'Basement', 211626 => 'Basement',
      211627 => 'Basement', 211628 => 'Basement', 211629 => 'Basement',
      211630 => 'Basement', 211631 => 'Basement', 211632 => 'Basement',
      211633 => 'Basement', 211634 => 'Basement', 211635 => 'Basement',
      211636 => 'Basement', 211637 => 'Basement', 213873 => 'Second Floor',
      213874 => 'Second Floor'
    }
    zone_map[room_id] || 'Unknown'
  end

  # Create a booking
  def create_booking(params)
    token = @token_manager.get_valid_token

    response = self.class.post(
      "/space/book",
      body: params,
      headers: { 
        'Authorization' => "Bearer #{token}",
        'Content-Type' => 'application/x-www-form-urlencoded'
      }
    )

    if response.success?
      Rails.logger.info "Booking created successfully for room #{params[:eid]}"
      { success: true, data: JSON.parse(response.body) }
    else
      Rails.logger.error "Failed to create booking: #{response.code} - #{response.body}"
      { success: false, error: response.body, code: response.code }
    end
  end
end
