# Credentials Folder

## 📁 What Goes Here

This folder is for storing sensitive credential files that should **NEVER** be committed to git.

## 🔐 Required Files

### 1. Google Cloud Credentials
**Filename**: `google-cloud-credentials.json`

Save your Google Cloud service account JSON file here.

**Structure**:
```
credentials/
├── google-cloud-credentials.json  ← Your Google Cloud service account key
└── README.md                      ← This file
```

## ⚠️ Security Notes

- ✅ This folder is in `.gitignore`
- ✅ Files here will NOT be committed to git
- ✅ Each developer should have their own credentials
- ❌ NEVER share these files publicly
- ❌ NEVER commit these files to version control

## 📝 How to Get Your Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Navigate to: IAM & Admin → Service Accounts
3. Select your service account
4. Click "Keys" tab
5. Click "Add Key" → "Create new key"
6. Choose JSON format
7. Save the downloaded file as `google-cloud-credentials.json` in this folder

## 🔧 Configuration

After saving your credentials file, update:
- `appsettings.Development.json` with your project ID
- Verify the path in configuration matches this location

## 🚀 Quick Start

```bash
# 1. Save your credentials file here
cp ~/Downloads/your-key.json ./google-cloud-credentials.json

# 2. Verify it's here
ls -la google-cloud-credentials.json

# 3. Run the application
cd ..
dotnet run
```

## 🆘 Need Help?

See `GOOGLE_CLOUD_SETUP.md` in the project root for detailed setup instructions.
