# 📦 AgroSolutions.Alert
> As Funções do Azure, desenvolvidas no Hackathon da AgroSolutions, mantêm o contexto Serverless para o ambiente Azure com ações assíncronas.

## 🚜 Funcionalidades
  - Processar dados dos sensores;
  - Gerar Alertas;
  - Integrar com API de previsão do Tempo;
  - Gerar Métricas e Gráficos dos sensores;

## ⚙️ Requisitos não funcionais
  - O sistema suporta escalabilidade horizontal conforme aumento de carga com HPA.
  - O sistema garante confiabilidade e consistência eventual na comunicação orientada a eventos.
  - O sistema garante manutenabilidade dado os microsserviços desacoplados.
  - O sistema prove observabilidade, com métricas, logs e logs distribuídos rastreáveis.
  - O sistema garante atualizações contínuas do artefeto de produção com fluxos de integração e entrega contínua.

## 🏗️ Desenho da Arquitetura
<img width="4123" height="4559" alt="Diagrama" src="https://github.com/user-attachments/assets/71eff8d1-67e7-42bf-9005-11694eaa4f83" />

## 🛠️ Detalhes Técnicos
### ⭐ Arquitetura e Padrões
 - Arquitetura orientada a eventos (Event-Driven Architecture – EDA);
 - Clean Architecture;
 - Microsserviços containerizados.

### ⚙️ Backend & Framework
 - .NET 10 com C# 14;
 - Azure Function Isolated com Timer Trigger e RabbitMQ Queue Trigger;
 - API de previsão do Tempo Open Meteo;

### 🗄️ Banco de Dados & Mensageria
 - InfluxDb;
 - RabbitMQ para mensageria assíncrona;
 - Comunicação orientada a eventos;
 - Logs distribuídos com CorrelationId para rastreabilidade.

### 📊 Observabilidade & Monitoramento
 - Prometheus para coleta de métricas;
 - Grafana Loki para centralização de logs;
 - Estratégia de logging estruturado e distribuído.

### 🧪 Testes
 - Testes unitários com xUnit;
 - FluentAssertions para assertions mais expressivas;
 - Moq para criação de mocks e isolamento de dependências.

### 🚀 DevOps & Infraestrutura
 - CI/CD self-hosted;
 - Docker para containerização;
 - Kubernetes (Deployments, Services, HPA, ConfigMaps e Secrets);

## ▶️ Execução
  - Via Kubernertes local (minikube/kind):
    - Execute o comando para aplicar todos os arquivos yamls presentes no diretório:
    ```
    kubectl apply -f .\k8s\
    ```
    - Em seguida faça os PortForwards do:
      - Grafana:
        ```
        kubectl port-forward svc/grafana 3000:3000
        ```
        - Acesse [http://localhost:3000](http://localhost:3000)
        <br>
  
      - InfluxDb:
        ```
        kubectl port-forward svc/influxdb 8086:8086
        ```
        - Acesse [http://localhost:8086](http://localhost:8086)
        <br>

      - Kong Admin Proxy:
        ```
        kubectl port-forward deployment/kong 8000:8000
        ```
        - Acesse [http://localhost:8000](http://localhost:8000)
        <br>

      - RabbitMQ:
        ```
        kubectl port-forward svc/rabbitmq 5672:5672 15672:15672
        ```
        - Acesse [http://localhost:5672](http://localhost:5672)
        <br>

      - Kong:
        ```
        kubectl port-forward deployment/kong 8001:8001
        ```
        Acesse [http://localhost:8001](http://localhost:8001)
        <br>


      - Konga:
        ```
        kubectl port-forward deployment/konga 1337:1337
        ```
        - Acesse [http://localhost:8001](http://localhost:1337)
        <br>

      - Loki:
        ```
        kubectl port-forward svc/loki 3100:3100
        ```
        - Acesse [http://localhost:3100](http://localhost:3100)
        <br>

      - Prometheus:
        ```
        kubectl port-forward svc/prometheus 9090:9090
        ```
        - Acesse [http://localhost:9090](http://localhost:9090)
        <br>

## 🚀 Requisições para Kong API Gateway
```javascript
// Mock sensors datas
const datas = [
  { // Drought
    login: {
      clientId: "sensor-001",
      clientSecret: "passSensor001"
    },
    data: [
      {
        Timestamp: "2026-02-26T08:00:00Z",
        PrecipitationMm: 0,
        WindSpeedKmh: 12,
        SoilPH: 6.2,
        AirTemperatureC: 28,
        AirHumidityPercent: 45,
        SoilMoisturePercent: 25,
        DataQualityScore: 95
      },
      {
        Timestamp: "2026-02-26T16:00:00Z",
        PrecipitationMm: 0,
        WindSpeedKmh: 10,
        SoilPH: 6.1,
        AirTemperatureC: 30,
        AirHumidityPercent: 40,
        SoilMoisturePercent: 22,
        DataQualityScore: 93
      }
    ]
  },
  { // Plague Risk
    login: {
      clientId: "sensor-002",
      clientSecret: "passSensor002"
    },
    data: [
      {
        Timestamp: "2026-02-26T00:00:00Z",
        PrecipitationMm: 0,
        WindSpeedKmh: 8,
        SoilPH: 11,
        AirTemperatureC: 22,
        AirHumidityPercent: 55,
        SoilMoisturePercent: 55,
        DataQualityScore: 90
      },
      {
        Timestamp: "2026-02-26T06:00:00Z",
        PrecipitationMm: 0,
        WindSpeedKmh: 6,
        SoilPH: 10,
        AirTemperatureC: 23,
        AirHumidityPercent: 59,
        SoilMoisturePercent: 58,
        DataQualityScore: 92
      }
    ]
  },
  { // Low Quality
    login: {
      clientId: "sensor-003",
      clientSecret: "passSensor003"
    },
    data: [
      {
        Timestamp: "2026-02-26T07:00:00Z",
        PrecipitationMm: 1,
        WindSpeedKmh: 15,
        SoilPH: 6.8,
        AirTemperatureC: 27,
        AirHumidityPercent: 70,
        SoilMoisturePercent: 50,
        DataQualityScore: 65
      },
      {
        Timestamp: "2026-02-26T10:00:00Z",
        PrecipitationMm: 0,
        WindSpeedKmh: 12,
        SoilPH: 6.7,
        AirTemperatureC: 29,
        AirHumidityPercent: 68,
        SoilMoisturePercent: 52,
        DataQualityScore: 60
      }
    ]
  },
  { // Heat Wave
    login: {
      clientId: "sensor-004",
      clientSecret: "passSensor004"
    },
    data: [
      {
        Timestamp: "2026-02-24T12:00:00Z",
        PrecipitationMm: 0,
        WindSpeedKmh: 9,
        SoilPH: 6,
        AirTemperatureC: 3966,
        AirHumidityPercent: 50,
        SoilMoisturePercent: 40,
        DataQualityScore: 94
      },
      {
        Timestamp: "2026-02-25T12:00:00Z",
        PrecipitationMm: 0,
        WindSpeedKmh: 11,
        SoilPH: 6.1,
        AirTemperatureC: 37,
        AirHumidityPercent: 48,
        SoilMoisturePercent: 38,
        DataQualityScore: 96
      },
      {
        Timestamp: "2026-02-26T12:00:00Z",
        PrecipitationMm: 0,
        WindSpeedKmh: 10,
        SoilPH: 6.2,
        AirTemperatureC: 38,
        AirHumidityPercent: 46,
        SoilMoisturePercent: 35,
        DataQualityScore: 97
      }
    ]
  },
  { // Fungal Risk
    login: {
      clientId: "sensor-005",
      clientSecret: "passSensor005"
    },
    data: [
      {
        Timestamp: "2026-02-26T02:00:00Z",
        PrecipitationMm: 3,
        WindSpeedKmh: 7,
        SoilPH: 6.3,
        AirTemperatureC: 22,
        AirHumidityPercent: 88,
        SoilMoisturePercent: 75,
        DataQualityScore: 93
      },
      {
        Timestamp: "2026-02-26T06:00:00Z",
        PrecipitationMm: 2,
        WindSpeedKmh: 6,
        SoilPH: 6.4,
        AirTemperatureC: 25,
        AirHumidityPercent: 90,
        SoilMoisturePercent: 78,
        DataQualityScore: 92
      },
      {
        Timestamp: "2026-02-26T09:00:00Z",
        PrecipitationMm: 1,
        WindSpeedKmh: 5,
        SoilPH: 6.2,
        AirTemperatureC: 27,
        AirHumidityPercent: 87,
        SoilMoisturePercent: 72,
        DataQualityScore: 94
      }
    ]
  },
  { // High Acidity
    login: {
      clientId: "sensor-006",
      clientSecret: "passSensor006"
    },
    data: [
      {
        Timestamp: "2026-02-26T10:30:00Z",
        PrecipitationMm: 4,
        WindSpeedKmh: 14,
        SoilPH: 4.7,
        AirTemperatureC: 23,
        AirHumidityPercent: 75,
        SoilMoisturePercent: 65,
        DataQualityScore: 90
      },
      {
        Timestamp: "2026-02-26T10:35:00Z",
        PrecipitationMm: 6,
        WindSpeedKmh: 12,
        SoilPH: 4.3,
        AirTemperatureC: 23.5,
        AirHumidityPercent: 72,
        SoilMoisturePercent: 63,
        DataQualityScore: 92
      }
    ]
  },
  { // Normal
    login: {
      clientId: "sensor-007",
      clientSecret: "passSensor007"
    },
    data: [
      {
        Timestamp: "2026-02-26T08:00:00Z",
        PrecipitationMm: 1,
        WindSpeedKmh: 10,
        SoilPH: 6.5,
        AirTemperatureC: 24,
        AirHumidityPercent: 60,
        SoilMoisturePercent: 55,
        DataQualityScore: 92
      },
      {
        Timestamp: "2026-02-26T09:00:00Z",
        PrecipitationMm: 5,
        WindSpeedKmh: 15,
        SoilPH: 6.9,
        AirTemperatureC: 21,
        AirHumidityPercent: 65,
        SoilMoisturePercent: 51,
        DataQualityScore: 95
      }
    ]
  }
];

// Login in Sensors and send them datas
for (const data of datas) {
  let headersSensor = {
    "Content-Type": "application/json"
  };
  let response = await fetch("/ingest/api/v1/sensor-clients/auth", {
    method: "POST",
    body: JSON.stringify(data.login),
    headers: headersSensor
  }).then(r => r.json());
  const sensorToken = response.data.token;
  headersSensor = {
    "Content-Type": "application/json",
    Authorization: `Bearer ${sensorToken}`
  };
    
  for (const sensorData of data.data) {
    await fetch("/ingest/api/v1/sensor-datas/save", {
      method: "POST",
      body: JSON.stringify(sensorData),
      headers: headersSensor
    })
  }
}
```
