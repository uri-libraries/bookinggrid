# frozen_string_literal: true

module Api
  module V1
    class RoomsController < ApplicationController
      before_action :set_libcal_service

      # GET /api/v1/rooms
      def index
        date = params[:date] ? Date.parse(params[:date]) : Date.today
        rooms = @libcal_service.fetch_all_rooms(date)
        
        render json: {
          rooms: rooms,
          date: date,
          count: rooms.size
        }
      end

      # GET /api/v1/rooms/:id
      def show
        room_id = params[:id].to_i
        date = params[:date] ? Date.parse(params[:date]) : Date.today
        
        room = @libcal_service.fetch_room_details(room_id, date)
        
        if room
          render json: room
        else
          render json: { error: 'Room not found' }, status: :not_found
        end
      end

      # GET /api/v1/rooms/availability
      def availability
        date = params[:date] ? Date.parse(params[:date]) : Date.today
        rooms = @libcal_service.fetch_all_rooms(date)
        
        availability_data = rooms.map do |room|
          {
            id: room['id'],
            name: room['name'],
            zone: room['zone'],
            capacity: room['capacity'],
            is_available_now: room_available_now?(room),
            availability_slots: room['availability'] || []
          }
        end
        
        render json: {
          date: date,
          rooms: availability_data
        }
      end

      private

      def set_libcal_service
        @libcal_service = LibcalService.new
      end

      def room_available_now?(room)
        return false unless room['availability']
        
        now = Time.current
        room['availability'].any? do |slot|
          slot_from = Time.parse(slot['from'])
          slot_to = Time.parse(slot['to'])
          slot_from <= now && now <= slot_to
        end
      end
    end
  end
end
