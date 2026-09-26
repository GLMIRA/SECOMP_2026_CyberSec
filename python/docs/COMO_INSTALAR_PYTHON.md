<!-- Passo a passo para instalar o Python e o Flask -->
# Como instalar o Python e o Flask

## Linux — Debian / Kali / Ubuntu
```bash
sudo apt update
sudo apt install -y python3 python3-venv python3-pip
```

## Windows
Baixe em https://python.org/downloads (marque **"Add Python to PATH"** na instalação) ou:
```
winget install Python.Python.3.12
```

## Verificar
- Linux: `python3 --version`
- Windows: `python --version`

## Instalar o Flask (dependência do projeto)
A partir da raiz `python/`, dentro de um ambiente virtual:

Linux:
```bash
python3 -m venv .venv
.venv/bin/pip install -r requirements.txt
```

Windows:
```
python -m venv .venv
.venv\Scripts\pip install -r requirements.txt
```
O `requirements.txt` contém apenas o `flask`.
