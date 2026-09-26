# Rotas /admin/*: area administrativa (listar e deletar usuarios).
import contextlib

from flask import Blueprint, request

import banco
from util import responder, responder_json, usuario_logado

rota = Blueprint("admin", __name__)


# Confere se o usuario logado e administrador.
def eh_admin(usuario_id):
    with contextlib.closing(banco.conectar()) as conexao:
        cursor = conexao.cursor()
        cursor.execute("SELECT admin FROM usuarios WHERE id = ?", (usuario_id,))
        linha = cursor.fetchone()
    return linha is not None and linha[0] == 1


@rota.route("/admin/usuarios", methods=["GET"])
def listar_usuarios():
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)
    if not eh_admin(usuario_id):
        return responder("Acesso restrito a administradores.", 403)

    with contextlib.closing(banco.conectar()) as conexao:
        cursor = conexao.cursor()
        cursor.execute(
            "SELECT u.id, u.usuario, u.senha, u.admin, p.nome, p.cpf "
            "FROM usuarios u LEFT JOIN perfis p ON u.id = p.usuario_id "
            "ORDER BY u.id"
        )
        linhas = cursor.fetchall()

    usuarios = []
    for l in linhas:
        usuarios.append({
            "id": l[0], "usuario": l[1], "senha": l[2],
            "admin": l[3], "nome": l[4], "cpf": l[5],
        })
    return responder_json(usuarios)


@rota.route("/admin/transferencias", methods=["GET"])
def listar_transferencias():
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)
    if not eh_admin(usuario_id):
        return responder("Acesso restrito a administradores.", 403)

    with contextlib.closing(banco.conectar()) as conexao:
        cursor = conexao.cursor()
        cursor.execute(
            "SELECT id, conta_origem, conta_destino, valor, data "
            "FROM transferencias ORDER BY id DESC"
        )
        linhas = cursor.fetchall()

    transferencias = []
    for l in linhas:
        transferencias.append({
            "id": l[0], "origem": l[1], "destino": l[2],
            "valor": l[3], "data": l[4],
        })
    return responder_json(transferencias)


@rota.route("/admin/estornar", methods=["POST"])
def estornar_transferencia():
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)
    if not eh_admin(usuario_id):
        return responder("Acesso restrito a administradores.", 403)

    dados = request.get_json(silent=True) or {}
    alvo = dados.get("id")
    if not alvo:
        return responder("Informe o id da transferencia.", 400)

    try:
        with contextlib.closing(banco.conectar()) as conexao:
            cursor = conexao.cursor()

            cursor.execute(
                "SELECT conta_origem, valor FROM transferencias WHERE id = ?", (alvo,))
            linha = cursor.fetchone()
            if not linha:
                return responder("Transferencia nao encontrada.", 404)

            origem = linha[0]
            valor = linha[1]

            # Devolve o valor da transferencia para a conta de origem.
            valor_devolvido = abs(valor)
            cursor.execute("SELECT saldo FROM contas WHERE id = ?", (origem,))
            saldo = cursor.fetchone()[0]
            cursor.execute(
                "UPDATE contas SET saldo = ? WHERE id = ?",
                (saldo + valor_devolvido, origem))
            cursor.execute(
                "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (?, 'estorno', ?)",
                (origem, valor_devolvido))
            conexao.commit()
        return responder("Transferencia estornada. Valor devolvido a conta " + str(origem) + ".")
    except Exception as erro:
        return responder("Erro ao estornar: " + str(erro), 400)


@rota.route("/admin/deletar", methods=["POST"])
def deletar_usuario():
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)
    if not eh_admin(usuario_id):
        return responder("Acesso restrito a administradores.", 403)

    dados = request.get_json(silent=True) or {}
    alvo = dados.get("id")
    if not alvo:
        return responder("Informe o id do usuario.", 400)

    try:
        with contextlib.closing(banco.conectar()) as conexao:
            cursor = conexao.cursor()
            # Remove os dados ligados ao usuario antes de apaga-lo.
            cursor.execute(
                "DELETE FROM movimentacoes WHERE conta_id IN "
                "(SELECT id FROM contas WHERE usuario_id = ?)", (alvo,))
            cursor.execute(
                "DELETE FROM transferencias WHERE conta_origem IN "
                "(SELECT id FROM contas WHERE usuario_id = ?) OR conta_destino IN "
                "(SELECT id FROM contas WHERE usuario_id = ?)", (alvo, alvo))
            cursor.execute("DELETE FROM contas WHERE usuario_id = ?", (alvo,))
            cursor.execute("DELETE FROM perfis WHERE usuario_id = ?", (alvo,))
            cursor.execute("DELETE FROM usuarios WHERE id = ?", (alvo,))
            conexao.commit()
        return responder("Usuario removido.")
    except Exception as erro:
        return responder("Erro ao remover: " + str(erro), 400)
