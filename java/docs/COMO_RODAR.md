<!-- Passo a passo para rodar o projeto -->
# Como rodar — Banco CTF (Java)

> **AVISO:** projeto educacional, contém código inseguro de propósito.
> **NÃO use em produção.** Uso apenas em localhost.

## Pré-requisitos
- **JDK** instalado (`java` e `javac`). Confira com `java -version`.
- Os drivers já vêm no projeto, em `lib/` (`sqlite-jdbc.jar` e `slf4j-api.jar`).
- (Opcional) `sqlite3` CLI — veja `COMO_INSTALAR_SQLITE.md`.

## Passo a passo (rode a partir da raiz `banco-ctf/`)

1. **Banco de dados** — criado automaticamente na 1ª execução.
   Se quiser criar na mão:
   ```bash
   sqlite3 db/banco.db < db/schema.sql
   ```

2. **Compilar:**
   ```bash
   javac -d back-end/out back-end/*.java
   ```

3. **Rodar o servidor:**
   ```bash
   java -cp "back-end/out:lib/sqlite-jdbc.jar:lib/slf4j-api.jar" Main
   ```

4. **Acessar:** http://localhost:8090

## Usuários de teste
| usuário | senha    |
|---------|----------|
| alice   | alice123 |
| bob     | bob123   |

## Dica (interceptar no Burp)
O navegador costuma **ignorar o proxy para `localhost`**. Para o Burp enxergar o tráfego,
acesse por:
```
http://127.0.0.1.nip.io:8090
```
