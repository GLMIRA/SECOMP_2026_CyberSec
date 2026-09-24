# Banco CTF (Python)

> **AVISO: projeto educacional. Contém código inseguro de propósito. NÃO use em produção.**

Versão em Python + Flask do Banco CTF. Usa o mesmo `schema.sql` e o mesmo front-end
da versão em Java.

## Como rodar (a partir da raiz do projeto)

```bash
pip install -r requirements.txt
python back-end/app.py
```

O servidor sobe em http://localhost:8091 e cria o banco `db/banco.db` automaticamente
na primeira execução.
