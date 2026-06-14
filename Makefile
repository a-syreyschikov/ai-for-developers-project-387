.RECIPEPREFIX := >
.DEFAULT_GOAL := test

IMAGE_NAME ?= calendar-app
BACKEND_IMAGE_NAME ?= calendar-backend
DOTNET_SDK_IMAGE ?= mcr.microsoft.com/dotnet/sdk:8.0
PORT ?= 8080

.PHONY: install contract test test-coverage test-e2e backend-build backend-run backend-test compose-build compose-up compose-down docker-build docker-run clean help

install: ## Install npm dependencies from lockfile
> npm ci

contract: ## Compile TypeSpec and generate contracts/openapi.yaml
> npm run contract

test: ## Generate contract, run contract tests, and enforce coverage
> npm test

test-coverage: ## Run contract tests with coverage report
> npm run test:coverage

test-e2e: ## Run Playwright integration tests against Docker Compose app
> npm run test:e2e --prefix apps/frontend

backend-build: ## Build backend Docker image
> docker build -t $(BACKEND_IMAGE_NAME) apps/backend

backend-run: ## Run backend API on localhost:8080
> docker run --rm -p 8080:8080 $(BACKEND_IMAGE_NAME)

backend-test: ## Run backend tests with coverage in .NET SDK Docker container
> docker run --rm -v $(CURDIR)/apps/backend:/src -w /src $(DOTNET_SDK_IMAGE) dotnet test Calendar.Backend.Tests/Calendar.Backend.Tests.csproj /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:CoverletOutputFormat=json /p:CoverletOutput=TestResults/coverage

compose-build: ## Build frontend and backend compose services
> docker compose build

compose-up: ## Run frontend and backend with Docker Compose
> docker compose up --build

compose-down: ## Stop frontend and backend compose services
> docker compose down

docker-build: ## Build production Docker image
> docker build -t $(IMAGE_NAME) .

docker-run: ## Run production Docker image on PORT
> docker run --rm -e PORT=$(PORT) -p $(PORT):$(PORT) $(IMAGE_NAME)

clean: ## Remove temporary generated output and coverage reports
> rm -rf generated coverage tsp-output

help: ## Show available commands
> @awk 'BEGIN {FS = ":.*##"; printf "Available targets:\n"} /^[a-zA-Z_-]+:.*##/ {printf "  %-16s %s\n", $$1, $$2}' $(MAKEFILE_LIST)
