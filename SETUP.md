# Setup Guide

This walks through everything needed to run the experiment: registering an Azure
AD application, configuring the project, and publishing to GitHub.

## 1. Register an Azure AD application

The Graph API needs an app registration so it can issue you a sign-in token.

1. Go to the **Azure Portal** → search for **App registrations** →
   https://portal.azure.com (sign in with your Microsoft account).
2. Click **New registration**.
3. Fill in:
   - **Name:** `OneDriveIntegrityLab`
   - **Supported account types:** select
     **"Personal Microsoft accounts only"** (or "Accounts in any org directory and
     personal Microsoft accounts" if you also want work accounts to work).
   - **Redirect URI:** leave blank — device code flow does not need one.
4. Click **Register**.
5. On the app's **Overview** page, copy the **Application (client) ID** — this is
   your `ClientId`.

### Allow public client / device code flow

1. In the app, go to **Authentication**.
2. Scroll to **Advanced settings** → **Allow public client flows** → set to **Yes**.
3. Click **Save**.

### Add the Graph permissions

1. Go to **API permissions** → **Add a permission** → **Microsoft Graph** →
   **Delegated permissions**.
2. Add: **Files.ReadWrite** and **User.Read**.
3. (Personal accounts grant consent at sign-in, so no admin consent is needed.)

## 2. Configure the project

Set your client ID using an environment variable (keeps it out of source control):

```bash
# macOS / Linux
export ONEDRIVE_CLIENT_ID="paste-your-client-id-here"

# Windows (PowerShell)
$env:ONEDRIVE_CLIENT_ID="paste-your-client-id-here"
```

Or edit `appsettings.json` and replace `YOUR_CLIENT_ID_HERE` (note: don't commit a
real ID — `appsettings.local.json` is gitignored if you prefer that route).

## 3. Run it

```bash
dotnet restore
dotnet run
```

Follow the device-code prompt to sign in. You should see a results table and a
`LAB_NOTES_run.txt` file appear.

## 4. Publish to GitHub

```bash
# From inside the project folder
git init
git branch -M main

# Create the repo on GitHub first (via github.com → New repository,
# name it "onedrive-integrity-lab", leave it empty), then:
git remote add origin https://github.com/<your-username>/onedrive-integrity-lab.git
```

Then follow the staged commit plan in `COMMIT_PLAN.md` so the history reflects your
build process step by step, and finally:

```bash
git push -u origin main
```
