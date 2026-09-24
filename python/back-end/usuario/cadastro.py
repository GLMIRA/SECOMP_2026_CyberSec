# Rota /cadastro: cria um novo cliente (usuario, perfil e uma conta).
from flask import Blueprint, request

import banco
from util import responder

rota = Blueprint("cadastro", __name__)


@rota.route("/cadastro", methods=["POST"])
def cadastro():
    # Le os campos enviados pelo formulario.
    usuario = request.form.get("usuario", "")
    senha = request.form.get("senha", "")
    nome = request.form.get("nome", "")
    cpf = request.form.get("cpf", "")

    # Usuario e senha sao obrigatorios.
    if not usuario or not senha:
        return responder("Informe usuario e senha.", 400)

    try:
        conexao = banco.conectar()
        cursor = conexao.cursor()

        # 1) Cria o usuario e recupera o id gerado.
        cursor.execute(
            "INSERT INTO usuarios (usuario, senha) VALUES (?, ?)",
            (usuario, senha),
        )
        usuario_id = cursor.lastrowid

        # 2) Cria o perfil do usuario (relacao 1:1).
        cursor.execute(
            "INSERT INTO perfis (usuario_id, nome, cpf) VALUES (?, ?, ?)",
            (usuario_id, nome, cpf),
        )

        # 3) Cria uma conta para o usuario, comecando com saldo 0.
        cursor.execute(
            "INSERT INTO contas (usuario_id, saldo) VALUES (?, 0)",
            (usuario_id,),
        )

        conexao.commit()
        conexao.close()
        return responder("Cadastro realizado! id do usuario: " + str(usuario_id))
    except Exception as erro:
        # Ex.: usuario ja existente (o campo usuario e UNIQUE no banco).
        return responder("Nao foi possivel cadastrar: " + str(erro), 400)
