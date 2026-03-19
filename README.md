# Render_Repo

## Redis requirement setup (backup + user rights)

This project uses SQL Server as source of truth. Redis is used as cache.
For your requirement, Redis is configured with:

- AOF persistence (operation log) to recreate DB state
- RDB snapshots (periodic backup points)
- ACL users with different permissions

Current local setup in this project:

- SQL DB (source of truth): `Server=(localdb)\\mssqllocaldb;Database=RenderDb`
- Redis cache: `127.0.0.1:6379`

### 1. Apply Redis ACL + persistence on your local Redis (NO DOCKER)

Run these commands against your local Redis:

```bash
redis-cli -h 127.0.0.1 -p 6379 ACL SETUSER default off
redis-cli -h 127.0.0.1 -p 6379 ACL SETUSER admin on >local123 ~* +@all
redis-cli -h 127.0.0.1 -p 6379 ACL SETUSER normal_db_user on >local123 ~* +@read +@write +@keyspace +@connection -@admin -@dangerous
redis-cli -h 127.0.0.1 -p 6379 ACL SETUSER readonly_user on >local123 ~* +@read +@keyspace +@connection -@write -@admin -@dangerous
redis-cli -h 127.0.0.1 -p 6379 ACL SETUSER writeonly_user on >local123 ~* +@write +@keyspace +@connection -@read -@admin -@dangerous

redis-cli -h 127.0.0.1 -p 6379 CONFIG SET appendonly yes
redis-cli -h 127.0.0.1 -p 6379 CONFIG SET appendfsync everysec
redis-cli -h 127.0.0.1 -p 6379 CONFIG SET save "900 1 300 10 60 10000"
redis-cli -h 127.0.0.1 -p 6379 CONFIG REWRITE
```

This enables:

- operation-log style backup (AOF)
- snapshot backup (RDB)
- user-right based access with 4 users

### 2. App connection (cache user)

The app is configured to use Redis as cache only:

- `Render/appsettings.json` -> `RedisSettings:ConnectionString=127.0.0.1:6379`
- `Render/appsettings.json` -> `RedisSettings:Username=normal_db_user`
- `Render/appsettings.json` -> `RedisSettings:Password=local123`

SQL stays primary DB in LocalDB.

### 3. Verify ACL rights from console

Admin should have full rights:

```bash
redis-cli -u redis://admin:local123@127.0.0.1:6379 set admin:test ok
redis-cli -u redis://admin:local123@127.0.0.1:6379 flushdb
```

Normal DB user should read/write, but not admin commands:

```bash
redis-cli -u redis://normal_db_user:local123@127.0.0.1:6379 set normal:test ok
redis-cli -u redis://normal_db_user:local123@127.0.0.1:6379 get normal:test
redis-cli -u redis://normal_db_user:local123@127.0.0.1:6379 flushall
```

Readonly user should read, but fail on write:

```bash
redis-cli -u redis://readonly_user:local123@127.0.0.1:6379 get normal:test
redis-cli -u redis://readonly_user:local123@127.0.0.1:6379 set ro:test nope
```

Writeonly user should write, but fail on read:

```bash
redis-cli -u redis://writeonly_user:local123@127.0.0.1:6379 set wo:test ok
redis-cli -u redis://writeonly_user:local123@127.0.0.1:6379 get wo:test
```

Expected:

- `normal_db_user` can `SET/GET`, but `FLUSHALL` fails with `NOPERM`
- `readonly_user` cannot `SET` (`NOPERM`)
- `writeonly_user` cannot `GET` (`NOPERM`)

### 4. Verify backup mode from console

```bash
redis-cli -u redis://admin:local123@127.0.0.1:6379 info persistence
```

Expected keys in output:

- `aof_enabled:1`
- `rdb_last_bgsave_status:ok`

### 5. App-level delete authorization rule

- Normal users can delete only their own posts.
- User with username `admin` gets `Admin` role on login and can delete any post.

### Notes

- SQL remains the primary database.
- Redis persistence here is mainly to satisfy requirement and for failure recovery behavior.
