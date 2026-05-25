-- ============================================================
-- BOOKIFY HOTEL — DATABASE-LEVEL FIXES
-- Run against BookifyHotelDb
-- ============================================================

-- ─── FIX 1: Performance indexes ──────────────────────────────
-- Speeds up date-conflict queries and booking lookups

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bookings_RoomId_Dates_Status')
    CREATE INDEX IX_Bookings_RoomId_Dates_Status
        ON Bookings (RoomId, CheckInDate, CheckOutDate, Status)
        INCLUDE (UserProfileId, TotalPrice);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bookings_UserProfileId_Status')
    CREATE INDEX IX_Bookings_UserProfileId_Status
        ON Bookings (UserProfileId, Status)
        INCLUDE (CheckInDate, CheckOutDate, TotalPrice);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Rooms_Status_IsAvailable')
    CREATE INDEX IX_Rooms_Status_IsAvailable
        ON Rooms (Status, IsAvailable)
        INCLUDE (RoomTypeId, Floor);

-- ─── FIX 2: CHECK constraints ────────────────────────────────
-- Prevent bad data at the database level

-- Check-out must be after check-in
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Bookings_CheckOutAfterCheckIn'
)
    ALTER TABLE Bookings
        ADD CONSTRAINT CK_Bookings_CheckOutAfterCheckIn
        CHECK (CheckOutDate > CheckInDate);

-- TotalPrice must be positive
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Bookings_PositiveTotalPrice'
)
    ALTER TABLE Bookings
        ADD CONSTRAINT CK_Bookings_PositiveTotalPrice
        CHECK (TotalPrice > 0);

-- Status must be one of the valid values
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Bookings_ValidStatus'
)
    ALTER TABLE Bookings
        ADD CONSTRAINT CK_Bookings_ValidStatus
        CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled', 'Completed'));

-- NumberOfGuests must be positive
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Bookings_PositiveGuests'
)
    ALTER TABLE Bookings
        ADD CONSTRAINT CK_Bookings_PositiveGuests
        CHECK (NumberOfGuests >= 1);

-- Room status must be valid
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Rooms_ValidStatus'
)
    ALTER TABLE Rooms
        ADD CONSTRAINT CK_Rooms_ValidStatus
        CHECK (Status IN ('Available', 'Booked', 'Maintenance', 'OutOfService'));

-- PricePerNight must be positive
IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_RoomTypes_PositivePrice'
)
    ALTER TABLE RoomTypes
        ADD CONSTRAINT CK_RoomTypes_PositivePrice
        CHECK (PricePerNight > 0);

-- ─── FIX 3: Fix existing lowercase status values ─────────────
-- Normalize any "pending"/"confirmed" to PascalCase

UPDATE Bookings SET Status = 'Pending'   WHERE Status = 'pending';
UPDATE Bookings SET Status = 'Confirmed' WHERE Status = 'confirmed';
UPDATE Bookings SET Status = 'Cancelled' WHERE Status = 'cancelled';
UPDATE Bookings SET Status = 'Completed' WHERE Status = 'completed';

-- ─── FIX 4: Monitoring queries ───────────────────────────────
-- Run these periodically to detect anomalies

-- Detect overlapping confirmed bookings (should return 0 rows)
SELECT
    a.BookingId AS Booking1,
    b.BookingId AS Booking2,
    a.RoomId,
    a.CheckInDate  AS A_CheckIn,
    a.CheckOutDate AS A_CheckOut,
    b.CheckInDate  AS B_CheckIn,
    b.CheckOutDate AS B_CheckOut
FROM Bookings a
JOIN Bookings b ON a.RoomId = b.RoomId
    AND a.BookingId < b.BookingId
    AND a.Status = 'Confirmed'
    AND b.Status = 'Confirmed'
    AND (
        (a.CheckInDate  >= b.CheckInDate  AND a.CheckInDate  < b.CheckOutDate) OR
        (a.CheckOutDate >  b.CheckInDate  AND a.CheckOutDate <= b.CheckOutDate) OR
        (a.CheckInDate  <= b.CheckInDate  AND a.CheckOutDate >= b.CheckOutDate)
    );

-- Detect bookings with check-out <= check-in (should return 0 rows)
SELECT BookingId, CheckInDate, CheckOutDate, Status
FROM Bookings
WHERE CheckOutDate <= CheckInDate;

-- Detect rooms marked Booked with no active confirmed booking
SELECT r.RoomId, r.RoomNumber, r.Status
FROM Rooms r
WHERE r.Status = 'Booked'
  AND NOT EXISTS (
      SELECT 1 FROM Bookings b
      WHERE b.RoomId = r.RoomId
        AND b.Status = 'Confirmed'
        AND b.CheckInDate <= GETDATE()
        AND b.CheckOutDate >= GETDATE()
  );

-- Detect orphan bookings (no matching UserProfile)
SELECT b.BookingId, b.UserProfileId
FROM Bookings b
LEFT JOIN UserProfiles u ON b.UserProfileId = u.Id
WHERE u.Id IS NULL;

-- Revenue summary
SELECT
    Status,
    COUNT(*)        AS BookingCount,
    SUM(TotalPrice) AS TotalRevenue,
    AVG(TotalPrice) AS AvgRevenue
FROM Bookings
GROUP BY Status
ORDER BY TotalRevenue DESC;
