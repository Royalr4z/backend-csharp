<h1>Backend C# ASP .NET</h1>

## Tecnologias Utilizadas

- **Linguagem:** C#
- **Framework:** .NET
- **Banco de Dados:** PostgreSQL

## Instalação e Configuração

1. Certifique-se de ter o .NET instalado em sua máquina.
2. Clone este repositório.
3. Execute `dotnet run` para iniciar o servidor.

## Rotas/APIs Disponíveis

A seguir estão as principais rotas e APIs fornecidas pelo backend.

### 1. Users

- **Descrição:** API de Usuário
- **Método HTTP:** [GET, GET(ById), DELETE]
- **Exemplo de Requisição:**
  ```bash
  curl -X GET http://localhost:5003/users

### 2. Blogs

- **Descrição:** API do Blog
- **Método HTTP:** [GET, GET(ById), POST, DELETE]
- **Exemplo de Requisição:**
  ```bash
  curl -X GET http://localhost:5003/blogs

### 3. Category

- **Descrição:** API de Categoria
- **Método HTTP:** [GET, GET(ById), POST, DELETE]
- **Exemplo de Requisição:**
  ```bash
  curl -X GET http://localhost:5003/category

### 4. Category

- **Descrição:** API de Categoria
- **Método HTTP:** [GET, GET(ById), POST, DELETE]
- **Exemplo de Requisição:**
  ```bash
  curl -X GET http://localhost:5003/category

### 5. FreeQuote

- **Descrição:** API de Orçamento Gratuito
- **Método HTTP:** [GET, GET(ById), POST, DELETE]
- **Exemplo de Requisição:**
  ```bash
  curl -X GET http://localhost:5003/freequote

### 5. Message

- **Descrição:** API de Mensagem
- **Método HTTP:** [GET, GET(ById), POST, DELETE]
- **Exemplo de Requisição:**
  ```bash
  curl -X GET http://localhost:5003/message
