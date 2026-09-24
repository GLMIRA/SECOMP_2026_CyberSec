<!-- Passo a passo para rodar o projeto -->
# Como rodar — Banco CTF (Python / Flask)

> **AVISO:** projeto educacional, contém código inseguro de propósito.
> **NÃO use em produção.** Uso apenas em localhost.

## Pré-requisitos
- **Python 3** (`python3 --version`). Veja `COMO_INSTALAR_PYTHON.md`.

## Passo a passo (rode a partir da raiz `banco-ctf2/`)

1. **Criar o ambiente e instalar o Flask:**
   ```bash
   python3 -m venv .venv
   .venv/bin/pip install -r requirements.txt
   ```

2. **Rodar o servidor** (o banco `db/banco.db` é criado automaticamente na 1ª execução):
   ```bash
   .venv/bin/python back-end/app.py
   ```

3. **Acessar:** http://localhost:8091

## Usuários de teste
| usuário | senha    |
|---------|----------|
| alice   | alice123 |
| bob     | bob123   |

## Dica (interceptar no Burp)
O navegador costuma **ignorar o proxy para `localhost`**. Para o Burp enxergar o tráfego,
acesse por:
```
http://127.0.0.1.nip.io:8091
```
