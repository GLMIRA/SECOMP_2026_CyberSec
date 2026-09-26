# Como rodar — Banco CTF (Java)

> **AVISO:** projeto educacional, contém código inseguro de propósito. **NÃO use em produção.** Uso apenas em localhost.

## Pré-requisitos
- **Só o JDK** (`java` e `javac`). Confira: `java -version`.
- Os drivers já vêm no projeto, em `lib/` (`sqlite-jdbc.jar` e `slf4j-api.jar`).
- **Você NÃO precisa instalar o SQLite** — o banco (`db/banco.db`) é **criado automaticamente** na 1ª execução.

---

## ⭐ Jeito mais garantido: terminal
Os comandos por OS estão abaixo (🐧 Linux / 🪟 Windows). Eles colocam os jars **explicitamente no
classpath**, então **sempre funcionam** — sem depender de configuração de IDE.

## IntelliJ (atenção a 1 passo obrigatório)
Se rodar pela IDE, você **precisa** adicionar os jars de `lib/` como biblioteca — senão dá
`ClassNotFoundException: org.sqlite.JDBC` / "Driver do SQLite nao encontrado no classpath" (o IntelliJ
não inclui os jars sozinho).

1. Abra a pasta **`java/`** no IntelliJ.
2. No painel **Project**, abra `lib`, selecione os **dois** `.jar` (`sqlite-jdbc.jar` e `slf4j-api.jar`).
3. **Clique direito → "Add as Library…" → OK.**
4. Rode a classe **`core.Main`** (▶) → abra **http://localhost:8090**.

---

## 🐧 Linux / Kali (terminal, a partir da pasta `java/`)
```bash
# 1) compilar
javac -d back-end/out $(find back-end -name "*.java")

# 2) rodar (o banco é criado sozinho)
java -cp "back-end/out:lib/sqlite-jdbc.jar:lib/slf4j-api.jar" core.Main
```
Abra **http://localhost:8090**

---

## 🪟 Windows — PowerShell (a partir da pasta `java\`)
```powershell
# 1) compilar (o PowerShell monta a lista de arquivos .java)
javac -d back-end\out (Get-ChildItem -Path back-end -Recurse -Filter *.java).FullName

# 2) rodar (repare no ";" no classpath — no Windows é diferente do Linux)
java -cp "back-end\out;lib\sqlite-jdbc.jar;lib\slf4j-api.jar" core.Main
```
Abra **http://localhost:8090**

### 🪟 Windows — Prompt de Comando (cmd), alternativa
```bat
dir /s /b back-end\*.java > sources.txt
javac -d back-end\out @sources.txt
del sources.txt
java -cp "back-end\out;lib\sqlite-jdbc.jar;lib\slf4j-api.jar" core.Main
```

---

## Usuários de teste
| usuário | senha    |
|---------|----------|
| alice   | alice123 |
| bob     | bob123   |

## Dica (interceptar no Burp)
O navegador ignora o proxy para `localhost`. Para o Burp enxergar o tráfego, acesse por:
```
http://127.0.0.1.nip.io:8090
```
