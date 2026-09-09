
FROM node:18 AS build

WORKDIR /app

# Install the dependencies
COPY package*.json ./
RUN npm ci

# Copy all app files.
COPY . .

RUN npm run build -- --configuration production

# Use nginx to serve the web client.
FROM nginx:alpine

# Copy the client files to the default public location for nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/web-client/browser/ /usr/share/nginx/html/

# Nginx will serve the app on port 80 by default.
EXPOSE 80
