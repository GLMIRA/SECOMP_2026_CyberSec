# Rota /recuperar-senha: troca a senha depois de confirmar a identidade.
import contextlib

from flask import Blueprint, request

import banco
from util import responder

rota = Blueprint("recuperar_senha", __name__)


@rota.route("/recuperar-senha", methods=["POST"])
def recuperar_senha():
    # Le os campos enviados pelo formulario.
    usuario = request.form.get("usuario", "")
    cpf = request.form.get("cpf", "")
    nova_senha = request.form.get("nova_senha", "")

    if not usuario or not cpf or not nova_senha:
        return responder("Informe usuario, cpf e a nova senha.", 400)

    try:
        with contextlib.closing(banco.conectar()) as conexao:
            cursor = conexao.cursor()

            # Confere a identidade: o usuario e o cpf precisam bater.
            sql = ("SELECT u.id FROM usuarios u "
                   "JOIN perfis p ON u.id = p.usuario_id "
                   "WHERE u.usuario = '" + usuario + "' AND p.cpf = '" + cpf + "'")
            cursor.execute(sql)
            linha = cursor.fetchone()

            if not linha:
                return responder("Dados nao conferem.", 401)

            # Identidade confirmada: atualiza a senha.
            usuario_id = linha[0]
            cursor.execute("UPDATE usuarios SET senha = ? WHERE id = ?", (nova_senha, usuario_id))
            conexao.commit()
        return responder("Senha atualizada.")
    except Exception as erro:
        return responder("Erro ao atualizar senha: " + str(erro), 400)
