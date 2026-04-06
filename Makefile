.PHONY: restore build run test clean

restore:
	dotnet restore ./src/ProductService/ProductService.csproj

build:
	dotnet build ./src/ProductService/ProductService.csproj --no-restore

run:
	dotnet run --project ./src/ProductService

test:
	dotnet test 2>/dev/null || echo "No test project configured"

clean:
	dotnet clean ./src/ProductService/ProductService.csproj
	rm -rf bin/ obj/

docker-build:
	docker build -t dotnet-microservice .

docker-run:
	docker run -p 8080:8080 dotnet-microservice
