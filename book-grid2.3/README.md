# BookGrid Ruby on Rails API

A Ruby on Rails API application for managing LibCal room bookings and availability.

## Prerequisites

- Ruby 3.2.0 or higher
- Rails 7.1.0 or higher
- Redis (for caching)
- SQLite3 (for development)

## Installation

1. Install dependencies:
```bash
bundle install
```

2. Setup the database:
```bash
rails db:setup
```

3. Copy the environment file and configure:
```bash
cp .env.example .env
# Edit .env with your LibCal API credentials
```

## Configuration

Edit `.env` file with your LibCal API credentials:

```
LIBCAL_BASE_URL=https://uri.libcal.com/api/1.1
LIBCAL_STATIC_TOKEN=your_libcal_token_here
LIBCAL_REFRESH_TOKEN=your_refresh_token_here
LIBCAL_USER_ID=your_user_id
LIBCAL_LOCATION_ID=23510
LIBCAL_ROOM_ITEM_IDS=211617,211618,211619,211620,211621,211623,211624,211625,211626,211627,211628,211629,211630,211631,211632,211633,211634,211635,211636,211637,213873,213874
```

## Running the Application

Start the Rails server:

```bash
rails server
```

The API will be available at `http://localhost:3000`

## API Endpoints

### Rooms

- `GET /api/v1/rooms` - Get all rooms with availability
- `GET /api/v1/rooms/:id` - Get specific room details
- `GET /api/v1/rooms/availability` - Get availability for all rooms

Query parameters:
- `date` - Date in YYYY-MM-DD format (defaults to today)

### Bookings

- `GET /api/v1/bookings` - Get bookings for a specific date
- `GET /api/v1/bookings/by_date` - Get bookings for a date range

Query parameters:
- `date` - Date in YYYY-MM-DD format (defaults to today)
- `start_date` - Start date for range queries
- `end_date` - End date for range queries

### Token Management

- `POST /api/v1/token/refresh` - Refresh LibCal access token
- `POST /api/v1/token/link` - Link LibCal token to user

## Architecture

### Services

- **TokenManagerService** - Manages LibCal API token lifecycle, including caching and refresh
- **LibcalService** - Handles all LibCal API interactions for rooms and bookings

### Controllers

- **RoomsController** - Endpoints for room data and availability
- **BookingsController** - Endpoints for booking data
- **TokensController** - Endpoints for token management

## Caching

The application uses Redis for caching:
- LibCal tokens are cached until 5 minutes before expiry
- Token expiry times are tracked separately

## Logging

Logs are configured to output to:
- Console (STDOUT)
- `log/development.log` (in development)
- `log/production.log` (in production)

## Testing

Run tests with:

```bash
rails test
```

## Deployment

For production deployment:

1. Set `RAILS_ENV=production`
2. Precompile assets if needed
3. Set up production database
4. Configure Redis URL
5. Set all required environment variables

## License

Proprietary - University of Rhode Island Library
