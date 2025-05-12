# 🧪 Teste Técnico — Microsserviço de Processamento de CNPJ

## 📌 Visão Geral

Este projeto consiste em um **microsserviço em C#** responsável por:

- Consumir mensagens com CNPJs a partir de um tópico Kafka.
- Validar os CNPJs conforme regras oficiais (formato e checksum).
- Persistir registros válidos em um banco PostgreSQL.
- Publicar os resultados em tópicos distintos (sucesso e erro).
- Automatizar build/test/deploy via Makefile.
- (Opcional) Implantar a solução em um cluster Kubernetes local com Helm.

---

## 🧱 Arquitetura

Kafka (TopicCnpjProcessingRequests, TopicCnpjValidationValid, TopicCnpjValidationInvalid)
[ Microsserviço C# ]
PostgreSQL Kafka (TopicCnpjProcessingRequests, TopicCnpjValidationValid, TopicCnpjValidationInvalid)

---

## 🚀 Tecnologias Utilizadas

- **.NET 8 / C#**
- **Confluent.Kafka** (cliente Kafka)
- **Entity Framework Core** (acesso a dados)
- **PostgreSQL**
- **Docker & Docker Compose**
- **Kubernetes (Minikube ou k3s) + Helm**
- **Serilog** (logging estruturado)
- **GitHub Actions** (CI/CD - opcional)

---

## ⚙️ Como Executar o Projeto Localmente

### ✅ Pré-requisitos

- [.NET SDK 8+](https://dotnet.microsoft.com/en-us/download)
- [Docker + Docker Compose](https://docs.docker.com/get-docker/)
- [Kafka e PostgreSQL](https://github.com/confluentinc/cp-all-in-one)
- (Opcional) [Minikube](https://minikube.sigs.k8s.io/) ou [k3s](https://k3s.io/)
- [Helm](https://helm.sh/)

### 🔧 Executando Localmente

```bash
# Inicializar infraestrutura local (Kafka + PostgreSQL)
docker-compose up -d

# Build da aplicação
make build

# Executar testes
make test

# Executar aplicação localmente
make run
