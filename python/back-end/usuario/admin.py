# Rotas /admin/*: area administrativa (listar e deletar usuarios).
from flask import Blueprint, request

import banco
from util import responder, responder_json, usuario_logado

rota = Blueprint("admin", __name__)


# Confere se o usuario logado e administrador.
def eh_admin(usuario_id):
    conexao = banco.conectar()
    cursor = conexao.cursor()
    cursor.execute("SELECT admin FROM usuarios WHERE id = ?", (usuario_id,))
    linha = cursor.fetchone()
    conexao.close()
    return linha is not None and linha[0] == 1


@rota.route("/admin/usuarios", methods=["GET"])
def listar_usuarios():
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)
    if not eh_admin(usuario_id):
        return responder("Acesso restrito a administradores.", 403)

    conexao = banco.conectar()
    cursor = conexao.cursor()
    cursor.execute(
        "SELECT u.id, u.usuario, u.senha, u.admin, p.nome, p.cpf "
        "FROM usuarios u LEFT JOIN perfis p ON u.id = p.usuario_id "
        "ORDER BY u.id"
    )
    linhas = cursor.fetchall()
    conexao.close()

    usuarios = []
    for l in linhas:
        usuarios.append({
            "id": l[0], "usuario": l[1], "senha": l[2],
            "admin": l[3], "nome": l[4], "cpf": l[5],
        })
    return responder_json(usuarios)


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
        conexao = banco.conectar()
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
        conexao.close()
        return responder("Usuario removido.")
    except Exception as erro:
        return responder("Erro ao remover: " + str(erro), 400)
