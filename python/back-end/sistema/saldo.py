# Rota /saldo: informa o saldo de uma conta.
import contextlib

from flask import Blueprint, request

import banco
from util import responder, usuario_logado

rota = Blueprint("saldo", __name__)


@rota.route("/saldo", methods=["GET"])
def saldo():
    # Precisa estar logado.
    if usuario_logado() is None:
        return responder("Faca login primeiro.", 401)

    # A consulta usa GET, com o numero da conta na URL (?conta=1).
    conta = request.args.get("conta", "")
    if not conta:
        return responder("Informe a conta (?conta=...).", 400)

    try:
        with contextlib.closing(banco.conectar()) as conexao:
            cursor = conexao.cursor()

            # Busca o saldo da conta pedida.
            cursor.execute("SELECT saldo FROM contas WHERE id = ?", (conta,))
            linha = cursor.fetchone()

        if linha:
            return responder("Saldo da conta " + conta + ": " + str(linha[0]))
        return responder("Conta nao encontrada.", 404)
    except Exception as erro:
        return responder("Erro ao consultar saldo: " + str(erro), 400)
