# Esqueleto do AutoDiagnóstico — como subir

Este pacote tem 17 arquivos, já nas pastas certas:
- **12 novos:** a pasta `Services/` inteira e 5 models novos.
- **5 alterados:** `AutoDiagnostico.csproj`, `Program.cs`, `appsettings.json`, `Models/Consulta.cs` e `Models/Usuario.cs`.

O que cada arquivo faz está explicado no card "Esqueleto do código" do Trello e nos comentários dentro dos próprios arquivos.

## 1. Preparar a branch

No terminal, dentro da pasta do projeto:

```
git checkout main
git pull
git checkout -b esqueleto
```

## 2. Copiar os arquivos

Extraia o ZIP **na raiz do projeto**, a pasta onde está o `AutoDiagnostico.csproj`. Quando o Windows perguntar, escolha **substituir** os arquivos.

Depois confira:

```
git status
```

O resultado esperado é:
- 5 arquivos "modified";
- a pasta `Services/` e 5 arquivos em `Models/` como "untracked".

Este arquivo `LEIA-ME-ESQUELETO.md` não precisa ir para o Git. Pode apagar.

## 3. Compilar

```
dotnet build
```

Precisa terminar com **0 Error(s)**. Avisos (warnings) não impedem.

> Este código foi escrito sem um compilador .NET disponível no ambiente do Claude. Por isso o `dotnet build` é o primeiro teste real. Se der erro, copie a mensagem **inteira** e mande para o Claude antes de seguir.

## 4. Criar e aplicar a migration (mudança no banco)

```
dotnet ef migrations add AdicionaDiagnosticoESenha
```

Abra o arquivo novo em `Migrations/` (termina em `_AdicionaDiagnosticoESenha.cs`) e confira se ele:
- adiciona as colunas `Resumo`, `PossiveisCausas`, `Gravidade` e `Especialidade` em `Consultas`;
- adiciona `SenhaHash` em `Usuarios`;
- altera `UsuarioId` para aceitar nulo (`nullable: true`).

Se estiver tudo lá:

```
dotnet ef database update
```

## 5. Rodar

```
dotnet run
```

Abra o endereço que aparece no terminal. A página inicial ainda é a padrão, porque as telas são do Gabriel. O teste aqui é só "abre sem erro".

## 6. Subir para o GitHub

```
git add -A
git commit -m "Esqueleto: contratos, dublês, models, configuração e login por cookie"
git push -u origin esqueleto
```

No GitHub:
1. Clique em **Compare & pull request**.
2. Confira que está **base: main ← compare: esqueleto**.
3. Clique em **Create pull request** e depois em **Merge pull request**.

## 7. Destravar a equipe

1. **GitHub → Settings → Collaborators:** adicione Luan e Gabriel. Sem isso, o push deles é recusado.
2. **Mensagem para o grupo** (pode copiar):

> O esqueleto está na main. Antes de começar, todo mundo faz a Rotina de início do Trello: git checkout main, git pull, git checkout sua-branch, git merge main, dotnet ef database update, dotnet run.
> Não é mais preciso rodar "dotnet user-secrets init": o projeto já tem o ID dos segredos. Milena e Kauan: rodem de novo o "dotnet user-secrets set" da chave de vocês. Se tinham feito init antes, a chave ficou guardada com outro ID.
> Por padrão o site usa os DUBLÊS (dados falsos), então ninguém precisa de chave para trabalhar.
> Cada um mexe só nos arquivos do seu card. Program.cs, appsettings.json e Migrations são só meus.
