# BookGrid - ASP.NET Core + Blazor Server Room Booking System

A robust, real-time room booking application built with ASP.NET Core and Blazor Server, featuring automatic token refresh, real-time updates via SignalR, and comprehensive error handling.

## Features

- **Real-time Updates**: SignalR integration for live booking updates
- **Automatic Token Refresh**: Seamless LibCal API token management
- **Resilient Architecture**: Polly retry policies and circuit breakers
- **Responsive Design**: Works on desktop and mobile devices
- **Comprehensive Logging**: Serilog integration with structured logging
- **Caching**: Memory caching for improved performance
- **Error Handling**: Graceful degradation and user-friendly error messages

## Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Blazor UI     │◄──►│  ASP.NET Core    │◄──►│   LibCal API    │
│  (Components)   │    │   (Backend)      │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │
                       ┌──────────────────┐
                       │  Memory Cache    │
                       │  (Tokens/Rooms)  │
                       └──────────────────┘
```

## Prerequisites

- .NET 8.0 SDK
- LibCal API credentials
- FastAPI token refresh service (optional)

## Setup

1. **Clone and navigate to the project**:
   ```bash
   cd book-grid2.1
   ```

2. **Configure settings**:
   Update `appsettings.json` with your LibCal API details:
   ```json
   {
     "LibCal": {
       "StaticToken": "your_libcal_token_here",
       "LocationId": "your_location_id",
       "RoomItemIds": [211617, 211618, ...],
       "TokenRefreshUrl": "http://auth.library.uri.edu/api/v1/libcal/refresh"
     }
   }
   ```

3. **Install dependencies**:
   ```bash
   dotnet restore
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```

5. **Access the application**:
   Open https://localhost:5001 in your browser

## Key Components

### Services

- **TokenManager**: Handles automatic token refresh with retry logic
- **LibCalService**: Manages all LibCal API interactions with circuit breakers
- **RoomService**: Provides cached room data with filtering capabilities

### Components

- **Home.razor**: Main booking interface with real-time updates
- **RoomCard.razor**: Individual room component with booking functionality
- **BookingHub**: SignalR hub for real-time communication

### Models

- **Room**: Complete room information with availability and bookings
- **BookingRequest**: Structured booking data
- **TimeSegment**: Timeline visualization data

## Configuration Options

### Retry Policies
- **Retry Count**: 3 attempts with exponential backoff
- **Circuit Breaker**: Opens after 5 failures, stays open for 30 seconds

### Caching
- **Room Data**: 5-minute cache (configurable)
- **Token Cache**: 50-minute cache with automatic refresh

### Logging
- **Console**: Development logging
- **File**: Production logs in `/logs` directory
- **Structured**: JSON formatting with correlation IDs

## Production Deployment

1. **Build for production**:
   ```bash
   dotnet publish -c Release
   ```

2. **Configure production settings**:
   - Update `appsettings.Production.json`
   - Set connection strings for persistent caching
   - Configure authentication if required

3. **Deploy**:
   - Docker container
   - Azure App Service
   - IIS
   - Linux with Kestrel

## Docker Deployment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY publish/ /app/
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "BookGrid.dll"]
```

## Monitoring & Logging

The application includes comprehensive logging:

- **Request/Response**: All LibCal API interactions
- **Performance**: Cache hit/miss ratios, response times
- **Errors**: Detailed error context with correlation IDs
- **SignalR**: Connection events and message flow

## Failsafes Implemented

1. **Token Management**: Automatic refresh with fallback to static token
2. **API Resilience**: Retry policies and circuit breakers via Polly
3. **Caching**: Memory cache with configurable expiration
4. **Real-time Fallback**: Graceful degradation if SignalR fails
5. **Input Validation**: Comprehensive form validation
6. **Error Boundaries**: User-friendly error messages

## Future Enhancements

- **Database Integration**: Persistent storage for bookings and audit trails
- **Authentication**: User authentication and authorization
- **Admin Panel**: Room management and booking administration
- **Mobile App**: Native mobile application
- **Analytics**: Usage analytics and reporting
- **Multi-tenant**: Support for multiple libraries/locations