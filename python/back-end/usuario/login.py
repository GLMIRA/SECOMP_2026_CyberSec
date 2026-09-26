# Rota /login: verifica usuario e senha e informa se o acesso foi liberado.
import contextlib

from flask import Blueprint, request, session

import banco
from util import responder

rota = Blueprint("login", __name__)


@rota.route("/login", methods=["POST"])
def login():
    # Le os campos enviados pelo formulario.
    usuario = request.form.get("usuario", "")
    senha = request.form.get("senha", "")

    if not usuario or not senha:
        return responder("Informe usuario e senha.", 400)

    try:
        with contextlib.closing(banco.conectar()) as conexao:
            cursor = conexao.cursor()

            # Monta a consulta com o usuario e a senha recebidos.
            sql = ("SELECT id FROM usuarios "
                   "WHERE usuario = '" + usuario + "' AND senha = '" + senha + "'")
            cursor.execute(sql)

            linha = cursor.fetchone()

        if linha:
            # Guarda na sessao quem acabou de logar.
            session["usuario_id"] = linha[0]
            return responder("Login OK! id do usuario: " + str(linha[0]))
        return responder("Usuario ou senha invalidos.", 401)
    except Exception as erro:
        return responder("Erro no login: " + str(erro), 400)


@rota.route("/logout", methods=["POST"])
def logout():
    # Encerra a sessao do usuario.
    session.clear()
    return responder("Logout feito.")
