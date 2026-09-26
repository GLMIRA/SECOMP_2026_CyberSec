<!-- Passo a passo para instalar o SQLite -->
# Como instalar o SQLite (CLI)

> Opcional: o banco (`db/banco.db`) é criado automaticamente na 1ª execução do servidor.
> O `sqlite3` CLI só é útil para criar/inspecionar o banco na mão.

## Linux — Debian / Kali / Ubuntu
```bash
sudo apt update
sudo apt install -y sqlite3
```

## Windows
Baixe em https://sqlite.org/download.html (pacote **"Precompiled Binaries for Windows"**) ou:
```
winget install SQLite.SQLite
```

## Verificar
```
sqlite3 --version
```

## Criar o banco manualmente (opcional)
A partir da raiz `banco-ctf/`:
```bash
sqlite3 db/banco.db < db/schema.sql
```
