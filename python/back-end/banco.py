# Conexao com o banco de dados SQLite.
import contextlib
import os
import sqlite3

# Pasta raiz do projeto (um nivel acima de "back-end").
RAIZ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CAMINHO_BANCO = os.path.join(RAIZ, "db", "banco.db")
CAMINHO_SCHEMA = os.path.join(RAIZ, "db", "schema.sql")


# Abre uma nova conexao com o banco.
def conectar():
    conexao = sqlite3.connect(CAMINHO_BANCO)
    # O SQLite so respeita as chaves estrangeiras se ligarmos isto em cada conexao.
    conexao.execute("PRAGMA foreign_keys = ON")
    # Espera ate 5s por um lock em vez de falhar com "database is locked".
    conexao.execute("PRAGMA busy_timeout = 5000")
    return conexao


# Verifica se uma conta pertence a um usuario.
def conta_pertence(conta, usuario_id):
    with contextlib.closing(conectar()) as conexao:
        cursor = conexao.cursor()
        cursor.execute(
            "SELECT 1 FROM contas WHERE id = ? AND usuario_id = ?",
            (conta, usuario_id),
        )
        return cursor.fetchone() is not None


# Cria o banco a partir do schema, caso ainda nao exista.
def preparar_banco():
    if not os.path.exists(CAMINHO_BANCO):
        with open(CAMINHO_SCHEMA, encoding="utf-8") as arquivo:
            with contextlib.closing(conectar()) as conexao:
                conexao.executescript(arquivo.read())
                conexao.commit()
