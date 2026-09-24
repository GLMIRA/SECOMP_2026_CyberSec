# Banco CTF ⚠️

> Aplicação bancária **propositalmente insegura**, criada para um **CTF** e um **mini-hackathon**
> de segurança da informação. Material **educacional** — rode **apenas em localhost**.
> **NÃO use em produção.**

Um "banco" digital minimalista (cadastro, login, ver saldo, depósito, saque, transferência)
implementado em **três linguagens diferentes**, para treinar a **caça e a correção de
vulnerabilidades web**. As três versões têm as **mesmas funcionalidades** e as **mesmas
falhas propositais**.

## Versões

| Pasta | Stack | Porta | Como rodar |
|-------|-------|-------|-----------|
| [`java/`](java/) | Java (HttpServer + SQLite) | 8090 | [`java/docs/COMO_RODAR.md`](java/docs/COMO_RODAR.md) |
| [`python/`](python/) | Python (Flask + SQLite) | 8091 | [`python/docs/COMO_RODAR.md`](python/docs/COMO_RODAR.md) |
| [`csharp/`](csharp/) | C# (ASP.NET Core + SQLite) | 8092 | [`csharp/docs/COMO_RODAR.md`](csharp/docs/COMO_RODAR.md) |

## Para quem?

- **CTF:** encontre e explore as vulnerabilidades escondidas na aplicação.
- **Hackathon:** parta deste código semipronto e **corrija/implemente** as partes que faltam
  (ex.: a transferência vem como esqueleto).

## Usuários de teste

| usuário | senha    |
|---------|----------|
| alice   | alice123 |
| bob     | bob123   |

## Aviso

Este projeto contém **código inseguro de propósito** para fins de estudo. Não hospede em
ambiente público nem use qualquer parte em produção.
