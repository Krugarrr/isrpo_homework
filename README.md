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

Стек включает VictoriaMetrics (хранение метрик), vmagent (сбор метрик), Loki (логи), Grafana и VictoriaLogs (как альтернативное хранилище логов)

```bash
cd src/TranscriberVCA.Generated
docker-compose up -d
```

### Grafana дашборд
 http://localhost:3000 (логин `admin` / `admin`)
Дашборд:

1. Transcription Rate - график транскрипций во времени
2. Total Transcriptions - общее количество транскрипций
3. In Progress - количество транскрипций в обработке
4. Completed vs Failed - успешные и неудачные транскрипции
5. Transcription Duration - среднее время обработки
6. Logins - успешные и неудачные попытки входа

<img width="685" height="209" alt="image" src="https://github.com/user-attachments/assets/eeb71144-c274-403e-a4a4-b17a616d9572" />

<img width="601" height="427" alt="image" src="https://github.com/user-attachments/assets/f5d8704e-b333-46e8-a824-75c7dd72c723" />

<img width="475" height="147" alt="image" src="https://github.com/user-attachments/assets/a826bd5a-8338-4000-974b-5cd2ebc3a6cf" />

<img width="694" height="477" alt="image" src="https://github.com/user-attachments/assets/f323df54-9b03-406e-b946-e716b7dc59f5" />

<img width="939" height="817" alt="image" src="https://github.com/user-attachments/assets/1768a4d7-a1c5-4a33-9b29-62e4e424587d" />


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

### Логирование в консоли
<img width="1280" height="243" alt="image" src="https://github.com/user-attachments/assets/ec6b0768-6ad8-4910-a4d4-0db1df32c2b0" />

### Датасурс Loki в Graphana
<img width="1280" height="608" alt="image" src="https://github.com/user-attachments/assets/0ae15a15-a3db-490f-aa83-fd8bff8bc5c9" />

<img width="786" height="240" alt="image" src="https://github.com/user-attachments/assets/8b7c8af8-8d58-481e-b332-0bb41b977de3" />

<img width="1280" height="654" alt="image" src="https://github.com/user-attachments/assets/563ac7be-85d9-4443-8155-75006ea02758" />

### Dashboard в Graphana с Loki
<img width="739" height="241" alt="image" src="https://github.com/user-attachments/assets/5f712b93-874b-4683-b0cd-8da6dad3f74a" />

<img width="1089" height="514" alt="image" src="https://github.com/user-attachments/assets/85112b13-d28e-42ea-96b4-a6f3607b227d" />

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
