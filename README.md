# vndbre-proxy

```sh
docker build -f ./VndbReProxy.Api/Dockerfile . -t vndbre-proxy
cd VndbReProxy.Api
heroku container:push -a vndbre-proxy web --context-path ..
heroku container:release -a vndbre-proxy web
heroku logs -a vndbre-proxy
```
