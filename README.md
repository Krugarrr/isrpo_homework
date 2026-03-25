# TranscriberVCA.Generated - ASP.NET Core 8.0 Server

Сервис транскрипции YouTube видео с помощью ИИ

## Upgrade NuGet Packages

NuGet packages get frequently updated.

To upgrade this solution to the latest version of all NuGet packages, use the dotnet-outdated tool.

Install dotnet-outdated tool:

```
dotnet tool install --global dotnet-outdated-tool
```

Upgrade only to new minor versions of packages

```
dotnet outdated --upgrade --version-lock Major
```

Upgrade to all new versions of packages (more likely to include breaking API changes)

```
dotnet outdated --upgrade
```

## Run

Linux/OS X:

```
sh build.sh
```

Windows:

```
build.bat
```

## Run in Docker

```
cd src/TranscriberVCA.Generated
docker build -t transcribervca.generated .
docker run -p 5000:8080 transcribervca.generated
```

## API Использование

### Регистрация

```powershell
Invoke-RestMethod -Uri "http://localhost:8080/auth/register" -Method POST -ContentType "application/json" -Body '{"username":"testuser","password":"testpass"}'
```

### Логин

```powershell
$login = Invoke-RestMethod -Uri "http://localhost:8080/auth/login" -Method POST -ContentType "application/json" -Body '{"username":"testuser","password":"testpass"}'
$token = $login.token
```

### Транскрипция (требует токен)

```powershell
$headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
$headers.Add("Authorization", "Bearer $token")
Invoke-RestMethod -Uri "http://localhost:8080/transcribe" -Method POST -ContentType "application/json" -Headers $headers -Body '{"youtube_url":"https://www.youtube.com/watch?v=dQw4w9WgXcQ","model":"base","format":"txt"}'
```

## Метрики и мониторинг

### Prometheus метрики

Приложение экспортирует метрики в формате Prometheus по адресу:

http://localhost:8080/metrics


### Запуск стека мониторинга

Стек включает VictoriaMetrics (хранение метрик), vmagent (сбор метрик), Loki (логи), Grafana VictoriaLogs (как альтернативное хранилище логов), Grafana Tempo (хранилище трейсов) и OpenTelemetry (единый стандарт сбора метрик)

```bash
cd src/TranscriberVCA.Generated
docker-compose up -d
```

### Дашборд метрик
1. Transcription Rate - график транскрипций во времени - `rate(transcriber_transcriptions_started_total[1m])`
2. Total Transcriptions - общее количество транскрипций - `sum(transcriber_transcriptions_started_total)`
3. In Progress - количество транскрипций в обработке - `transcriber_transcriptions_in_progress`
4. Completed vs Failed - успешные и неудачные транскрипции - `sum(transcriber_transcriptions_completed_total)` и `sum(transcriber_transcriptions_completed_total)`
5. Transcription Duration - среднее время обработки - `histogram_quantile(0.95, rate(transcriber_transcription_duration_seconds_bucket[5m]))`
6. Logins - успешные и неудачные попытки входа - `transcriber_logins_total{status="success"}` и `transcriber_logins_total{status="failureч"}`

<img width="685" height="209" alt="image" src="https://github.com/user-attachments/assets/eeb71144-c274-403e-a4a4-b17a616d9572" />

<img width="601" height="427" alt="image" src="https://github.com/user-attachments/assets/f5d8704e-b333-46e8-a824-75c7dd72c723" />

<img width="475" height="147" alt="image" src="https://github.com/user-attachments/assets/a826bd5a-8338-4000-974b-5cd2ebc3a6cf" />

<img width="694" height="477" alt="image" src="https://github.com/user-attachments/assets/f323df54-9b03-406e-b946-e716b7dc59f5" />

### Логирование
Использую Serilog для логирования. Логи отправляются в консоль и в Grafana Loki

### Что логируется

| Событие | Уровень | Пример |
|---------|---------|--------|
| Регистрация пользователя | Information | User testuser registered successfully. Total users: 5 |
| Попытка регистрации существующего пользователя | Warning | Registration failed: user testuser already exists |
| Успешный логин | Information | User testuser logged in successfully |
| Неудачный логин | Warning | Login failed: invalid password for user testuser |
| Старт транскрипции | Information | Transcription abc1234 started by user testuser |
| Завершение транскрипции | Information | Transcription abc1234 completed successfully. Duration: 5023ms |
| Ошибка транскрипции | Error | Transcription abc1234 failed with error: ... |
| Несанкционированный доступ | Warning | User testuser attempted to access transcription without permission |
| HTTP запросы | Information | Автоматически через Serilog middleware |

### Дашборд логов
- Application logs — все логи приложения - `{app="transcriber-vca"}`
- Log rate by level — количество логов по уровням - `sum by (level) (count_over_time({app="transcriber-vca"} | logfmt [1m]))`
- Errors & warnings — проблемные логи - `{app="transcriber-vca"} |= "failed" or |= "Error" or |= "Warning" or |= "Failed"`
- Failed logins per minute — мониторинг брутфорса - `count_over_time({app="transcriber-vca"} |= "Login failed" [1m])`
- Transcription lifecycle — жизненный цикл транскрипций - `{app="transcriber-vca"} |= "Transcription"`
- User registrations per minute — регистрации - `count_over_time({app="transcriber-vca"} |= "registered successfully" [1m])`

### Трейсы дашборд
- Recent Traces - последние трейсы - `{resource.service.name = "transcriber-vca"}`
- Error Traces - ошибочные трейсы - `{resource.service.name = "transcriber-vca" && status = error}`
- Auth Traces - трейсы аутентификации - `{span.auth.username != ""}`
- Slow Requests (>1000ms) — медленные запросы - `{resource.service.name = "transcriber-vca" && duration > 1000ms}`
- Transcription Traces - трейсы транскрипций - `{span.transcription.id != ""}`
- Failed Login Traces - неудачные логины - `{span.auth.result = "invalid_password" || span.auth.result = "user_not_found"}`

### Доступ к сервисам

| Сервис | URL                           | Описание |
|--------|-------------------------------|----------|
| **Приложение** | http://localhost:8080         | API сервер |
| **Метрики** | http://localhost:8080/metrics | Prometheus-метрики |
| **VictoriaMetrics** | http://localhost:8428/vmui    | UI для запросов к метрикам |
| **vmagent** | http://localhost:8429/targets | Статус сбора метрик |
| **Grafana** | http://localhost:3000         | Дашборды и логи (логин: `admin` / `admin`) |
| **Loki** | http://localhost:3100/ready   | Хранилище логов |
| **VictoriaLogs** | http://localhost:9428         | Альтернативное хранилище логов |
| **Grafana Tempo** | http://localhost:3200        | Хранилище трейсов |