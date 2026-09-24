# Rota /transferencia: transfere um valor de uma conta para outra.
# ESQUELETO - a implementacao deve ser feita aqui.
from flask import Blueprint, request

import banco
from util import responder, usuario_logado

rota = Blueprint("transferencia", __name__)


@rota.route("/transferencia", methods=["POST"])
def transferencia():
    # Precisa estar logado.
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)

    # Campos esperados do formulario.
    origem = request.form.get("origem", "")
    destino = request.form.get("destino", "")
    valor = request.form.get("valor", "")

    # TODO: validar os campos recebidos (origem, destino e valor).

    # TODO: conferir que a conta de origem pertence ao usuario logado
    #       (usar banco.conta_pertence(origem, usuario_id)).

    # TODO: buscar o saldo da conta de origem.

    # TODO: aplicar as regras de negocio, por exemplo:
    #   - o valor precisa ser positivo;
    #   - a origem precisa ter saldo suficiente;
    #   - o saldo de uma conta nao pode passar de 1.000.000,00.

    # TODO: debitar da origem e creditar no destino.

    # TODO: registrar a transferencia na tabela "transferencias".

    # TODO: responder com o resultado da operacao.
    return responder("Transferencia ainda nao implementada.", 501)
