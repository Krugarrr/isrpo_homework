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

Стек включает VictoriaMetrics (хранение метрик), vmagent (сбор метрик) и Grafana

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

### Preview
<img width="1064" height="350" alt="image" src="https://github.com/user-attachments/assets/0475e31d-dd3f-44f0-9999-0c588990fdb9" />

### PromQL query details
<img width="353" height="131" alt="image" src="https://github.com/user-attachments/assets/5902cbf1-483a-4805-86ac-fdc4bb2e1420" />

---

<img width="259" height="176" alt="image" src="https://github.com/user-attachments/assets/2b577c88-0ee6-4cc8-a951-547efed23798" />


### Доступ к сервисам

| Сервис | URL                           | Описание |
|--------|-------------------------------|----------|
| **Приложение** | http://localhost:8080         | API сервер |
| **Метрики** | http://localhost:8080/metrics | Prometheus-метрики |
| **VictoriaMetrics** | http://localhost:8428/vmui    | UI для запросов к метрикам |
| **vmagent** | http://localhost:8429/targets | Статус сбора метрик |
| **Grafana** | http://localhost:3000         | Дашборды и логи (логин: `admin` / `admin`) |


