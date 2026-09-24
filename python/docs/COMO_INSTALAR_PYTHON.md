<!-- Passo a passo para instalar o Python e o Flask -->
# Como instalar o Python e o Flask

## Debian / Kali / Ubuntu
```bash
sudo apt update
sudo apt install -y python3 python3-venv python3-pip
```

## Verificar
```bash
python3 --version
```

## Instalar o Flask (dependência do projeto)
A partir da raiz `banco-ctf2/`, dentro de um ambiente virtual:
```bash
python3 -m venv .venv
.venv/bin/pip install -r requirements.txt
```
O `requirements.txt` contém apenas o `flask`.
