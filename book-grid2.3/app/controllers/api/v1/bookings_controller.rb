# frozen_string_literal: true

module Api
  module V1
    class BookingsController < ApplicationController
      before_action :set_libcal_service

      # GET /api/v1/bookings
      def index
        date = params[:date] ? Date.parse(params[:date]) : Date.today
        bookings = @libcal_service.fetch_bookings(date)
        
        render json: {
          bookings: bookings,
          date: date,
          count: bookings.size
        }
      end

      # GET /api/v1/bookings/by_date
      def by_date
        start_date = params[:start_date] ? Date.parse(params[:start_date]) : Date.today
        end_date = params[:end_date] ? Date.parse(params[:end_date]) : start_date
        
        all_bookings = []
        (start_date..end_date).each do |date|
          bookings = @libcal_service.fetch_bookings(date)
          all_bookings.concat(bookings)
        end
        
        render json: {
          bookings: all_bookings,
          start_date: start_date,
          end_date: end_date,
          count: all_bookings.size
        }
      end

      # POST /api/v1/bookings
      def create
        result = @libcal_service.create_booking(booking_params)
        
        if result[:success]
          render json: result[:data], status: :created
        else
          render json: { error: result[:error] }, status: result[:code] || :unprocessable_entity
        end
      end

      private

      def set_libcal_service
        @libcal_service = LibcalService.new
      end

      def booking_params
        params.permit(:eid, :lid, :start, :fname, :lname, :email)
      end
    end
  end
end
