FROM node:20-alpine AS build
WORKDIR /src

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# L'URL de l'API est injectée au build (Vite intègre les variables VITE_* dans le bundle statique).
# Vide par défaut : l'Ingress de production route /api et /hubs vers le backend sur le même
# domaine que le frontend (routage par chemin), donc des appels relatifs suffisent — pas de CORS
# à gérer en production. Passer --build-arg VITE_API_BASE_URL=http://localhost:5248 en local.
ARG VITE_API_BASE_URL=""
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY --from=build /src/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 8080
