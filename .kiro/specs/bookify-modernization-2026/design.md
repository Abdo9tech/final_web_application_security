# Design Document

## Overview

This design document outlines the technical approach for modernizing the Bookify Hotel application to meet 2026 professional standards. The modernization focuses on fixing critical build errors, implementing a unique modern UI design system, enhancing AI capabilities, improving code architecture, and strengthening security measures.

The design follows a phased approach prioritizing critical fixes first, followed by UI modernization, feature enhancements, and finally optimization and testing improvements.

## Architecture

### High-Level Architecture

The application maintains its existing ASP.NET Core MVC architecture with the following layers:

```
┌─────────────────────────────────────────────────────────────┐
│                     Presentation Layer                       │
│  (Controllers, Razor Views, ViewModels, Client-side JS)     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                   Business Logic Layer (PLL)                 │
│        (Services, Business Rules, Validation Logic)          │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                   Data Access Layer (DAL)                    │
│     (Entity Framework Core, Repositories, DbContext)         │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                      SQL Server Database                     │
└─────────────────────────────────────────────────────────────┘
```

### New Components

1. **Design System Module**: Centralized UI component library
2. **AI Service Layer**: Enhanced AI assistant with NLP capabilities
3. **Notification Service**: Real-time notification delivery system
4. **PWA Service Worker**: Offline functionality and caching
5. **Analytics Service**: Dashboard metrics and reporting

## Components and Interfaces

### 1. Build Error Resolution

**Component**: Razor View Validator

**Purpose**: Fix syntax errors in Razor views preventing compilation

**Affected Files**:
- `Project(DEPI)/Views/Booking/Edit.cshtml` (line 212)
- `Project(DEPI)/Views/User/Create.cshtml` (line 233)

**Implementation**:
- Locate unclosed section blocks in Razor views
- Add missing closing braces
- Validate all Razor syntax across the project
- Add pre-commit hooks to prevent future syntax errors

### 2. Modern Design System

**Component**: UI Component Library

**Purpose**: Provide consistent, modern UI components across the application

**Design Tokens**:
```css
/* Color Palette - Unique 2026 Design */
--primary-50: #f0f4ff;
--primary-100: #e0e9ff;
--primary-200: #c7d7fe;
--primary-300: #a5b8fc;
--primary-400: #8b93f8;
--primary-500: #7c6ff2;  /* Main brand color */
--primary-600: #6d4ee6;
--primary-700: #5d3dcb;
--primary-800: #4d32a4;
--primary-900: #402a82;

/* Accent Colors */
--accent-coral: #ff6b6b;
--accent-teal: #4ecdc4;
--accent-amber: #ffd93d;
--accent-purple: #a78bfa;

/* Neutrals */
--neutral-50: #fafafa;
--neutral-100: #f5f5f5;
--neutral-200: #e5e5e5;
--neutral-300: #d4d4d4;
--neutral-400: #a3a3a3;
--neutral-500: #737373;
--neutral-600: #525252;
--neutral-700: #404040;
--neutral-800: #262626;
--neutral-900: #171717;

/* Typography */
--font-display: 'Inter Variable', system-ui, sans-serif;
--font-body: 'Inter', system-ui, sans-serif;
--font-mono: 'JetBrains Mono', monospace;

/* Spacing Scale */
--space-1: 0.25rem;
--space-2: 0.5rem;
--space-3: 0.75rem;
--space-4: 1rem;
--space-6: 1.5rem;
--space-8: 2rem;
--space-12: 3rem;
--space-16: 4rem;

/* Border Radius */
--radius-sm: 0.375rem;
--radius-md: 0.5rem;
--radius-lg: 0.75rem;
--radius-xl: 1rem;
--radius-2xl: 1.5rem;
--radius-full: 9999px;

/* Shadows */
--shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.05);
--shadow-md: 0 4px 6px -1px rgb(0 0 0 / 0.1);
--shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1);
--shadow-xl: 0 20px 25px -5px rgb(0 0 0 / 0.1);
--shadow-2xl: 0 25px 50px -12px rgb(0 0 0 / 0.25);

/* Animations */
--transition-fast: 150ms cubic-bezier(0.4, 0, 0.2, 1);
--transition-base: 200ms cubic-bezier(0.4, 0, 0.2, 1);
--transition-slow: 300ms cubic-bezier(0.4, 0, 0.2, 1);
```

**Key UI Components**:
1. **Button Component**: Multiple variants (primary, secondary, ghost, danger)
2. **Card Component**: Glassmorphic design with hover effects
3. **Input Component**: Floating labels with validation states
4. **Modal Component**: Smooth animations with backdrop blur
5. **Toast Component**: Non-intrusive notifications
6. **Skeleton Loader**: Content loading states
7. **Navigation Component**: Sticky header with scroll effects
8. **Footer Component**: Multi-column responsive layout

**Dark Mode Implementation**:
- CSS custom properties for theme switching
- LocalStorage persistence
- Smooth color transitions
- Respect system preferences

### 3. Enhanced AI Assistant

**Component**: AI Assistant Service

**Purpose**: Provide intelligent conversational support for users

**Interface**:
```csharp
public interface IAIAssistantService
{
    Task<AIResponse> ProcessQueryAsync(string userId, string query, string language = "en");
    Task<List<Room>> GetRoomRecommendationsAsync(string userId, RoomPreferences preferences);
    Task<ConversationContext> GetConversationContextAsync(string userId);
    Task SaveFeedbackAsync(string userId, string messageId, FeedbackType feedback);
    Task<bool> EscalateToHumanAsync(string userId, string reason);
}

public class AIResponse
{
    public string MessageId { get; set; }
    public string Response { get; set; }
    public List<RoomSuggestion> Suggestions { get; set; }
    public List<QuickAction> QuickActions { get; set; }
    public bool RequiresEscalation { get; set; }
    public double ConfidenceScore { get; set; }
}

public class RoomPreferences
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? Capacity { get; set; }
    public List<string> Amenities { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
}
```

**Implementation Strategy**:
- Use Azure OpenAI or similar service for NLP
- Implement conversation context management
- Create intent classification system
- Build knowledge base from hotel policies
- Implement fallback to human support

### 4. Advanced Search and Filtering

**Component**: Smart Search Service

**Purpose**: Provide fast, intelligent search with multiple filters

**Interface**:
```csharp
public interface ISmartSearchService
{
    Task<SearchResult> SearchRoomsAsync(SearchCriteria criteria);
    Task<FilterOptions> GetAvailableFiltersAsync(SearchCriteria currentCriteria);
    Task SaveSearchPreferencesAsync(string userId, SearchCriteria criteria);
    Task<SearchCriteria> GetSavedPreferencesAsync(string userId);
    Task<List<string>> GetSearchSuggestionsAsync(string partialQuery);
}

public class SearchCriteria
{
    public string Query { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public List<int> RoomTypeIds { get; set; }
    public int? MinCapacity { get; set; }
    public List<string> Amenities { get; set; }
    public string Location { get; set; }
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public SortOption SortBy { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class SearchResult
{
    public List<RoomDto> Rooms { get; set; }
    public int TotalCount { get; set; }
    public FilterOptions AppliedFilters { get; set; }
    public long SearchTimeMs { get; set; }
}
```

**Performance Optimizations**:
- Database indexing on searchable fields
- Query result caching with Redis
- Debounced search input
- Optimistic UI updates

### 5. Modern Payment Integration

**Component**: Enhanced Payment Service

**Purpose**: Provide seamless payment experience with Stripe Payment Element

**Interface**:
```csharp
public interface IEnhancedPaymentService
{
    Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency, string customerId);
    Task<PaymentMethod> SavePaymentMethodAsync(string customerId, string paymentMethodId);
    Task<List<PaymentMethod>> GetSavedPaymentMethodsAsync(string customerId);
    Task<Refund> ProcessRefundAsync(string paymentIntentId, decimal? amount = null);
    Task<bool> VerifyPaymentAsync(string paymentIntentId);
    Task SendPaymentConfirmationAsync(string userId, string bookingId);
}
```

**Implementation**:
- Upgrade to Stripe Payment Element
- Implement 3D Secure authentication
- Add support for digital wallets (Apple Pay, Google Pay)
- Implement automatic retry logic for failed payments
- Add webhook handling for async payment events

### 6. Real-Time Notifications

**Component**: Notification Service

**Purpose**: Deliver real-time notifications via multiple channels

**Interface**:
```csharp
public interface INotificationService
{
    Task SendNotificationAsync(string userId, Notification notification);
    Task<List<Notification>> GetUserNotificationsAsync(string userId, bool unreadOnly = false);
    Task MarkAsReadAsync(string userId, string notificationId);
    Task<NotificationPreferences> GetPreferencesAsync(string userId);
    Task UpdatePreferencesAsync(string userId, NotificationPreferences preferences);
    Task RegisterPushSubscriptionAsync(string userId, PushSubscription subscription);
}

public class Notification
{
    public string Id { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string ActionUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public Dictionary<string, object> Data { get; set; }
}

public enum NotificationType
{
    BookingConfirmed,
    BookingCancelled,
    PaymentReceived,
    PriceDropAlert,
    SystemAnnouncement
}
```

**Implementation**:
- SignalR for real-time browser notifications
- Background service for email notifications
- Web Push API for browser push notifications
- Database queue for notification persistence

### 7. Enhanced Admin Dashboard

**Component**: Analytics and Management Dashboard

**Purpose**: Provide comprehensive admin tools and analytics

**Features**:
1. **Real-time Metrics Dashboard**
   - Current occupancy rate
   - Revenue today/week/month
   - Active bookings count
   - Average booking value

2. **Interactive Charts**
   - Booking trends (line chart)
   - Revenue by room type (pie chart)
   - Occupancy heatmap (calendar view)
   - User growth (area chart)

3. **Management Tools**
   - Bulk room operations
   - Booking calendar view
   - User management with filters
   - Audit log viewer

4. **Report Generation**
   - PDF export with charts
   - Excel export with raw data
   - Scheduled email reports
   - Custom date range selection

**Technology Stack**:
- Chart.js for interactive charts
- FullCalendar for booking calendar
- DataTables for advanced table features
- jsPDF for PDF generation

### 8. Progressive Web App (PWA)

**Component**: Service Worker and PWA Manifest

**Purpose**: Enable offline functionality and app installation

**Service Worker Strategy**:
```javascript
// Cache-first for static assets
workbox.routing.registerRoute(
  /\.(?:js|css|png|jpg|jpeg|svg|gif|woff2)$/,
  new workbox.strategies.CacheFirst({
    cacheName: 'static-assets',
    plugins: [
      new workbox.expiration.ExpirationPlugin({
        maxEntries: 100,
        maxAgeSeconds: 30 * 24 * 60 * 60, // 30 days
      }),
    ],
  })
);

// Network-first for API calls
workbox.routing.registerRoute(
  /\/api\//,
  new workbox.strategies.NetworkFirst({
    cacheName: 'api-cache',
    plugins: [
      new workbox.expiration.ExpirationPlugin({
        maxEntries: 50,
        maxAgeSeconds: 5 * 60, // 5 minutes
      }),
    ],
  })
);

// Offline fallback
workbox.routing.setCatchHandler(({event}) => {
  if (event.request.destination === 'document') {
    return caches.match('/offline.html');
  }
  return Response.error();
});
```

**Manifest Configuration**:
```json
{
  "name": "Bookify Hotel",
  "short_name": "Bookify",
  "description": "Modern hotel booking platform",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#7c6ff2",
  "theme_color": "#7c6ff2",
  "icons": [
    {
      "src": "/icons/icon-192.png",
      "sizes": "192x192",
      "type": "image/png"
    },
    {
      "src": "/icons/icon-512.png",
      "sizes": "512x512",
      "type": "image/png"
    }
  ]
}
```

## Data Models

### New/Modified Models

**NotificationModel**:
```csharp
public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    
    // Navigation
    public virtual AppUser User { get; set; }
}
```

**SearchPreference**:
```csharp
public class SearchPreference
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string PreferencesJson { get; set; }
    public DateTime LastUpdated { get; set; }
    
    // Navigation
    public virtual AppUser User { get; set; }
}
```

**AIConversation**:
```csharp
public class AIConversation
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string SessionId { get; set; }
    public string Message { get; set; }
    public string Response { get; set; }
    public string Intent { get; set; }
    public double ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public virtual AppUser User { get; set; }
}
```

**PushSubscription**:
```csharp
public class PushSubscription
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Endpoint { get; set; }
    public string P256dh { get; set; }
    public string Auth { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    
    // Navigation
    public virtual AppUser User { get; set; }
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*


### Property 1: Form Validation Feedback
*For any* form with validation rules, when invalid data is entered, the system should display validation feedback messages immediately without requiring form submission.
**Validates: Requirements 2.4**

### Property 2: Navigation Response Time
*For any* navigation action (clicking links, buttons), the system should provide visual feedback (loading state, transition) within 100ms of the user action.
**Validates: Requirements 2.8**

### Property 3: Responsive Layout Adaptation
*For any* viewport width (mobile: 320-767px, tablet: 768-1023px, desktop: 1024px+), the layout should adapt appropriately without horizontal scrolling or content overflow.
**Validates: Requirements 2.9**

### Property 4: AI Room Availability Accuracy
*For any* room availability query to the AI assistant, the returned information should match the current database state for room availability.
**Validates: Requirements 3.1**

### Property 5: AI Recommendation Matching
*For any* set of user preferences (price range, capacity, amenities), AI room recommendations should only include rooms that match all specified criteria.
**Validates: Requirements 3.2**

### Property 6: AI Conversation Context Preservation
*For any* conversation session, when multiple related messages are sent, the AI should reference information from previous messages in the same session.
**Validates: Requirements 3.5**

### Property 7: AI Escalation on Low Confidence
*For any* AI response with confidence score below 0.7, the system should automatically escalate to human support.
**Validates: Requirements 3.6**

### Property 8: AI Response Time
*For any* user query to the AI assistant, the system should return a response within 2 seconds.
**Validates: Requirements 3.7**

### Property 9: AI Feedback Logging
*For any* user feedback (positive/negative) on AI responses, the system should persist the feedback to the database with the associated message ID.
**Validates: Requirements 3.8**

### Property 10: Search Performance
*For any* search query with filters, the system should return results within 500ms.
**Validates: Requirements 4.1**

### Property 11: Multi-Filter Support
*For any* combination of filters (price, type, capacity, amenities, location), the system should support applying all filter types simultaneously.
**Validates: Requirements 4.2**

### Property 12: Filter AND Logic
*For any* set of applied filters, the search results should only include rooms that satisfy all filter conditions (AND logic, not OR).
**Validates: Requirements 4.3**

### Property 13: Filter Result Counts
*For any* filter option, the displayed count should match the actual number of rooms that would be returned if that filter were applied.
**Validates: Requirements 4.4**

### Property 14: Search Preference Persistence
*For any* user's saved search preferences, when the user returns in a new session, the preferences should be restored correctly.
**Validates: Requirements 4.6**

### Property 15: Empty Search Suggestions
*For any* search query that returns zero results, the system should provide at least one alternative suggestion.
**Validates: Requirements 4.7**

### Property 16: Sort Order Correctness
*For any* sort option (price, rating, popularity, availability), the results should be ordered correctly according to the selected criterion.
**Validates: Requirements 4.8**

### Property 17: Payment Failure Error Handling
*For any* failed payment attempt, the system should return a specific error message and provide a retry option.
**Validates: Requirements 5.4**

### Property 18: Automatic Refund Processing
*For any* cancelled booking with a completed payment, the system should automatically initiate a refund to the original payment method.
**Validates: Requirements 5.6**

### Property 19: Payment Confirmation Timing
*For any* successful payment, the system should send a confirmation email within 30 seconds.
**Validates: Requirements 5.7**

### Property 20: Booking Status Notifications
*For any* booking status change (confirmed, cancelled, modified), the system should send a notification to the associated user.
**Validates: Requirements 6.1**

### Property 21: Price Drop Notifications
*For any* watched room where the price decreases, the system should send a notification to all users watching that room.
**Validates: Requirements 6.3**

### Property 22: Critical Event Email Notifications
*For any* critical event (booking confirmation, cancellation, payment received), the system should send an email notification.
**Validates: Requirements 6.4**

### Property 23: Unread Notification Badge Accuracy
*For any* user, the notification badge count should equal the number of unread notifications for that user.
**Validates: Requirements 6.5**

### Property 24: Notification Preference Respect
*For any* user with disabled notification preferences for a specific type, the system should not send notifications of that type.
**Validates: Requirements 6.6**

### Property 25: Notification Timezone Conversion
*For any* notification with a timestamp, the displayed time should be converted to the user's configured timezone.
**Validates: Requirements 6.7**

### Property 26: Occupancy Rate Calculation
*For any* time period, the displayed occupancy rate should equal (booked rooms / total rooms) × 100 for that period.
**Validates: Requirements 7.1**

### Property 27: Bulk Room Operations
*For any* bulk operation (update status, change price) on a set of rooms, all selected rooms should be updated atomically.
**Validates: Requirements 7.4**

### Property 28: Booking Trend Accuracy
*For any* date range, the booking trend data should match the actual booking counts from the database for that range.
**Validates: Requirements 7.5**

### Property 29: Audit Log Completeness
*For any* admin action (create, update, delete), the system should create an audit log entry with user, action, timestamp, and affected entity.
**Validates: Requirements 7.6**

### Property 30: Role-Based Access Control
*For any* admin feature endpoint, the system should only allow access to users with the appropriate role.
**Validates: Requirements 7.7**

### Property 31: Error Handling Structure
*For any* exception thrown in the application, the system should catch it, log it with appropriate severity, and return a user-friendly error response.
**Validates: Requirements 9.5**

### Property 32: Logging Severity Levels
*For any* log message, the system should use the appropriate severity level (Debug, Info, Warning, Error, Critical) based on the event type.
**Validates: Requirements 9.6**

### Property 33: Input Validation at Controller
*For any* controller action with input parameters, the system should validate the input and return 400 Bad Request for invalid data before processing.
**Validates: Requirements 9.8**

### Property 34: Sensitive Data Encryption
*For any* sensitive data field (payment info, personal data), the stored value in the database should be encrypted.
**Validates: Requirements 10.2**

### Property 35: Rate Limiting Enforcement
*For any* API endpoint, when the rate limit is exceeded, the system should return HTTP 429 Too Many Requests.
**Validates: Requirements 10.3**

### Property 36: Suspicious Activity Alerting
*For any* detected suspicious activity pattern (multiple failed logins, unusual access patterns), the system should create an alert log entry.
**Validates: Requirements 10.4**

### Property 37: CSRF Token Validation
*For any* state-changing HTTP request (POST, PUT, DELETE), the system should validate the anti-forgery token and reject requests without valid tokens.
**Validates: Requirements 10.6**

### Property 38: File Upload Validation
*For any* file upload, the system should validate the file extension and MIME type against an allowlist and reject disallowed types.
**Validates: Requirements 10.7**

### Property 39: Homepage Load Performance
*For any* homepage load, the First Contentful Paint should occur within 1.5 seconds on a standard connection.
**Validates: Requirements 12.1**

### Property 40: List Pagination
*For any* list with more than 50 items, the system should implement pagination or virtual scrolling rather than rendering all items.
**Validates: Requirements 12.7**

## Error Handling

### Error Handling Strategy

The application implements a comprehensive error handling strategy across all layers:

**1. Controller Level**
- Model validation errors return 400 Bad Request with detailed validation messages
- Authorization failures return 403 Forbidden
- Not found resources return 404 Not Found
- Unhandled exceptions are caught by global exception handler

**2. Service Level**
- Business logic exceptions are wrapped in custom exception types
- Transient failures (database timeouts, network issues) trigger automatic retry with exponential backoff
- All exceptions are logged with context (user ID, operation, parameters)

**3. Data Access Level**
- Database connection failures are retried up to 3 times
- Concurrency conflicts are detected and handled gracefully
- SQL exceptions are logged with query context (sanitized)

**4. External Service Integration**
- API call failures are wrapped with circuit breaker pattern
- Timeout values are configured per service
- Fallback responses are provided when external services are unavailable

**5. Client-Side Error Handling**
- JavaScript errors are caught and logged to server
- Network failures show user-friendly retry options
- Form validation errors are displayed inline

### Error Response Format

All API errors follow a consistent format:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more validation errors occurred.",
    "details": [
      {
        "field": "email",
        "message": "Email address is required."
      }
    ],
    "traceId": "00-abc123-def456-00"
  }
}
```

### Logging Strategy

**Log Levels**:
- **Debug**: Detailed diagnostic information for development
- **Information**: General application flow (user logged in, booking created)
- **Warning**: Unexpected but handled situations (rate limit approached, slow query)
- **Error**: Errors that prevent operation completion (payment failed, database error)
- **Critical**: System-wide failures requiring immediate attention (database unavailable, security breach)

**Structured Logging**:
```csharp
_logger.LogInformation(
    "Booking created: {BookingId} for User: {UserId}, Room: {RoomId}, CheckIn: {CheckIn}",
    booking.Id, userId, roomId, checkIn);
```

**Sensitive Data Redaction**:
- Passwords are never logged
- Credit card numbers are masked (showing only last 4 digits)
- Personal information is redacted in production logs

## Testing Strategy

### Testing Pyramid

The testing strategy follows the testing pyramid approach:

```
        /\
       /  \
      / E2E \
     /--------\
    /          \
   / Integration \
  /--------------\
 /                \
/   Unit Tests     \
--------------------
```

### Unit Testing

**Framework**: xUnit with Moq for mocking

**Coverage Target**: 80% code coverage for service layer

**Test Categories**:
1. **Service Layer Tests**: Test business logic in isolation
2. **Validation Tests**: Test model validation rules
3. **Utility Tests**: Test helper methods and extensions

**Example Unit Test**:
```csharp
[Fact]
public async Task SearchRoomsAsync_WithPriceFilter_ReturnsMatchingRooms()
{
    // Arrange
    var mockRepo = new Mock<IGenericRepository<Room>>();
    var rooms = new List<Room>
    {
        new Room { Id = 1, RoomType = new RoomType { PricePerNight = 100 } },
        new Room { Id = 2, RoomType = new RoomType { PricePerNight = 200 } },
        new Room { Id = 3, RoomType = new RoomType { PricePerNight = 300 } }
    };
    mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
    
    var service = new SmartSearchService(mockRepo.Object);
    var criteria = new SearchCriteria { MinPrice = 150, MaxPrice = 250 };
    
    // Act
    var result = await service.SearchRoomsAsync(criteria);
    
    // Assert
    Assert.Single(result.Rooms);
    Assert.Equal(2, result.Rooms[0].Id);
}
```

### Property-Based Testing

**Framework**: FsCheck for C#

**Purpose**: Validate properties that should hold for all inputs

**Configuration**: Minimum 100 iterations per property test

**Example Property Test**:
```csharp
[Property]
public Property SearchWithMultipleFilters_ReturnsOnlyMatchingRooms()
{
    return Prop.ForAll(
        Arb.Generate<SearchCriteria>(),
        Arb.Generate<List<Room>>(),
        (criteria, rooms) =>
        {
            // Setup
            var mockRepo = new Mock<IGenericRepository<Room>>();
            mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(rooms);
            var service = new SmartSearchService(mockRepo.Object);
            
            // Act
            var result = service.SearchRoomsAsync(criteria).Result;
            
            // Assert
            return result.Rooms.All(room =>
                MatchesCriteria(room, criteria));
        });
}
```

**Property Test Tags**:
Each property test must include a comment referencing the design property:
```csharp
// Feature: bookify-modernization-2026, Property 12: Filter AND Logic
[Property]
public Property FilterAndLogic_Test() { ... }
```

### Integration Testing

**Framework**: xUnit with WebApplicationFactory

**Purpose**: Test API endpoints with real database (in-memory or test container)

**Test Categories**:
1. **API Endpoint Tests**: Test HTTP requests/responses
2. **Database Integration Tests**: Test EF Core queries
3. **External Service Tests**: Test third-party integrations (with mocks)

**Example Integration Test**:
```csharp
public class RoomControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public RoomControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task SearchRooms_WithValidCriteria_ReturnsOk()
    {
        // Arrange
        var criteria = new { minPrice = 100, maxPrice = 200 };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/rooms/search", criteria);
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SearchResult>();
        Assert.NotNull(result);
    }
}
```

### End-to-End Testing

**Framework**: Playwright or Selenium

**Purpose**: Test critical user journeys in a real browser

**Test Scenarios**:
1. User registration and login
2. Room search and booking flow
3. Payment processing
4. Admin dashboard operations

**Example E2E Test**:
```csharp
[Test]
public async Task CompleteBookingFlow_Success()
{
    await Page.GotoAsync("https://localhost:5001");
    await Page.ClickAsync("text=Sign in");
    await Page.FillAsync("#email", "user@bookify.com");
    await Page.FillAsync("#password", "User@123456!");
    await Page.ClickAsync("button[type=submit]");
    
    await Page.ClickAsync("text=Find Rooms");
    await Page.ClickAsync(".room-card:first-child .book-button");
    await Page.FillAsync("#checkIn", "2026-06-01");
    await Page.FillAsync("#checkOut", "2026-06-05");
    await Page.ClickAsync("text=Continue to Payment");
    
    // Fill payment details
    await Page.FillAsync("#cardNumber", "4242424242424242");
    await Page.FillAsync("#expiry", "12/28");
    await Page.FillAsync("#cvc", "123");
    await Page.ClickAsync("text=Complete Booking");
    
    // Verify success
    await Expect(Page.Locator("text=Booking Confirmed")).ToBeVisibleAsync();
}
```

### Performance Testing

**Framework**: NBomber or k6

**Purpose**: Validate performance requirements

**Test Scenarios**:
1. Homepage load time (FCP < 1.5s)
2. Search response time (< 500ms)
3. AI response time (< 2s)
4. Concurrent user load (100+ simultaneous users)

**Example Performance Test**:
```csharp
var scenario = Scenario.Create("search_rooms", async context =>
{
    var response = await Http.CreateRequest("POST", "https://localhost:5001/api/rooms/search")
        .WithJsonBody(new { minPrice = 100, maxPrice = 300 })
        .WithCheck(response => Task.FromResult(response.IsSuccessStatusCode))
        .ExecuteAsync();
    
    return response;
})
.WithLoadSimulations(
    Simulation.KeepConstant(copies: 50, during: TimeSpan.FromMinutes(1))
);

NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFormats(ReportFormat.Html)
    .Run();
```

### Continuous Integration

**CI Pipeline**:
1. Build application
2. Run unit tests (fail if coverage < 80%)
3. Run integration tests
4. Run security scans (OWASP dependency check)
5. Run code quality analysis (SonarQube)
6. Build Docker image
7. Deploy to staging environment
8. Run E2E tests against staging
9. Run performance tests
10. Deploy to production (if all tests pass)

**Test Execution**:
- Unit tests: Run on every commit
- Integration tests: Run on every commit
- E2E tests: Run on pull requests and before deployment
- Performance tests: Run nightly and before major releases

### Test Data Management

**Strategy**:
- Use in-memory database for unit tests
- Use Docker test containers for integration tests
- Use seed data scripts for E2E tests
- Reset database state between test runs

**Test Data Builders**:
```csharp
public class RoomBuilder
{
    private Room _room = new Room();
    
    public RoomBuilder WithRoomNumber(int number)
    {
        _room.RoomNumber = number;
        return this;
    }
    
    public RoomBuilder WithPrice(decimal price)
    {
        _room.RoomType = new RoomType { PricePerNight = price };
        return this;
    }
    
    public Room Build() => _room;
}

// Usage
var room = new RoomBuilder()
    .WithRoomNumber(101)
    .WithPrice(150)
    .Build();
```

## Implementation Notes

### Phase 1: Critical Fixes (Week 1)
- Fix Razor syntax errors in Edit.cshtml and Create.cshtml
- Verify build succeeds
- Deploy hotfix to production

### Phase 2: UI Modernization (Weeks 2-4)
- Implement design system with new color palette
- Create reusable UI components
- Implement dark mode
- Update all views to use new design system
- Ensure responsive design across all pages

### Phase 3: Feature Enhancements (Weeks 5-8)
- Enhance AI assistant with NLP capabilities
- Implement advanced search and filtering
- Upgrade payment integration to Stripe Payment Element
- Implement real-time notifications with SignalR
- Enhance admin dashboard with analytics

### Phase 4: PWA and Performance (Weeks 9-10)
- Implement service worker and PWA manifest
- Optimize database queries and add indexes
- Implement image optimization and lazy loading
- Add response compression and caching
- Achieve Lighthouse score > 90

### Phase 5: Testing and Quality (Weeks 11-12)
- Write unit tests for all services
- Implement property-based tests for critical paths
- Write integration tests for all endpoints
- Create E2E tests for user journeys
- Achieve 80% code coverage

### Technology Stack Updates

**Frontend**:
- Tailwind CSS 3.4+ (already in use, update configuration)
- Alpine.js 3.x (already in use)
- Chart.js 4.x (for analytics)
- Workbox 7.x (for service worker)

**Backend**:
- .NET 9.0 (already in use)
- Entity Framework Core 9.0 (already in use)
- SignalR (for real-time notifications)
- Stripe.net SDK (latest version)
- Azure OpenAI SDK (for AI assistant)

**Testing**:
- xUnit (unit and integration tests)
- FsCheck (property-based testing)
- Playwright (E2E testing)
- NBomber (performance testing)

**DevOps**:
- GitHub Actions (CI/CD)
- Docker (containerization)
- SonarQube (code quality)
- OWASP Dependency Check (security scanning)
