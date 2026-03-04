# Stage 1: Build de la aplicación Angular
FROM node:20-alpine AS build

WORKDIR /app

# Copiar archivos de dependencias
COPY package*.json ./

# Instalar dependencias
RUN npm ci

# Copiar el resto de los archivos
COPY . .

# Compilar la aplicación para producción
RUN npm run build -- --configuration production

# Stage 2: Servir con nginx
FROM nginx:alpine

# Copiar los archivos compilados desde la etapa de build
COPY --from=build /app/dist/driver-app/browser /usr/share/nginx/html

# Copiar configuración personalizada de nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Exponer el puerto 80
EXPOSE 80

# Comando por defecto (nginx ya viene configurado)
CMD ["nginx", "-g", "daemon off;"]
