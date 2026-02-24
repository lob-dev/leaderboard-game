# Publishing Guide — Leaderboard Game

## Overview

This document covers everything needed to publish the Leaderboard Game to **Google Play Store** and **Apple App Store**.

---

## Prerequisites

### General
- Unity 2022 LTS (or Unity 6) installed with **Android Build Support** and **iOS Build Support** modules
- Git for version control

### Google Play Store
- [Google Play Developer account](https://play.google.com/console) ($25 one-time fee)
- Java/JDK installed (for keystore generation)
- Android keystore file (see [Signing Setup](#android-signing-setup))

### Apple App Store
- [Apple Developer Program membership](https://developer.apple.com/programs/) ($99/year)
- macOS machine with Xcode 14+ installed
- Apple Developer Team ID configured in Unity
- Signing certificate + provisioning profile

---

## App Identity

| Field | Value |
|---|---|
| **App Name** | Leaderboard Game |
| **Bundle ID (Android)** | `com.leaderboardgame.app` |
| **Bundle ID (iOS)** | `com.leaderboardgame.app` |
| **Version** | 1.0.0 |
| **Build Number** | 1 |
| **Min Android SDK** | API 25 (Android 7.1) |
| **Min iOS Version** | 15.0 |
| **Category** | Games — Casual |
| **Content Rating** | Everyone / 4+ |

---

## Android Signing Setup

### 1. Generate a Keystore

```bash
keytool -genkey -v -keystore leaderboard-game.keystore \
  -alias leaderboard-game \
  -keyalg RSA -keysize 2048 -validity 10000 \
  -storepass <YOUR_STORE_PASSWORD> \
  -keypass <YOUR_KEY_PASSWORD> \
  -dname "CN=Leaderboard Game, O=LeaderboardGame, L=Unknown, ST=Unknown, C=US"
```

Place the generated `leaderboard-game.keystore` file in `Build/Signing/` (this directory is gitignored — **never commit keystores**).

### 2. Configure in Unity
1. **Edit → Project Settings → Player → Android**
2. Under **Publishing Settings**:
   - Check **Custom Keystore**
   - Browse to `Build/Signing/leaderboard-game.keystore`
   - Enter alias: `leaderboard-game`
   - Enter passwords
3. Under **Other Settings**:
   - Package Name: `com.leaderboardgame.app`
   - Version: `1.0.0`
   - Bundle Version Code: `1`
   - Minimum API Level: `25`
   - Target API Level: `Automatic (highest installed)`
   - Scripting Backend: **IL2CPP**
   - Target Architectures: **ARM64** (required for Play Store)

### 3. Build AAB (Android App Bundle)
1. **File → Build Settings**
2. Switch to **Android**
3. Check **Build App Bundle (Google Play)**
4. Click **Build** → save as `Build/Android/leaderboard-game.aab`

---

## iOS Signing Setup

### 1. Configure in Unity (on macOS)
1. **Edit → Project Settings → Player → iOS**
2. Under **Other Settings**:
   - Bundle Identifier: `com.leaderboardgame.app`
   - Version: `1.0.0`
   - Build: `1`
   - Signing Team ID: *(your Apple Developer Team ID)*
   - Check **Automatically Sign**
   - Target minimum iOS Version: `15.0`
   - Scripting Backend: **IL2CPP**
   - Architecture: **ARM64**

### 2. Build Xcode Project
1. **File → Build Settings** → Switch to **iOS**
2. Click **Build** → choose output folder `Build/iOS/`
3. Open the generated `.xcodeproj` in Xcode

### 3. Archive & Upload from Xcode
1. Select **Product → Archive**
2. In the Organizer, click **Distribute App**
3. Choose **App Store Connect** → **Upload**
4. Follow the prompts to upload to App Store Connect

---

## Store Listings

### Google Play Store Listing

See `store-listings/google-play/` for all assets.

| Field | Value |
|---|---|
| **Title** | Leaderboard Game |
| **Short Description** | The leaderboard IS the game. Tap to climb. Compete in real-time. |
| **Category** | Games → Casual |
| **Content Rating** | Everyone |
| **Contact Email** | *(your email)* |
| **Privacy Policy URL** | *(required — see privacy-policy.md)* |

### Apple App Store Listing

See `store-listings/apple-app-store/` for all assets.

| Field | Value |
|---|---|
| **Name** | Leaderboard Game |
| **Subtitle** | Tap. Climb. Compete. |
| **Category** | Games — Casual |
| **Age Rating** | 4+ |
| **Privacy Policy URL** | *(required)* |

---

## Publishing Checklist

### Before First Submission
- [ ] Generate and securely store Android keystore
- [ ] Set up Apple Developer account + certificates
- [ ] Configure bundle IDs in Unity Player Settings
- [ ] Set IL2CPP scripting backend for both platforms
- [ ] Create app icons (see `store-listings/` for specs)
- [ ] Write privacy policy and host it at a public URL
- [ ] Prepare store screenshots (see asset specs below)
- [ ] Fill in store listing descriptions
- [ ] Test on physical Android and iOS devices
- [ ] Set up Google Play App Signing (recommended)

### Android Release
- [ ] Build signed AAB
- [ ] Upload to Google Play Console
- [ ] Complete store listing, content rating questionnaire, pricing
- [ ] Submit for review (typically 1-3 days)

### iOS Release
- [ ] Build Xcode project from Unity on macOS
- [ ] Archive and upload to App Store Connect
- [ ] Complete App Store listing, screenshots, age rating
- [ ] Submit for review (typically 1-2 days)

---

## Screenshot Specifications

### Google Play
- **Phone**: 1080×1920 or 1920×1080 (min 320px, max 3840px per side)
- **Tablet 7"**: 1080×1920
- **Tablet 10"**: 1920×1200
- **Min 2, max 8** screenshots per device type
- Format: JPEG or PNG (24-bit, no alpha)

### Apple App Store
- **iPhone 6.7"** (1290×2796): Required
- **iPhone 6.5"** (1242×2688): Required
- **iPhone 5.5"** (1242×2208): Required
- **iPad Pro 12.9"** (2048×2732): Required if supporting iPad
- **Min 1, max 10** per device size
- Format: JPEG or PNG (no alpha)

---

## Keystore Security

⚠️ **CRITICAL**: The Android keystore is your permanent signing identity.

- **NEVER** commit it to git
- Back it up in multiple secure locations
- If lost, you cannot update your app — you'd need a new listing
- The `Build/Signing/` directory is gitignored for this reason
- Store passwords in a password manager, not in code

---

## CI/CD (Future)

For automated builds, consider:
- **Unity Build Server** or **Unity Cloud Build**
- **Fastlane** for iOS upload automation
- **GitHub Actions** with GameCI for Unity builds
- Store keystore/signing credentials as encrypted CI secrets
