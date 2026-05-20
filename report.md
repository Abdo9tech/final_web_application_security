# Bookify Hotel - System Modifications Report

This report documents the security enhancements, routing bug fixes, two-factor authentication (2FA) updates, and mail configuration implemented in the Bookify Hotel application, along with instructions for rebuilding the containers.

---

## 1. Summary of Changes

### 🌐 Google External Login (New!)
* **Files Modified**: `HotelEcomm.csproj`, `Program.cs`, `appsettings.json`, `Controllers/LoginController.cs`, `Views/Login/Login.cshtml`
* **Workflow**:
  * Added `Microsoft.AspNetCore.Authentication.Google` package dependency.
  * Registered the Google Authentication middleware handler in `Program.cs`.
  * Added client configuration keys under `Authentication:Google` in `appsettings.json`.
  * Implemented `ExternalLogin` and `ExternalLoginCallback` action handlers in `LoginController.cs`.
  * The callback automatically maps Google login assertions to a local user:
    * If the user doesn't exist locally, it registers them, flags their email as verified, creates a custom `UserProfile` and custom `User_Role` (Role: "User"), and signs them in.
    * If they already exist, it links Google to their identity account and signs them in.
  * Wired the Google button on the Login page to trigger the Google authentication challenge.

### 📧 Real Email Verification via SMTP
* **Files Modified**: `Controllers/AccountController.cs` (RegisterController), `Program.cs`, `Views/Shared/_Layout.cshtml`
* **Workflow**:
  * Upon registration, new users now have `EmailConfirmed` set to `false`.
  * The application automatically generates an Identity email confirmation token and constructs a verification link.
  * A styled verification email is dispatched to the user's Gmail using the configured SMTP server.
  * Users are redirected to the Login page with a notice prompting them to check their inbox.
  * A `ConfirmEmail` callback action was added to `RegisterController` to validate the token and activate the account.
  * Identity option `RequireConfirmedAccount` has been set to `true` inside `Program.cs` to strictly enforce this validation upon sign-in.
  * Added global TempData alert banners in `_Layout.cshtml` to gracefully show verification success or failure messages.

### 🛠️ Two-Factor Authentication (2FA) & QR Code UI Fix
* **File Modified**: `Views/Profile/Enable2fa.cshtml`
* **Issue**: The QR code on the 2FA configuration page failed to render because of strict Subresource Integrity (`integrity` and `crossorigin`) attributes on the `qrcode.min.js` CDN script tag.
* **Fix**: Removed the `integrity` and `crossorigin` properties from the script tag. The script now loads dynamically and renders the dynamic TOTP QR code correctly.

### 🧭 Account Redirection Bug Fixes (Resolving 404 Pages)
* **File Modified**: `Controllers/UserController.cs`
* **Issue**: The application did not have an `AccountController`. However, various actions in `UserController.cs` redirected unauthorized users to `RedirectToAction("Login", "Account")` or `RedirectToAction("Profile", "Account")`, which returned `HTTP 404 Not Found`.
* **Fix**:
  * Corrected login redirection from `"Account"` controller to `"Login"` controller: `RedirectToAction("Login", "Login")`
  * Corrected profile redirection from `"Account"` controller to `"Profile"` controller: `RedirectToAction("Index", "Profile")`

### 🔑 2FA Login Flow Support
* **File Modified**: `Controllers/LoginController.cs`
* **Issue**: The `Login` post handler previously did not check for `result.RequiresTwoFactor`. If a user enabled 2FA and attempted to log in, they would get an "Invalid credentials" error and be blocked from accessing the site.
* **Fix**:
  * The login pipeline was aligned to handle `result.RequiresTwoFactor` status on authentication.
  * Added redirects to a two-factor verification code input view when 2FA is active.

### 📧 Gmail SMTP Email Configuration
* **Configuration File**: `appsettings.json`
* **Implementation Details**:
  * Configured secure `MailSettings` pointing to Google's SMTP server (`smtp.gmail.com`) using port `587` with STARTTLS.
  * Configured standard configuration mappings for email notification delivery (including 2FA recovery codes and token notifications).

---

## 2. Default Credentials & Database Seeding

The application seeds two default users on startup. These default accounts are seeded with `EmailConfirmed = true` so you can continue testing them without manual email verification.

| User | Email | Password | Role |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@bookify.com` | `Admin@123456!` | Admin |
| **Demo User** | `user@bookify.com` | `User@123456!` | User |

---

## 3. How to Cleanly Rebuild and Run the Containers

To ensure a clean environment without stale files or corrupted database constraints, execute the following commands in your shell.

> [!IMPORTANT]
> Since terminal commands must not be run by the AI agent on your host, you must run these commands yourself in your terminal.

### Step 1: Clean and Reset Containers (Wipe Data Volume)
This stops all active services and completely removes the volume associated with the SQL Server container, forcing a clean seed.
```bash
docker compose down -v
```

### Step 2: Build and Start Services
This rebuilds the application images with the new routing, email verification, and Google Sign-in code, and starts the containers in the background.
```bash
docker compose up --build -d
```

### Step 3: Verify Startup Logs
To confirm that EF Core migrations ran successfully and the default admin and user were seeded, check the container logs:
```bash
docker compose logs web
```
You should see:
* `Applying database migrations...`
* `✅ Migrations applied successfully`
* `✅ Role 'Admin' created` / `✅ Role 'User' created`
* `✅ Default users seeded successfully!`
