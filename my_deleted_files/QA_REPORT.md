# Bookify Hotel — QA Audit Report
Generated: 2026-05-24

---

## CRITICAL (must fix before production)

### C1 — EF Core LINQ translation failure on BookingStatus helpers
**File**: BookingService.cs, BookingController.cs, BookNowController.cs  
**What**: `BookingStatus.IsConfirmed(b.Status)` inside `.AnyAsync()` / `.Where()` throws at runtime  
**Why**: EF Core cannot translate arbitrary C# methods to SQL  
**Fix**: Replace with inline string literals inside all EF queries (done in code fixes below)

### C2 — Booking.IsActive / CanBeCancelled use lowercase status
**File**: DAL/Model/Booking.cs  
**What**: `Status == "confirmed"` but the app writes `"Confirmed"` (PascalCase)  
**Fix**: Use `StringComparison.OrdinalIgnoreCase` or constants

### C3 — UpdateRoomStatus in BookingController still uses BookingStatus helpers inside AnyAsync
**File**: BookingController.cs line ~520  
**What**: `b.Status != BookingStatus.Cancelled` inside EF query — same translation failure  
**Fix**: Replace with string literals

### C4 — CancelBooking in BookNowController uses BookingStatus.IsConfirmed inside AnyAsync
**File**: BookNowController.cs  
**What**: Same EF translation issue  
**Fix**: Replace with `b.Status == "Confirmed"`

### C5 — BookingService.IsRoomAvailableForDatesAsync uses BookingStatus.IsConfirmed inside AnyAsync
**File**: PLL/Services/BookingService.cs  
**Fix**: Replace with `b.Status == "Confirmed"`

---

## HIGH

### H1 — No database-level constraint preventing duplicate bookings
**What**: Two concurrent requests can both pass the AnyAsync check and create overlapping bookings  
**Fix**: Add a unique filtered index + CHECK constraint in SQL (see SQL fixes below)

### H2 — Booking.Status default is "pending" (lowercase) but code writes "Pending" (PascalCase)
**File**: DAL/Model/Booking.cs  
**What**: `Status = "pending"` default vs `BookingStatus.Pending = "Pending"` constant  
**Impact**: `IsActive` computed property always returns false for new bookings  
**Fix**: Change default to `"Pending"` to match the constant

### H3 — BookingController.UpdateRoomStatus uses BookingStatus helpers inside EF query
**File**: BookingController.cs  
**What**: `b.Status != BookingStatus.Cancelled` inside `.AnyAsync()` — EF translation failure  
**Fix**: Use raw string `"Cancelled"` and `"Completed"`

### H4 — Admin BookingController.Create conflict check uses BookingStatus.IsConfirmedOrCompleted inside EF
**File**: BookingController.cs  
**What**: Same EF translation issue  
**Fix**: Replace with `(b.Status == "Confirmed" || b.Status == "Completed")`

### H5 — No pagination on Booking.Index (admin)
**What**: `_bookingService.GetAll()` loads ALL bookings into memory  
**Impact**: Crashes with large datasets  
**Fix**: Add `.Skip().Take()` with page parameter

### H6 — N+1 query on Booking.Index
**What**: `GetAll()` returns Bookings without Include — lazy loading fires per row  
**Fix**: Use direct context query with `.Include()`

---

## MEDIUM

### M1 — Room.RoomTypes alias property causes confusion
**File**: DAL/Model/Room.cs  
**What**: `public virtual RoomType RoomTypes => RoomType;` — plural alias for singular nav property  
**Impact**: Developers use wrong name, causes Include errors  
**Fix**: Remove the alias, fix all callers to use `RoomType`

### M2 — Booking.UserId orphan field
**File**: DAL/Model/Booking.cs  
**What**: `public int UserId { get; set; }` — unused compatibility field, not mapped to UserProfile  
**Impact**: Confusion, potential data inconsistency  
**Fix**: Remove or map properly

### M3 — No max booking duration check
**What**: A user can book a room for 10 years  
**Fix**: Add `(checkOut - checkIn).Days > 365` validation (already in BookingService.ValidateBookingDates but not called from BookNowController)

### M4 — BookingController.Create doesn't validate dates
**What**: Admin can create a booking with check-out before check-in  
**Fix**: Add date validation in Create POST

### M5 — SpecialRequests has no length limit
**What**: No `[MaxLength]` attribute — could store megabytes  
**Fix**: Add `[MaxLength(1000)]`

### M6 — No index on Bookings(RoomId, CheckInDate, CheckOutDate)
**What**: Date conflict queries do full table scans  
**Fix**: Add composite index (see SQL below)

---

## LOW

### L1 — Booking.NumberOfNights computed property uses TotalDays (decimal) cast to int
**What**: `(int)(CheckOutDate - CheckInDate).TotalDays` — truncates, not rounds  
**Fix**: Use `.Days` instead of `(int).TotalDays`

### L2 — BookingController.Delete is GET not POST
**What**: `public async Task<IActionResult> Delete(int id)` — no `[HttpPost]`  
**Impact**: Bookings can be deleted via a crafted link (CSRF)  
**Fix**: Add `[HttpPost][ValidateAntiForgeryToken]`

### L3 — Room.ImageUrl has no validation
**What**: No URL format validation  
**Fix**: Add `[Url]` attribute or validate in service

---

## BUG REPORT SUMMARY

| ID | Priority | Area | Status |
|----|----------|------|--------|
| C1 | Critical | EF LINQ translation | Fixed in code |
| C2 | Critical | Model status casing | Fixed in code |
| C3 | Critical | EF LINQ translation | Fixed in code |
| C4 | Critical | EF LINQ translation | Fixed in code |
| C5 | Critical | EF LINQ translation | Fixed in code |
| H1 | High | DB constraint | SQL fix provided |
| H2 | High | Status casing | Fixed in code |
| H3 | High | EF LINQ translation | Fixed in code |
| H4 | High | EF LINQ translation | Fixed in code |
| H5 | High | Performance | Fix provided |
| H6 | High | N+1 query | Fixed in code |
| M1 | Medium | Model design | Fix provided |
| M2 | Medium | Model design | Fix provided |
| M3 | Medium | Validation | Fix provided |
| M4 | Medium | Validation | Fix provided |
| M5 | Medium | Validation | Fix provided |
| M6 | Medium | Performance | SQL fix provided |
| L1 | Low | Computed property | Fixed in code |
| L2 | Low | Security/CSRF | Fix provided |
| L3 | Low | Validation | Fix provided |
