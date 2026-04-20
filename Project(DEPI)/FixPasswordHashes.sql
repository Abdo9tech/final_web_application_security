-- Fix Password Hashes for Admin and Demo User
-- This script deletes and recreates users with proper password hashes

USE BookifyHotelDB;
GO

-- Delete existing users
DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Email IN ('admin@bookify.com', 'user@bookify.com'));
DELETE FROM UserProfiles WHERE IdentityUserId IN (SELECT Id FROM AspNetUsers WHERE Email IN ('admin@bookify.com', 'user@bookify.com'));
DELETE FROM AspNetUsers WHERE Email IN ('admin@bookify.com', 'user@bookify.com');

PRINT 'Old users deleted successfully';
GO

-- Note: The application will recreate users with proper password hashes on next startup
PRINT 'Database cleaned. Restart the application to recreate users with proper password hashes.';
GO
