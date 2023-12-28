$openssl = "C:\Program Files\Git\usr\bin\openssl.exe"

& $openssl req -x509 -nodes -newkey rsa:2048 -subj '/CN=sql.docker.internal' -keyout "$PSScriptRoot/mssql.key" -out "$PSScriptRoot/mssql.pem" -days 365