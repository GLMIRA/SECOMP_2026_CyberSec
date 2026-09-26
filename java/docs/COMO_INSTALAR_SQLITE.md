# Instalar o SQLite (CLI) — OPCIONAL

> ## ⛔ VOCÊ PROVAVELMENTE NÃO PRECISA DISSO
> O banco (`db/banco.db`) é **criado automaticamente** quando você roda o servidor.
> **Para rodar o projeto, PULE este arquivo.** Ele só serve se você quiser **inspecionar/criar
> o banco na mão** com o comando `sqlite3` — nada obrigatório.

Se `sqlite3 --version` der "não é reconhecido/comando não encontrado", **não tem problema**: é só
o CLI que não está instalado — o projeto roda do mesmo jeito.

---

## Instalar (só se quiser mesmo)

### 🐧 Linux / Kali / Ubuntu
```bash
sudo apt update
sudo apt install -y sqlite3
sqlite3 --version
```

### 🪟 Windows
O `winget install SQLite.SQLite` **muitas vezes não adiciona o `sqlite3` ao PATH** (por isso o
"não é reconhecido"). O jeito confiável:
1. Baixe em https://sqlite.org/download.html o pacote **"Precompiled Binaries for Windows"** →
   *sqlite-tools-win-x64* (um .zip).
2. Extraia numa pasta, ex.: `C:\sqlite`.
3. Adicione `C:\sqlite` ao **PATH**: *Iniciar → "variáveis de ambiente" → Editar variáveis de
   ambiente do sistema → Variáveis de ambiente → Path → Novo → `C:\sqlite`*.
4. **Feche e reabra o terminal** e teste: `sqlite3 --version`.

> Ou simplesmente **não instale** — repito, o banco nasce sozinho ao rodar o servidor. 😉

---

## Criar o banco manualmente (opcional)
A partir da raiz `java/`:
```bash
# Linux
sqlite3 db/banco.db < db/schema.sql
```
```powershell
# Windows (PowerShell)
Get-Content db\schema.sql | sqlite3 db\banco.db
```
