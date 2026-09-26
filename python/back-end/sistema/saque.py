# Rota /saque: retira um valor do saldo de uma conta.
import contextlib

from flask import Blueprint, request

import banco
from util import responder, usuario_logado

rota = Blueprint("saque", __name__)

# Valor maximo por operacao.
LIMITE = 1_000_000.00


@rota.route("/saque", methods=["POST"])
def saque():
    # Precisa estar logado.
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)

    conta = request.form.get("conta", "")
    valor_texto = request.form.get("valor", "")

    if not conta or not valor_texto:
        return responder("Informe a conta e o valor.", 400)

    # So pode sacar de uma conta sua.
    if not banco.conta_pertence(conta, usuario_id):
        return responder("Essa conta nao e sua.", 403)

    # Converte o valor recebido (texto) para numero.
    try:
        valor = float(valor_texto)
    except ValueError:
        return responder("Valor invalido.", 400)

    if valor <= 0:
        return responder("O valor do saque deve ser positivo.", 400)

    # Nao pode sacar mais de 1.000.000,00 por operacao.
    if valor > LIMITE:
        return responder("Saque maximo por operacao e 1.000.000,00.", 400)

    try:
        with contextlib.closing(banco.conectar()) as conexao:
            cursor = conexao.cursor()

            # Le o saldo atual da conta.
            cursor.execute("SELECT saldo FROM contas WHERE id = ?", (conta,))
            linha = cursor.fetchone()

            if not linha:
                return responder("Conta nao encontrada.", 404)

            saldo_atual = linha[0]

            # Nao pode sacar mais do que tem na conta.
            if valor > saldo_atual:
                return responder("Saldo insuficiente.", 400)

            novo_saldo = saldo_atual - valor

            # Grava o novo saldo.
            cursor.execute("UPDATE contas SET saldo = ? WHERE id = ?", (novo_saldo, conta))
            # Registra a movimentacao (aparece no extrato).
            cursor.execute(
                "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (?, 'saque', ?)",
                (conta, valor),
            )
            conexao.commit()
        return responder("Saque realizado. Novo saldo: " + str(novo_saldo))
    except Exception as erro:
        return responder("Erro no saque: " + str(erro), 400)
