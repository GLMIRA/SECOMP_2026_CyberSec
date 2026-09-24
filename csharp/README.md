# Banco CTF (C#)

> **AVISO: projeto educacional. Contém código inseguro de propósito. NÃO use em produção.**

Versão em **C# / ASP.NET Core (Minimal API)** do Banco CTF. Gêmea da versão Python
(`banco-ctf2`): mesmo `schema.sql`, mesmo front-end e as mesmas vulnerabilidades propositais.

## Como rodar (a partir da raiz do projeto)

```bash
cd banco-ctf3
dotnet run --project back-end
```

O servidor sobe em http://localhost:8092 e cria o banco `db/banco.db` automaticamente
na primeira execução (a partir de `db/schema.sql`).

Portas dos "gêmeos": Java = 8090, Python = 8091, **C# = 8092**.
