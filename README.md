# FileExplorer

A self-hosted web file explorer built with ASP.NET Core and vanilla JavaScript. Access, upload, download, rename, and edit files from any browser. Supports real-time file system updates via WebSocket and video streaming.

## Features

- Browse directories and files through a web UI
- Upload files with a live progress bar
- Download, rename, and delete files
- Edit text files directly in the browser
- Stream video files (mp4, webm, ogg, mov)
- Real-time file system updates via WebSocket
- Single-admin authentication with session cookies

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) — for running locally
- [Docker](https://www.docker.com/) — for running in a container

---

## Running Locally

**1. Clone the repository:**
```bash
git clone <your-repo-url>
cd FileExplorer
```

**2. Generate a bcrypt hash of your chosen password:**

Add a temporary endpoint to `AuthController.cs`, start the app, visit `http://localhost:5247/api/auth/hash?pw=yourpassword`, copy the hash, then remove the endpoint.

```csharp
[HttpGet("hash")]
public IActionResult Hash([FromQuery] string pw) => Ok(BCrypt.Net.BCrypt.HashPassword(pw));
```

**3. Configure credentials in `appsettings.Development.json`:**
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

**4. Run the app:**
```bash
dotnet run
```

**5. Open your browser at:** `http://localhost:5247`

---

## Running with Docker

**1. Build the image:**
```bash
docker build -t fileexplorer .
```

**2. Run the container:**
```bash
docker run -d \
  --restart unless-stopped \
  -p 8080:8080 \
  -v /path/to/your/folder:/data \
  -e AdminCredentials__Username=admin \
  -e AdminCredentials__Password='$2a$11$your_bcrypt_hash_here' \
  --name fileexplorer \
  fileexplorer
```

- `-v /path/to/your/folder:/data` — mounts a folder from your host machine into the container
- `RootPath` is set to `/data` in `appsettings.json` by default — do not change this when using Docker
- Pass credentials as environment variables so they never touch the repository
- ASP.NET Core maps `__` in env var names to `:` in config keys, so `AdminCredentials__Username` maps to `AdminCredentials:Username`

**3. Open your browser at:** `http://localhost:8080`

---

## Deploying on a Raspberry Pi

**1. Build for ARM64 on your Mac:**
```bash
docker buildx build --platform linux/arm64 -t fileexplorer .
```

**2. Transfer and run on the Pi** (or build directly on the Pi):
```bash
git clone <your-repo-url>
cd FileExplorer
docker build -t fileexplorer .
docker run -d \
  --restart unless-stopped \
  -p 8080:8080 \
  -v /home/pi/files:/data \
  -e AdminCredentials__Username=admin \
  -e AdminCredentials__Password='$2a$11$your_bcrypt_hash_here' \
  --name fileexplorer \
  fileexplorer
```

**3. Access on your local network at:** `http://<pi-ip>:8080`

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
- Do not commit `appsettings.Development.json` (it is gitignored)
- Do not commit credentials to `appsettings.json` — use environment variables in production
- Session cookies are `HttpOnly` — JavaScript cannot read them
- All file paths are validated server-side to prevent path traversal attacks
- Consider enabling HTTPS in production via a reverse proxy (Nginx, Caddy) or Cloudflare Tunnel

---

## Project Structure

```
FileExplorer/
├── Controllers/
│   ├── AuthController.cs       # Login, logout
│   └── FilesController.cs      # File operations REST API
├── Middleware/
│   └── FileWatcherHandler.cs   # WebSocket file system watcher
├── Models/
│   └── Requests.cs             # Request body models
├── Services/
│   └── FileService.cs          # File system logic
├── wwwroot/
│   ├── index.html              # Main app
│   ├── index.js                # Frontend client
│   ├── login.html              # Login page
│   ├── login.js                # Login logic
│   └── styles.css              # Shared styles
├── appsettings.json            # Base config (no secrets)
├── appsettings.Development.json # Local dev config (gitignored)
├── Dockerfile
└── Program.cs                  # App entry point and middleware
```
