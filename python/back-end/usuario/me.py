# Rota /me: informa o usuario logado (pela sessao) e se ele e admin.
import contextlib

from flask import Blueprint

import banco
from util import responder, responder_json, usuario_logado

rota = Blueprint("me", __name__)


@rota.route("/me", methods=["GET"])
def me():
    # Precisa estar logado.
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)

    try:
        with contextlib.closing(banco.conectar()) as conexao:
            cursor = conexao.cursor()

            # Busca o nome de login e se e administrador.
            cursor.execute("SELECT usuario, admin FROM usuarios WHERE id = ?", (usuario_id,))
            linha = cursor.fetchone()

            # Busca o numero da conta do usuario (a primeira, se tiver varias).
            cursor.execute("SELECT id FROM contas WHERE usuario_id = ? ORDER BY id LIMIT 1", (usuario_id,))
            conta = cursor.fetchone()

        if linha:
            return responder_json({
                "id": usuario_id,
                "usuario": linha[0],
                "admin": linha[1],
                "conta": conta[0] if conta else None,
            })
        return responder("Usuario nao encontrado.", 404)
    except Exception as erro:
        return responder("Erro ao obter usuario: " + str(erro), 400)
