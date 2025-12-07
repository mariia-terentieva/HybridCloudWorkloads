# test.Dockerfile
FROM alpine:latest

# Устанавливаем простой HTTP сервер
RUN apk add --no-cache lighttpd

# Создаем тестовую HTML страницу
RUN echo '<!DOCTYPE html>\
<html>\
<head>\
    <title>Test Custom Image</title>\
    <style>\
        body { font-family: Arial, sans-serif; text-align: center; padding: 50px; }\
        h1 { color: #1976d2; }\
        .success { color: #4caf50; }\
    </style>\
</head>\
<body>\
    <h1>✅ Custom Image Test</h1>\
    <p class="success">Custom Docker image работает успешно!</p>\
    <p>Image: custom-test:latest</p>\
    <p>Время: $(date)</p>\
</body>\
</html>' > /var/www/localhost/htdocs/index.html

# Открываем порт 80
EXPOSE 80

# Запускаем веб-сервер
CMD ["lighttpd", "-D", "-f", "/etc/lighttpd/lighttpd.conf"]