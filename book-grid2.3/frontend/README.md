# BookGrid Vue Frontend

Vue.js frontend for the Library Room Booking System (Rails backend).

## Setup

1. Install dependencies:
```bash
npm install
```

2. Configure environment variables:
Copy `.env.example` to `.env` and update as needed.

3. Start the development server:
```bash
npm run dev
```

The app will be available at http://localhost:5173/

## Backend Connection

The Vue frontend connects to the Rails backend API at `http://localhost:3000` via Vite proxy.

Make sure the Rails backend is running:
```bash
cd ..
bundle exec rackup -p 3000
```

## API Endpoints Used

- `GET /api/v1/rooms` - Fetch all rooms with availability
- `GET /api/v1/rooms/:id/availability?date=YYYY-MM-DD` - Fetch room details and availability
- `GET /api/v1/bookings?date=YYYY-MM-DD` - Fetch bookings for a date
- `POST /api/v1/bookings` - Create a new booking

All authentication is handled by the Rails backend.

## Build for Production

```bash
npm run build
```

Built files will be in the `dist/` directory.
