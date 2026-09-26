<!-- Passo a passo para rodar o projeto -->
# Como rodar — Banco CTF (C# / ASP.NET Core)

> **AVISO:** projeto educacional, contém código inseguro de propósito.
> **NÃO use em produção.** Uso apenas em localhost.

## Pré-requisitos
- **.NET SDK 6+** (`dotnet --version`). Veja `COMO_INSTALAR_DOTNET.md`.

## Passo a passo (rode a partir da raiz `csharp/`)

O banco `db/banco.db` é **criado automaticamente na 1ª execução**. O comando é o mesmo nos dois sistemas.

### Linux / Kali
```bash
dotnet run --project back-end
```

### Windows (cmd ou PowerShell)
```
dotnet run --project back-end
```

**Acessar:** http://localhost:8092

## Usuários de teste
| usuário | senha    |
|---------|----------|
| alice   | alice123 |
| bob     | bob123   |

## Dica (interceptar no Burp)
O navegador costuma **ignorar o proxy para `localhost`**. Para o Burp enxergar o tráfego,
acesse por:
```
http://127.0.0.1.nip.io:8092
```
