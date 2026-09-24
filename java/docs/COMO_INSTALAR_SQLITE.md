<!-- Passo a passo para instalar o SQLite -->
# Como instalar o SQLite (CLI)

> Opcional: o banco (`db/banco.db`) é criado automaticamente na 1ª execução do servidor.
> O `sqlite3` CLI só é útil para criar/inspecionar o banco na mão.

## Debian / Kali / Ubuntu
```bash
sudo apt update
sudo apt install -y sqlite3
```

## Verificar
```bash
sqlite3 --version
```

## Criar o banco manualmente (opcional)
A partir da raiz `banco-ctf/`:
```bash
sqlite3 db/banco.db < db/schema.sql
```
