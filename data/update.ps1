$openssl = "C:\Program Files\Git\usr\bin\openssl.exe"

& $openssl req -x509 -nodes -newkey rsa:2048 -subj '/CN=localhost' -keyout mssql.key -out mssql.pem -days 365