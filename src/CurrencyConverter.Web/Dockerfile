# Build stage
FROM node:20-alpine AS build

# Accept build argument for API URL
ARG VITE_API_BASE_URL=http://localhost:8080

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source files
COPY . .

# Create .env file with the API URL
RUN echo "VITE_API_BASE_URL=${VITE_API_BASE_URL}" > .env

# Build the application
RUN npm run build

# Production stage
FROM nginx:alpine

# Copy built files to nginx
COPY --from=build /app/dist /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
