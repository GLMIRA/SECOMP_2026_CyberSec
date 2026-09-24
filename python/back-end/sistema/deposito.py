# Rota /deposito: adiciona um valor ao saldo de uma conta.
from flask import Blueprint, request

import banco
from util import responder, usuario_logado

rota = Blueprint("deposito", __name__)

# Valor maximo permitido em uma conta.
LIMITE = 1_000_000.00


@rota.route("/deposito", methods=["POST"])
def deposito():
    # Precisa estar logado.
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)

    conta = request.form.get("conta", "")
    valor_texto = request.form.get("valor", "")

    if not conta or not valor_texto:
        return responder("Informe a conta e o valor.", 400)

    # So pode depositar em uma conta sua.
    if not banco.conta_pertence(conta, usuario_id):
        return responder("Essa conta nao e sua.", 403)

    # Converte o valor recebido (texto) para numero.
    try:
        valor = float(valor_texto)
    except ValueError:
        return responder("Valor invalido.", 400)

    if valor <= 0:
        return responder("O valor do deposito deve ser positivo.", 400)

    try:
        conexao = banco.conectar()
        cursor = conexao.cursor()

        # Le o saldo atual da conta.
        cursor.execute("SELECT saldo FROM contas WHERE id = ?", (conta,))
        linha = cursor.fetchone()

        if not linha:
            conexao.close()
            return responder("Conta nao encontrada.", 404)

        novo_saldo = linha[0] + valor

        # Aplica a regra do limite maximo por conta.
        if novo_saldo > LIMITE:
            conexao.close()
            return responder("Limite de 1.000.000,00 por conta excedido.", 400)

        # Grava o novo saldo.
        cursor.execute("UPDATE contas SET saldo = ? WHERE id = ?", (novo_saldo, conta))
        # Registra a movimentacao (aparece no extrato).
        cursor.execute(
            "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (?, 'deposito', ?)",
            (conta, valor),
        )
        conexao.commit()
        conexao.close()
        return responder("Deposito realizado. Novo saldo: " + str(novo_saldo))
    except Exception as erro:
        return responder("Erro no deposito: " + str(erro), 400)
