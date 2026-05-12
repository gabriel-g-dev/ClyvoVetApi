# 🐾 ClyvoVet API

API RESTful desenvolvida em ASP.NET Core para gestão da jornada contínua de saúde do pet — FIAP Challenge 2026 em parceria com a Clyvo VET.

## 👥 Integrantes
- Gabiel Garcia - RM563298
- Andre Bellandi - RM564662
- Vitor Augusto - RM564227
## 📋 Descrição do Projeto

A ClyvoVet API resolve o problema da fragmentação na jornada de saúde animal. Hoje, tutores só interagem com clínicas em situações de urgência. Nossa solução oferece uma infraestrutura digital para organizar, lembrar e personalizar o cuidado preventivo e terapêutico do pet de forma contínua.

## 🚀 Tecnologias Utilizadas

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 9
- Oracle Database
- Swagger / OpenAPI

## 📁 Estrutura do Projeto
ClyvoVetApi/
├── Controllers/
│   ├── TutoresController.cs
│   ├── PetsController.cs
│   ├── ConsultasController.cs
│   └── VacinasController.cs
├── Models/
│   ├── Tutor.cs
│   ├── Pet.cs
│   ├── Consulta.cs
│   └── Vacina.cs
├── Data/
│   ├── AppDbContext.cs
│   └── AppDbContextFactory.cs
└── Program.cs

## 🔗 Rotas da API

### Tutores
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | /api/Tutores | Lista todos os tutores |
| GET | /api/Tutores/{id} | Busca tutor por ID |
| GET | /api/Tutores/email/{email} | Busca tutor por email |
| GET | /api/Tutores/{id}/pets | Lista pets do tutor |
| POST | /api/Tutores | Cadastra novo tutor |
| PUT | /api/Tutores/{id} | Atualiza tutor |
| DELETE | /api/Tutores/{id} | Remove tutor |

### Pets
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | /api/Pets | Lista todos os pets |
| GET | /api/Pets/{id} | Busca pet por ID |
| GET | /api/Pets/especie/{especie} | Busca pets por espécie |
| GET | /api/Pets/raca/{raca} | Busca pets por raça |
| GET | /api/Pets/{id}/vacinas | Lista vacinas do pet |
| GET | /api/Pets/{id}/consultas | Lista consultas do pet |
| POST | /api/Pets | Cadastra novo pet |
| PUT | /api/Pets/{id} | Atualiza pet |
| DELETE | /api/Pets/{id} | Remove pet |

### Consultas
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | /api/Consultas | Lista todas as consultas |
| GET | /api/Consultas/{id} | Busca consulta por ID |
| GET | /api/Consultas/veterinario/{nome} | Busca por veterinário |
| GET | /api/Consultas/periodo?inicio=&fim= | Busca por período |
| POST | /api/Consultas | Cadastra nova consulta |
| PUT | /api/Consultas/{id} | Atualiza consulta |
| DELETE | /api/Consultas/{id} | Remove consulta |

### Vacinas
| Método | Rota | Descrição |
|--------|------|-----------|
| GET | /api/Vacinas | Lista todas as vacinas |
| GET | /api/Vacinas/{id} | Busca vacina por ID |
| GET | /api/Vacinas/pendentes | Lista vacinas pendentes |
| GET | /api/Vacinas/nome/{nome} | Busca por nome da vacina |
| GET | /api/Vacinas/proximas?dias=30 | Lista próximas vacinas |
| POST | /api/Vacinas | Cadastra nova vacina |
| PUT | /api/Vacinas/{id} | Atualiza vacina |
| DELETE | /api/Vacinas/{id} | Remove vacina |

## ⚙️ Como Rodar o Projeto

### Pré-requisitos
- .NET 10 SDK
- Oracle Database (FIAP) ou acesso à VPN da FIAP
- Git

### Instalação

**1. Clone o repositório:**
```bash
git clone https://github.com/SEU_USUARIO/ClyvoVetApi.git
cd ClyvoVetApi
```

**2. Configure a connection string no `appsettings.json`:**
```json
"ConnectionStrings": {
  "OracleConnection": "User Id=SEU_RM;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL"
}
```

**3. Rode as migrations:**
```bash
dotnet ef database update
```

**4. Execute o projeto:**
```bash
dotnet run
```

**5. Acesse o Swagger:**
http://localhost:5109/swagger

## 📊 Exemplos de Requisições

### Cadastrar um Tutor
```json
POST /api/Tutores
{
  "nome": "João Silva",
  "email": "joao@email.com",
  "telefone": "11999999999"
}
```

### Cadastrar um Pet
```json
POST /api/Pets
{
  "nome": "Rex",
  "especie": "Cachorro",
  "raca": "Labrador",
  "dataNascimento": "2020-03-15",
  "tutorId": 1
}
```

### Cadastrar uma Consulta
```json
POST /api/Consultas
{
  "data": "2026-05-10T10:00:00",
  "descricao": "Consulta de rotina",
  "veterinario": "Dr. Carlos",
  "observacoes": "Pet saudável",
  "petId": 1
}
```

### Cadastrar uma Vacina
```json
POST /api/Vacinas
{
  "nome": "Antirrábica",
  "dataAplicacao": "2026-05-10T10:00:00",
  "proximaDose": "2027-05-10T10:00:00",
  "aplicada": true,
  "petId": 1
}
```