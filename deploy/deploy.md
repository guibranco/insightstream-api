# Deploying InsightStream to a Ubuntu VPS

This guide sets up InsightStream end-to-end on a single Ubuntu VPS (root access, behind Nginx,
managed by systemd): .NET runtime, PostgreSQL, Redis, LavinMQ, the API itself, and the cPanel→PHP
mail relay that feeds it.

Architecture recap: cPanel mail server → thin PHP script (pipes raw email) → `POST
/api/ingest/email` → validated, deduplicated, published to LavinMQ → background consumer
(hosted inside the API process) parses and persists it.

## 1. Prerequisites

- Ubuntu 22.04/24.04 VPS, root or sudo access
- A DNS record pointing `api.example.com` (or your chosen hostname) at the VPS
- The frontend's exact origin (scheme + host) for CORS — e.g. `https://guilherme.stracini.com.br`

## 2. Install the .NET runtime

InsightStream only needs the ASP.NET Core **runtime** in production (the SDK is only required for
building or running migrations from source).

```bash
sudo apt update && sudo apt install -y wget apt-transport-https
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y aspnetcore-runtime-10.0
```

## 3. Install PostgreSQL

```bash
sudo apt install -y postgresql postgresql-contrib

sudo -u postgres psql <<'SQL'
CREATE DATABASE insightstream;
CREATE USER insightstream WITH ENCRYPTED PASSWORD 'change-me-strong-password';
GRANT ALL PRIVILEGES ON DATABASE insightstream TO insightstream;
ALTER DATABASE insightstream OWNER TO insightstream;
SQL
```

## 4. Install Redis

```bash
sudo apt install -y redis-server
sudo systemctl enable --now redis-server
```

## 5. Install LavinMQ

LavinMQ ships an apt repository via Cloudsmith:

```bash
curl -1sLf 'https://dl.cloudsmith.io/public/cloudamqp/lavinmq/setup.deb.sh' | sudo -E bash
sudo apt install -y lavinmq
sudo systemctl enable --now lavinmq

# Create a dedicated vhost/user for InsightStream (defaults to guest/guest on "/" otherwise)
sudo lavinmqctl add_user insightstream 'change-me-strong-password'
sudo lavinmqctl set_permissions insightstream ".*" ".*" ".*"
```

LavinMQ speaks AMQP 0-9-1 and is wire-compatible with RabbitMQ.Client, so no client-side changes
are needed. The queue "newsletter.ingest" (durable) and its dead-letter queue
"newsletter.ingest.dlq" are declared automatically by the API on startup.

## 6. Build and publish the API

On your build machine (or directly on the VPS if the SDK is installed there too):

```bash
git clone git@github.com:guibranco/insightstream-api.git
cd insightstream-api

dotnet publish src/InsightStream.Api -c Release -o ./publish
```

Copy `./publish` to the VPS, e.g. `/opt/insightstream`:

```bash
sudo mkdir -p /opt/insightstream
sudo rsync -av ./publish/ root@your-vps:/opt/insightstream/
sudo useradd --system --no-create-home --shell /usr/sbin/nologin insightstream
sudo chown -R insightstream:insightstream /opt/insightstream
```

## 7. Configure environment variables

Create `/opt/insightstream/.env` (systemd `EnvironmentFile`, `KEY=VALUE` per line, **not**
checked into git):

```bash
ASPNETCORE_ENVIRONMENT=Production

ConnectionStrings__Postgres=Host=localhost;Database=insightstream;Username=insightstream;Password=change-me-strong-password
ConnectionStrings__Redis=localhost:6379

Jwt__Key=replace-with-a-random-64-char-secret-generated-with-openssl-rand-base64-48
Jwt__Issuer=InsightStream
Jwt__Audience=InsightStream
Jwt__ExpiryMinutes=60

Ingest__Token=replace-with-a-random-secret-generated-with-openssl-rand-hex-32
Ingest__MaxRequestBodyBytes=10485760
Ingest__RateLimitPerHour=60
Ingest__MaxRetries=3

RabbitMq__HostName=localhost
RabbitMq__Port=5672
RabbitMq__UserName=insightstream
RabbitMq__Password=change-me-strong-password
RabbitMq__VirtualHost=/
RabbitMq__IngestQueue=newsletter.ingest
RabbitMq__IngestDlq=newsletter.ingest.dlq

Cors__AllowedOrigin=https://guilherme.stracini.com.br
```

Lock it down:

```bash
sudo chown insightstream:insightstream /opt/insightstream/.env
sudo chmod 600 /opt/insightstream/.env
```

Generate strong secrets with, e.g.:

```bash
openssl rand -base64 48   # Jwt__Key
openssl rand -hex 32      # Ingest__Token
```

## 8. Run database migrations

From a machine with the .NET SDK and network access to the Postgres instance:

```bash
dotnet ef database update \
  --project src/InsightStream.Infrastructure \
  --startup-project src/InsightStream.Api \
  --connection "Host=your-vps;Database=insightstream;Username=insightstream;Password=change-me-strong-password"
```

This creates the schema and seeds a single admin user (`admin` / `ChangeMe123!`). **Change that
password immediately** — see [Rotating the seeded admin password](#10-rotating-the-seeded-admin-password).

## 9. systemd service

```bash
sudo cp deploy/insightstream.service /etc/systemd/system/insightstream.service
sudo systemctl daemon-reload
sudo systemctl enable --now insightstream
sudo systemctl status insightstream
```

Logs: `journalctl -u insightstream -f` and the rolling file logs under
`/opt/insightstream/logs/insightstream-*.log` (Serilog console + file sinks).

## 10. Rotating the seeded admin password

There is no self-service "change password" endpoint by design (single-admin system). Rotate it
directly in Postgres:

```bash
# Generate a bcrypt hash for the new password (any bcrypt tool works; example uses htpasswd)
python3 -c "import bcrypt; print(bcrypt.hashpw(b'your-new-password', bcrypt.gensalt(rounds=11)).decode())"

sudo -u postgres psql -d insightstream -c \
  "UPDATE users SET password_hash = '<hash from above>', updated_at = now() WHERE username = 'admin';"
```

## 11. Nginx reverse proxy + TLS

```bash
sudo apt install -y nginx
sudo cp deploy/nginx.conf /etc/nginx/sites-available/insightstream
sudo ln -s /etc/nginx/sites-available/insightstream /etc/nginx/sites-enabled/insightstream
sudo nginx -t && sudo systemctl reload nginx

sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d api.example.com
```

`nginx.conf` sets `client_max_body_size 10m` to match `Ingest:MaxRequestBodyBytes` /
Kestrel's `MaxRequestBodySize` — keep the two in sync if you ever change the ingest size limit.

## 12. Wire up the cPanel → PHP → ingest relay

On the cPanel mail server (a **different** server than this VPS):

1. Upload `deploy/ingest-relay.php` somewhere reachable by the mail pipe, e.g.
   `/home/youruser/scripts/ingest-relay.php`.
2. Set `INSIGHTSTREAM_URL` (e.g. `https://api.example.com/api/ingest/email`) and
   `INSIGHTSTREAM_TOKEN` (the same value as `Ingest__Token` above) as environment variables in the
   pipe configuration, or edit the script's defaults directly.
3. In cPanel, configure the destination mailbox's mail routing to pipe to a program:
   `|/usr/bin/php /home/youruser/scripts/ingest-relay.php`
   (via Email → Forwarders, or a custom `.qmail`/`procmail` rule, depending on the mail stack).

The script reads the raw message from STDIN and relays it verbatim to the ingest endpoint,
exiting 0 regardless of outcome so the MTA doesn't bounce or retry mail.

## 13. Smoke test

Replace `$TOKEN` with `Ingest__Token`, `$HOST` with your API's public URL.

```bash
HOST=https://api.example.com
INGEST_TOKEN=your-ingest-token

# 1. Health check (Postgres + Redis + LavinMQ connectivity)
curl -s $HOST/health | jq

# 2. Ingest the sample fixture
curl -s -X POST "$HOST/api/ingest/email" \
  -H "X-Ingest-Token: $INGEST_TOKEN" \
  -H "Content-Type: message/rfc822" \
  --data-binary @tests/fixtures/sample_medium.eml | jq
# -> {"success":true,"data":{"duplicate":false}} (202 Accepted)

# 3. Re-ingest the same fixture: should now report a duplicate, no new message queued
curl -s -X POST "$HOST/api/ingest/email" \
  -H "X-Ingest-Token: $INGEST_TOKEN" \
  -H "Content-Type: message/rfc822" \
  --data-binary @tests/fixtures/sample_medium.eml | jq
# -> {"success":true,"data":{"duplicate":true}} (200 OK)

# 4. Log in as the seeded admin (rotate this password first in production!)
TOKEN=$(curl -s -X POST "$HOST/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"ChangeMe123!"}' | jq -r '.data.token')

# 5. Stats
curl -s "$HOST/api/stats" -H "Authorization: Bearer $TOKEN" | jq

# 6. Links (paginated, prioritized, single, status update)
curl -s "$HOST/api/links?status=Awaiting&page=1&per_page=20&sort=priority" \
  -H "Authorization: Bearer $TOKEN" | jq

curl -s "$HOST/api/links/prioritized" -H "Authorization: Bearer $TOKEN" | jq

LINK_ID=$(curl -s "$HOST/api/links?per_page=1" -H "Authorization: Bearer $TOKEN" | jq -r '.data[0].id')

curl -s "$HOST/api/links/$LINK_ID" -H "Authorization: Bearer $TOKEN" | jq

curl -s -X PUT "$HOST/api/links/$LINK_ID/status" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"status":"Liked"}' | jq

# 7. Newsletters
curl -s "$HOST/api/newsletters" -H "Authorization: Bearer $TOKEN" | jq
NEWSLETTER_ID=$(curl -s "$HOST/api/newsletters" -H "Authorization: Bearer $TOKEN" | jq -r '.data[0].id')
curl -s "$HOST/api/newsletters/$NEWSLETTER_ID" -H "Authorization: Bearer $TOKEN" | jq

# 8. Authors
curl -s "$HOST/api/authors" -H "Authorization: Bearer $TOKEN" | jq
AUTHOR_ID=$(curl -s "$HOST/api/authors" -H "Authorization: Bearer $TOKEN" | jq -r '.data[0].id')
curl -s "$HOST/api/authors/$AUTHOR_ID" -H "Authorization: Bearer $TOKEN" | jq

# 9. CORS preflight succeeds without auth
curl -s -X OPTIONS "$HOST/api/links" \
  -H "Origin: https://guilherme.stracini.com.br" \
  -H "Access-Control-Request-Method: GET" \
  -i | head -20
```

## 14. Splitting the worker out later

The ingest consumer runs today as an `IHostedService` inside `InsightStream.Api` for
single-deployment simplicity. To split it into its own process later:

1. Create an `InsightStream.Worker` console project referencing `InsightStream.Core` and
   `InsightStream.Infrastructure`.
2. Move `src/InsightStream.Api/Workers/IngestConsumerService.cs` there, along with the same DI
   registrations from `Program.cs` (DbContext, RabbitMQ, scoring, parsing).
3. Remove the `AddHostedService<IngestConsumerService>()` line from the API's `Program.cs`.
4. Deploy the worker as its own systemd unit (same `.env`, same LavinMQ credentials).

No API or database contract changes are required — the queue and schema are already shared,
decoupled state.
