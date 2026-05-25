# Google Cloud Credentials Setup Guide

## 🔐 Secure Configuration (Recommended)

This guide shows you how to properly configure Google Cloud credentials for your Bookify Hotel application.

---

## Step 1: Save Your Credentials File

1. **Copy your Google Cloud service account JSON** that you received
2. **Save it as**: `Project(DEPI)/credentials/google-cloud-credentials.json`
3. **Verify the file is in the right location**:
   ```
   Project(DEPI)/
   └── credentials/
       └── google-cloud-credentials.json  ← Your file here
   ```

### ⚠️ Important
- This file is already in `.gitignore` and will NOT be committed to git
- Keep this file secure and never share it publicly
- Each developer should have their own credentials file

---

## Step 2: Update Configuration

### For Local Development

Edit `Project(DEPI)/appsettings.Development.json`:

```json
{
  "GoogleCloud": {
    "ProjectId": "nifty-realm-470915-r8",
    "CredentialsPath": "./credentials/google-cloud-credentials.json"
  },
  "GoogleMaps": {
    "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "UseGoogleCloudAuth": true
  }
}
```

### For Docker Development

Create `docker-compose.override.yml`:

```yaml
version: '3.8'
services:
  web:
    environment:
      - GOOGLE_APPLICATION_CREDENTIALS=/app/credentials/google-cloud-credentials.json
      - GoogleCloud__ProjectId=nifty-realm-470915-r8
    volumes:
      - ./Project(DEPI)/credentials:/app/credentials:ro
```

---

## Step 3: Configure Application to Use Credentials

### Option A: Using Environment Variable (Recommended for Docker)

Add to `Program.cs` (before `builder.Build()`):

```csharp
// Configure Google Cloud credentials
var googleCredPath = builder.Configuration["GoogleCloud:CredentialsPath"];
if (!string.IsNullOrEmpty(googleCredPath) && File.Exists(googleCredPath))
{
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", googleCredPath);
    Console.WriteLine($"✅ Google Cloud credentials loaded from: {googleCredPath}");
}
else
{
    Console.WriteLine("⚠️ Google Cloud credentials not found. Some features may be disabled.");
}
```

### Option B: Using Configuration (For specific services)

For Google Maps or Gemini AI:

```csharp
// In Program.cs
builder.Services.Configure<GoogleCloudOptions>(
    builder.Configuration.GetSection("GoogleCloud"));

// Create options class
public class GoogleCloudOptions
{
    public string ProjectId { get; set; } = string.Empty;
    public string CredentialsPath { get; set; } = string.Empty;
}
```

---

## Step 4: Verify Setup

### Test Locally

```bash
# Check if file exists
ls Project(DEPI)/credentials/google-cloud-credentials.json

# Run application
dotnet run --project Project(DEPI)/HotelEcomm.csproj
```

### Test in Docker

```bash
# Build and run
docker-compose up -d

# Check logs
docker logs bookify-web | grep "Google Cloud"
```

You should see:
```
✅ Google Cloud credentials loaded from: ./credentials/google-cloud-credentials.json
```

---

## Step 5: Production Deployment

### For Azure App Service

1. Upload credentials to Azure Key Vault
2. Configure App Service to read from Key Vault:

```bash
az keyvault secret set \
  --vault-name your-keyvault \
  --name google-cloud-credentials \
  --file google-cloud-credentials.json
```

3. Update `appsettings.Production.json`:

```json
{
  "GoogleCloud": {
    "ProjectId": "nifty-realm-470915-r8",
    "UseKeyVault": true,
    "KeyVaultName": "your-keyvault",
    "SecretName": "google-cloud-credentials"
  }
}
```

### For AWS

Use AWS Secrets Manager:

```bash
aws secretsmanager create-secret \
  --name bookify/google-cloud-credentials \
  --secret-string file://google-cloud-credentials.json
```

### For Google Cloud Run

Use Secret Manager:

```bash
gcloud secrets create google-cloud-credentials \
  --data-file=google-cloud-credentials.json

gcloud run services update bookify-hotel \
  --update-secrets=GOOGLE_APPLICATION_CREDENTIALS=google-cloud-credentials:latest
```

---

## Security Checklist

- ✅ Credentials file is in `credentials/` folder
- ✅ `credentials/` folder is in `.gitignore`
- ✅ File permissions are restricted (chmod 600 on Linux/Mac)
- ✅ Different credentials for dev/staging/production
- ✅ Credentials are rotated regularly
- ✅ Service account has minimum required permissions
- ✅ Audit logs are enabled for the service account

---

## Troubleshooting

### Issue: "Credentials not found"

**Solution**: Check file path
```bash
# Verify file exists
ls -la Project(DEPI)/credentials/google-cloud-credentials.json

# Check appsettings.Development.json path matches
cat Project(DEPI)/appsettings.Development.json | grep CredentialsPath
```

### Issue: "Permission denied"

**Solution**: Fix file permissions
```bash
chmod 600 Project(DEPI)/credentials/google-cloud-credentials.json
```

### Issue: "Invalid credentials"

**Solution**: Verify JSON format
```bash
# Check if valid JSON
cat Project(DEPI)/credentials/google-cloud-credentials.json | jq .
```

### Issue: Docker can't find credentials

**Solution**: Check volume mount
```yaml
# In docker-compose.override.yml
volumes:
  - ./Project(DEPI)/credentials:/app/credentials:ro
```

---

## What Services Use These Credentials?

### 1. Google Maps API
- Location search
- Map display
- Geocoding

### 2. Gemini AI (Google AI)
- AI Assistant chatbot
- Smart search
- Recommendations

### 3. Google Cloud Storage (if enabled)
- Image uploads
- File storage
- Backups

---

## Quick Start Commands

```bash
# 1. Save your credentials
# Copy your JSON file to: Project(DEPI)/credentials/google-cloud-credentials.json

# 2. Update configuration
# Edit: Project(DEPI)/appsettings.Development.json
# Set ProjectId to: nifty-realm-470915-r8

# 3. Run application
dotnet run --project Project(DEPI)/HotelEcomm.csproj

# OR with Docker
docker-compose up -d
```

---

## Need Help?

If you encounter issues:

1. Check the application logs
2. Verify file paths
3. Ensure JSON is valid
4. Check service account permissions in Google Cloud Console

---

## Security Reminder

🔒 **NEVER commit credentials to git**  
🔒 **NEVER share credentials in chat or email**  
🔒 **ALWAYS use environment variables or secret managers in production**  
🔒 **ROTATE credentials regularly**

---

**Status**: Ready to configure  
**Next Step**: Save your credentials file to `Project(DEPI)/credentials/google-cloud-credentials.json`
