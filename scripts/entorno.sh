#!/bin/bash
# ============================================
# Script de gestión de entorno NexoPostal
# Usage: ./scripts/entorno.sh [comando]
# ============================================

set -e

# Colores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Función para mostrar ayuda
show_help() {
    echo "Uso: $0 [comando]"
    echo ""
    echo "Comandos disponibles:"
    echo "  install          - Instalar dependencias y preparar entorno"
    echo "  start           - Iniciar todos los servicios"
    echo "  stop            - Detener todos los servicios"
    echo "  restart         - Reiniciar todos los servicios"
    echo "  status          - Ver estado de los servicios"
    echo "  logs            - Ver logs de los servicios"
    echo "  build           - Reconstruir imágenes Docker"
    echo "  clean           - Limpiar contenedores y volúmenes"
    echo "  prod            - Preparar para producción"
    echo "  ssl-generate    - Generar certificados SSL autofirmados"
    echo "  db-reset        - Reiniciar bases de datos (¡CUIDADO!)"
    echo ""
}

# Función para iniciar servicios
start_services() {
    echo -e "${GREEN}Iniciando servicios NexoPostal...${NC}"
    docker-compose up -d
    echo -e "${GREEN}Servicios iniciados.${NC}"
    echo ""
    echo "Servicios disponibles:"
    echo "  - Clientes:    http://localhost (nexopostal.es)"
    echo "  - Intranet:    http://localhost:4202 (intranet.nexopostal.es)"
    echo "  - Driver:      http://localhost:4201 (driver.nexopostal.es)"
    echo "  - API Gateway: http://localhost:8080"
    echo ""
    echo "Bases de datos:"
    echo "  - Auth:       localhost:15432"
    echo "  - Ciudadano: localhost:15433"
    echo "  - Intranet:   localhost:15434"
    echo "  - Reparto:    localhost:15435"
}

# Función para ver logs
show_logs() {
    docker-compose logs -f --tail=100
}

# Función para ver estado
show_status() {
    docker-compose ps
}

# Función para generar certificados SSL
generate_ssl() {
    echo -e "${YELLOW}Generando certificados SSL autofirmados...${NC}"
    
    # Crear directorio si no existe
    mkdir -p nginx/certs
    
    # Generar clave privada
    openssl genrsa -out nginx/certs/nexopostal.key 2048
    
    # Generar certificado
    openssl req -new -x509 -key nginx/certs/nexopostal.key \
        -out nginx/certs/nexopostal.crt \
        -days 365 \
        -subj "/C=ES/ST=Madrid/L=Madrid/O=NexoPostal/CN=nexopostal.es"
    
    echo -e "${GREEN}Certificados generados en nginx/certs/${NC}"
    echo "⚠️  Para producción, usa Let's Encrypt o un certificado válido"
}

# Función para preparar producción
prepare_prod() {
    echo -e "${YELLOW}Preparando entorno de producción...${NC}"
    
    # Verificar que .env existe
    if [ ! -f .env ]; then
        echo -e "${RED}Error: Archivo .env no encontrado.${NC}"
        echo "Copia .env.example a .env y configúralo"
        exit 1
    fi
    
    # VerificarJWT secreto
    source .env
    if [[ "$JWT_SECRET_KEY" == "TU_CLAVE"* ]]; then
        echo -e "${RED}Error: Debes configurar JWT_SECRET_KEY en .env${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}Entorno de producción preparado.${NC}"
    echo "Próximo paso: Desplegar a tu proveedor de cloud"
}

# Función para construir
build() {
    echo -e "${YELLOW}Reconstruyendo imágenes...${NC}"
    docker-compose build --no-cache
    echo -e "${GREEN}Imágenes reconstruidas.${NC}"
}

# Función para limpiar
clean() {
    echo -e "${YELLOW}Limpiando contenedores y volúmenes...${NC}"
    docker-compose down -v
    echo -e "${GREEN}Limpieza completada.${NC}"
}

# Función para reiniciar BD
reset_db() {
    echo -e "${RED}¡ATENCIÓN! Esto eliminará todas las bases de datos.${NC}"
    read -p "¿Estás seguro? (escribe 'SI' para continuar): " confirm
    if [ "$confirm" = "SI" ]; then
        docker-compose down -v
        docker-compose up -d
        echo -e "${GREEN}Bases de datos reiniciadas.${NC}"
    else
        echo "Operación cancelada."
    fi
}

# Función para instalar
install() {
    echo -e "${GREEN}Instalando dependencias...${NC}"
    
    # Verificar Docker
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}Docker no está instalado.${NC}"
        exit 1
    fi
    
    # Verificar Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        echo -e "${RED}Docker Compose no está instalado.${NC}"
        exit 1
    fi
    
    # Generar certificados si no existen
    if [ ! -f nginx/certs/nexopostal.crt ]; then
        generate_ssl
    fi
    
    echo -e "${GREEN}Instalación completada.${NC}"
    echo ""
    echo "Para iniciar: $0 start"
}

# ============================================
# MAIN
# ============================================

# Cambiar al directorio del script
cd "$(dirname "$0")"

case "$1" in
    install)
        install
        ;;
    start)
        start_services
        ;;
    stop)
        docker-compose stop
        ;;
    restart)
        docker-compose restart
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs
        ;;
    build)
        build
        ;;
    clean)
        clean
        ;;
    prod)
        prepare_prod
        ;;
    ssl-generate)
        generate_ssl
        ;;
    db-reset)
        reset_db
        ;;
    *)
        show_help
        ;;
esac