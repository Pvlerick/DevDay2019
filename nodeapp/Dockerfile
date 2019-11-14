FROM node:9.2.0-alpine

WORKDIR /usr/src/app

COPY package*.json ./

RUN npm install
RUN npm install log-timestamp

COPY . .

EXPOSE 8000
CMD [ "npm", "start" ]