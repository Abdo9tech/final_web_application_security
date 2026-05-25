using System;

namespace DAL.Constants
{
    /// <summary>
    /// Constants and helper methods for booking status values
    /// Ensures consistent status handling across the application
    /// </summary>
    public static class BookingStatus
    {
        // Status Constants
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        /// <summary>
        /// Checks if a booking status is active (Confirmed or Pending)
        /// </summary>
        public static bool IsActive(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return status.Equals(Confirmed, StringComparison.OrdinalIgnoreCase) ||
                   status.Equals(Pending, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a booking status is confirmed or completed (for revenue calculations)
        /// </summary>
        public static bool IsConfirmedOrCompleted(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return status.Equals(Confirmed, StringComparison.OrdinalIgnoreCase) ||
                   status.Equals(Completed, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a booking is cancelled
        /// </summary>
        public static bool IsCancelled(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return status.Equals(Cancelled, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a booking is confirmed
        /// </summary>
        public static bool IsConfirmed(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return status.Equals(Confirmed, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a booking is pending
        /// </summary>
        public static bool IsPending(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return status.Equals(Pending, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if a booking is completed
        /// </summary>
        public static bool IsCompleted(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return status.Equals(Completed, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Normalizes status string to standard format
        /// </summary>
        public static string Normalize(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return Pending;

            if (status.Equals(Confirmed, StringComparison.OrdinalIgnoreCase))
                return Confirmed;
            if (status.Equals(Completed, StringComparison.OrdinalIgnoreCase))
                return Completed;
            if (status.Equals(Cancelled, StringComparison.OrdinalIgnoreCase))
                return Cancelled;
            if (status.Equals(Pending, StringComparison.OrdinalIgnoreCase))
                return Pending;

            return Pending; // Default
        }
    }

    /// <summary>
    /// Constants for room status values
    /// </summary>
    public static class RoomStatus
    {
        public const string Available = "Available";
        public const string Booked = "Booked";
        public const string Maintenance = "Maintenance";
        public const string OutOfService = "OutOfService";

        public static bool IsAvailable(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            return status.Equals(Available, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return Available;

            if (status.Equals(Available, StringComparison.OrdinalIgnoreCase))
                return Available;
            if (status.Equals(Booked, StringComparison.OrdinalIgnoreCase))
                return Booked;
            if (status.Equals(Maintenance, StringComparison.OrdinalIgnoreCase))
                return Maintenance;
            if (status.Equals(OutOfService, StringComparison.OrdinalIgnoreCase))
                return OutOfService;

            return Available; // Default
        }
    }
}
