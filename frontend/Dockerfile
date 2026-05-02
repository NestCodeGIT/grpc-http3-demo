FROM node:22-alpine AS build
WORKDIR /app

# Install protoc + grpc-web plugin so `npm run prebuild:prod` can generate the
# TypeScript client from ../backend/GrpcApi/Protos/task.proto inside the image.
RUN apk add --no-cache bash protoc curl \
 && curl -fsSL https://github.com/grpc/grpc-web/releases/download/1.5.0/protoc-gen-grpc-web-1.5.0-linux-x86_64 \
      -o /usr/local/bin/protoc-gen-grpc-web \
 && chmod +x /usr/local/bin/protoc-gen-grpc-web

# The proto file lives outside the frontend context, so the docker-compose build
# context must be the repository root for this Dockerfile (see docker-compose.yml).
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
COPY backend/GrpcApi/Protos /backend/GrpcApi/Protos
RUN ln -s /backend ../backend
RUN npm run build:prod

FROM nginx:alpine AS final
COPY --from=build /app/dist/grpc-http3-frontend/browser /usr/share/nginx/html
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
