Rails.application.routes.draw do
  # Health check endpoint
  get "up" => "rails/health#show", as: :rails_health_check

  # API routes
  namespace :api do
    namespace :v1 do
      resources :rooms, only: [:index, :show] do
        collection do
          get :availability
        end
        member do
          get :availability
        end
      end
      
      resources :bookings, only: [:index, :create] do
        collection do
          get :by_date
        end
      end

      # Token management
      post 'token/refresh', to: 'tokens#refresh'
      post 'token/link', to: 'tokens#link'
    end
  end

  # Root path
  root "api/v1/rooms#index"
end
