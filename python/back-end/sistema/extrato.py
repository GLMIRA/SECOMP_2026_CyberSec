# Rota /extrato: lista as movimentacoes das contas do usuario logado.
import contextlib

from flask import Blueprint

import banco
from util import responder, usuario_logado

rota = Blueprint("extrato", __name__)


@rota.route("/extrato", methods=["GET"])
def extrato():
    # Precisa estar logado.
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)

    try:
        with contextlib.closing(banco.conectar()) as conexao:
            cursor = conexao.cursor()

            # Busca as movimentacoes (deposito, saque, ...) das contas do usuario.
            cursor.execute(
                "SELECT tipo, conta_id, valor, data FROM movimentacoes "
                "WHERE conta_id IN (SELECT id FROM contas WHERE usuario_id = ?) "
                "ORDER BY data DESC",
                (usuario_id,),
            )
            linhas = cursor.fetchall()

        if not linhas:
            return responder("Nenhuma movimentacao.")

        # Monta uma linha de texto por movimentacao.
        movimentacoes = []
        for tipo, conta, valor, data in linhas:
            movimentacoes.append(
                tipo.upper() + "  conta " + str(conta)
                + "  R$ " + str(valor) + "  (" + str(data) + ")"
            )
        return responder("\n".join(movimentacoes))
    except Exception as erro:
        return responder("Erro ao obter extrato: " + str(erro), 400)
