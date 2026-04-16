# FileExplorer

A self-hosted web file explorer built with ASP.NET Core and vanilla JavaScript. Access, upload, download, rename, and edit files from any browser. Supports real-time file system updates via WebSocket and video streaming.

## Features

- Browse directories and files through a web UI
- Upload files with a progress bar — via button or drag-and-drop from your desktop
- Download, rename, and delete files (with confirmation)
- Drag and drop files between folders in the sidebar
- Edit text files directly in the browser
- Preview images in a lightbox
- Stream video files (mp4, webm, ogg, mov)
- Search files by name in the current directory
- Real-time file system updates via WebSocket
- Single-admin authentication with session cookies

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — for running locally
- [Docker](https://www.docker.com/) — for running in a container

---

## Generating a Bcrypt Password Hash

Before setting up, you need a bcrypt hash of your chosen password. Use an online generator:

1. Go to [bcrypt-generator.com](https://bcrypt-generator.com)
2. Enter your password and use a cost factor of `11` or `12`
3. Click **Generate** and copy the resulting hash (starts with `$2a$...`)

Keep this hash — you'll need it in the steps below.

---

## Running Locally

**1. Clone the repository:**
```bash
git clone <your-repo-url>
cd FileExplorer
```

**2. Configure credentials in `appsettings.Development.json`:**
```json
{
  "RootPath": "/path/to/your/folder",
  "AdminCredentials": {
    "Username": "admin",
    "Password": "$2a$11$your_bcrypt_hash_here"
  }
}
```

> `appsettings.Development.json` is gitignored — your credentials will not be committed.

**3. Run the app:**
```bash
dotnet run
```

**4. Open your browser at:** `http://localhost:5247`

---

## Deploying on a Raspberry Pi

**1. Install Docker on the Pi** if not already installed:
```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
```
Log out and back in for the group change to take effect.

**2. Clone the repository on the Pi:**
```bash
git clone <your-repo-url>
cd FileExplorer
```

**3. Set up your environment variables:**

Copy the example env file and fill in your values:
```bash
cp .env.example .env
nano .env
```

`.env` contents:
```
ADMIN_USERNAME=admin
ADMIN_PASSWORD=$2a$11$your_bcrypt_hash_here
FILES_PATH=/path/to/your/folder
```

> `.env` is gitignored — your credentials will not be committed.

**4. Build and start with Docker Compose:**
```bash
docker compose up -d --build
```

**5. Find your Pi's IP address:**
```bash
hostname -I
```

**6. Access on your local network at:** `http://<pi-ip>:8080`

---

## Updating the App

To pull new changes and rebuild on the Pi:
```bash
cd FileExplorer
git pull
docker compose up -d --build
```

---

## Exposing to the Internet (Cloudflare Tunnel)

Cloudflare Tunnel gives you a public HTTPS URL without opening router ports or exposing your home IP.

**1. Install `cloudflared` on your Pi:**
```bash
curl -L https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-arm64 -o cloudflared
chmod +x cloudflared
sudo mv cloudflared /usr/local/bin/
```

**2. Quick test (no account required):**
```bash
cloudflared tunnel --url http://localhost:8080
```
A temporary public HTTPS URL will be printed to the terminal.

**3. Permanent setup (requires a Cloudflare account and domain):**
```bash
cloudflared tunnel login
cloudflared tunnel create fileexplorer
cloudflared tunnel route dns fileexplorer files.yourdomain.com
cloudflared tunnel run fileexplorer
```

Create `~/.cloudflared/config.yml`:
```yaml
tunnel: fileexplorer
credentials-file: /home/pi/.cloudflared/<tunnel-uuid>.json

ingress:
  - hostname: files.yourdomain.com
    service: http://localhost:8080
  - service: http_status:404
```

**4. Run as a system service (auto-start on boot):**
```bash
sudo cloudflared service install
sudo systemctl enable cloudflared
sudo systemctl start cloudflared
```

---

## Security Notes

- Passwords are stored as **bcrypt hashes** — never store plaintext passwords
- Do not commit `appsettings.Development.json` or `.env` (both are gitignored)
- Do not commit credentials to `appsettings.json` — use environment variables in production
- Session cookies are `HttpOnly` — JavaScript cannot read them
- All file paths are validated server-side to prevent path traversal attacks
- Consider enabling HTTPS in production via a reverse proxy (Nginx, Caddy) or Cloudflare Tunnel

---

## Project Structure

```
FileExplorer/
├── Controllers/
│   ├── AuthController.cs        # Login, logout
│   └── FilesController.cs       # File operations REST API
├── Middleware/
│   └── FileWatcherHandler.cs    # WebSocket file system watcher
├── Models/
│   └── Requests.cs              # Request body models
├── Services/
│   └── FileService.cs           # File system logic
├── wwwroot/
│   ├── index.html               # Main app
│   ├── index.js                 # Frontend client
│   ├── login.html               # Login page
│   ├── login.js                 # Login logic
│   └── styles.css               # Shared styles
├── appsettings.json             # Base config (no secrets)
├── appsettings.Development.json # Local dev config (gitignored)
├── docker-compose.yml           # Compose deployment
├── .env.example                 # Environment variable template
├── Dockerfile
└── Program.cs                   # App entry point and middleware
```
