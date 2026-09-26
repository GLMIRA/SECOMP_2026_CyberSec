# Rota /perfil: mostra (GET) e atualiza (POST) os dados do usuario logado.
import contextlib

from flask import Blueprint, request

import banco
from util import responder, responder_json, usuario_logado

rota = Blueprint("perfil", __name__)


@rota.route("/perfil", methods=["GET"])
def ver_perfil():
    # Precisa estar logado.
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)

    with contextlib.closing(banco.conectar()) as conexao:
        cursor = conexao.cursor()
        cursor.execute(
            "SELECT u.usuario, u.admin, p.nome, p.cpf "
            "FROM usuarios u JOIN perfis p ON u.id = p.usuario_id "
            "WHERE u.id = ?",
            (usuario_id,),
        )
        linha = cursor.fetchone()

    if not linha:
        return responder("Usuario nao encontrado.", 404)

    # Devolve os dados atuais do cliente para a tela preencher.
    return responder_json({
        "id": usuario_id,
        "usuario": linha[0],
        "admin": linha[1],
        "nome": linha[2],
        "cpf": linha[3],
    })


@rota.route("/perfil", methods=["POST"])
def atualizar_perfil():
    # Precisa estar logado.
    usuario_id = usuario_logado()
    if usuario_id is None:
        return responder("Faca login primeiro.", 401)

    # Le os dados enviados em JSON.
    dados = request.get_json(silent=True)
    if not dados:
        return responder("Envie os dados em JSON.", 400)

    try:
        with contextlib.closing(banco.conectar()) as conexao:
            cursor = conexao.cursor()

            # nome e cpf ficam na tabela de perfil.
            campos_usuario = {}
            for campo, valor in dados.items():
                if campo == "nome":
                    cursor.execute("UPDATE perfis SET nome = ? WHERE usuario_id = ?", (valor, usuario_id))
                elif campo == "cpf":
                    cursor.execute("UPDATE perfis SET cpf = ? WHERE usuario_id = ?", (valor, usuario_id))
                else:
                    campos_usuario[campo] = valor

            # Os demais campos atualizam a tabela de usuarios.
            if campos_usuario:
                partes = []
                valores = []
                for campo, valor in campos_usuario.items():
                    partes.append(campo + " = ?")
                    valores.append(valor)
                valores.append(usuario_id)
                cursor.execute("UPDATE usuarios SET " + ", ".join(partes) + " WHERE id = ?", valores)

            conexao.commit()

            # Devolve o perfil completo ja atualizado (mais visual na resposta).
            cursor.execute(
                "SELECT u.usuario, u.admin, p.nome, p.cpf "
                "FROM usuarios u JOIN perfis p ON u.id = p.usuario_id "
                "WHERE u.id = ?",
                (usuario_id,),
            )
            atual = cursor.fetchone()
        return responder_json({
            "mensagem": "Perfil atualizado.",
            "id": usuario_id,
            "usuario": atual[0],
            "admin": atual[1],
            "nome": atual[2],
            "cpf": atual[3],
        })
    except Exception as erro:
        return responder("Erro ao atualizar perfil: " + str(erro), 400)
