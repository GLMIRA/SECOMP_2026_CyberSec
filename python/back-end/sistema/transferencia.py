# Rota /transferencia: transfere um valor de uma conta para outra.
import contextlib

from flask import Blueprint, request

import banco
from util import responder, usuario_logado

rota = Blueprint("transferencia", __name__)

# Valor maximo por operacao.
LIMITE = 1_000_000.00


@rota.route("/transferencia", methods=["POST"])
def transferencia():
    # Precisa estar logado.
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)

    # Campos esperados do formulario.
    origem = request.form.get("origem", "")
    destino = request.form.get("destino", "")
    valor_texto = request.form.get("valor", "")

    if not origem or not destino or not valor_texto:
        return responder("Informe origem, destino e valor.", 400)

    if origem == destino:
        return responder("Origem e destino nao podem ser a mesma conta.", 400)

    # So pode transferir de uma conta sua.
    if not banco.conta_pertence(origem, usuario_id):
        return responder("Essa conta nao e sua.", 403)

    # Converte o valor recebido (texto) para numero.
    try:
        valor = float(valor_texto)
    except ValueError:
        return responder("Valor invalido.", 400)

    if valor > LIMITE:
        return responder("Transferencia maxima por operacao e 1.000.000,00.", 400)

    try:
        with contextlib.closing(banco.conectar()) as conexao:
            cursor = conexao.cursor()

            # Le o saldo das duas contas.
            cursor.execute("SELECT saldo FROM contas WHERE id = ?", (origem,))
            linha_origem = cursor.fetchone()
            cursor.execute("SELECT saldo FROM contas WHERE id = ?", (destino,))
            linha_destino = cursor.fetchone()

            if not linha_origem:
                return responder("Conta de origem nao encontrada.", 404)
            if not linha_destino:
                return responder("Conta de destino nao encontrada.", 404)

            saldo_origem = linha_origem[0]
            saldo_destino = linha_destino[0]

            if valor > saldo_origem:
                return responder("Saldo insuficiente.", 400)

            novo_origem = saldo_origem - valor
            novo_destino = saldo_destino + valor

            cursor.execute("UPDATE contas SET saldo = ? WHERE id = ?", (novo_origem, origem))
            cursor.execute("UPDATE contas SET saldo = ? WHERE id = ?", (novo_destino, destino))

            # Registra a transferencia.
            cursor.execute(
                "INSERT INTO transferencias (conta_origem, conta_destino, valor) VALUES (?, ?, ?)",
                (origem, destino, valor),
            )
            # Registra nos extratos das duas contas.
            cursor.execute(
                "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (?, 'transferencia_saida', ?)",
                (origem, valor),
            )
            cursor.execute(
                "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (?, 'transferencia_entrada', ?)",
                (destino, valor),
            )
            conexao.commit()
        return responder("Transferencia realizada. Novo saldo da origem: " + str(novo_origem))
    except Exception as erro:
        return responder("Erro na transferencia: " + str(erro), 400)
