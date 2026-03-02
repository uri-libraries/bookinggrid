# Deploy to EC2 (Amazon Linux + Apache)

## Prerequisites
- Apache running on library.uri.edu with SSL
- Ports 80/443 in use, app will be at `/reserve`

---

## 1. Install Node.js & PM2

```bash
sudo dnf install -y nodejs npm
sudo npm install -g pm2
```

## 2. Set Up Project

```bash
cd /var/www
sudo git clone https://github.com/your-username/booking-grid2.git
cd booking-grid2
sudo chown -R $USER:$USER .
npm install
```

Create `.env.production`:
```
VITE_LIBCAL_API_URL=/reserve/api
VITE_FLOOR_MAP_ID=7592
VITE_ROOM_ITEM_IDS=70047,70048,70049,70050,70052,70053,70054,70055,70060,70061,70062,70063,70064,70065,70066,70067,70069,70070,70071,70072,70073,70074,70075,70076,70077
```

## 3. Update vite.config.ts

```ts
export default defineConfig({
  plugins: [react()],
  base: '/reserve/',
})
```

## 4. Build

```bash
npm run build
```

## 5. Set Up API Proxy

Create `server/index.js` with LibCal credentials:
```bash
npm install express cors
nano server/index.js
# Add your LIBCAL_CLIENT_ID and LIBCAL_CLIENT_SECRET
```

Start with PM2:
```bash
pm2 start server/index.js --name "booking-grid-api"
pm2 save
pm2 startup
```

## 6. Configure Apache

Add to SSL VirtualHost (`/etc/httpd/conf.d/ssl.conf`):

```apache
Alias /reserve /var/www/booking-grid2/dist

<Directory /var/www/booking-grid2/dist>
    Require all granted
    RewriteEngine On
    RewriteBase /reserve/
    RewriteCond %{REQUEST_FILENAME} !-f
    RewriteCond %{REQUEST_FILENAME} !-d
    RewriteCond %{REQUEST_URI} !^/reserve/api
    RewriteRule ^ /reserve/index.html [L]
</Directory>

ProxyPass /reserve/api http://127.0.0.1:3001/reserve/api
ProxyPassReverse /reserve/api http://127.0.0.1:3001/reserve/api
```

Restart:
```bash
sudo apachectl configtest
sudo systemctl restart httpd
```

## 7. Set Permissions

```bash
sudo chown -R apache:apache /var/www/booking-grid2/dist
```

---

## Commands

| Task | Command |
|------|---------|
| View logs | `pm2 logs booking-grid-api` |
| Restart API | `pm2 restart booking-grid-api` |
| Restart Apache | `sudo systemctl restart httpd` |

---

**Site URL:** https://library.uri.edu/reserve
