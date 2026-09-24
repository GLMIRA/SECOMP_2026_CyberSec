# Rota /comprovante: baixa o comprovante (arquivo) de uma operacao.
import os

from flask import Blueprint, request

import banco
from util import responder, usuario_logado

rota = Blueprint("comprovante", __name__)

# Pasta onde ficam os comprovantes (na raiz do projeto).
PASTA = os.path.join(banco.RAIZ, "comprovantes")


@rota.route("/comprovante", methods=["GET"])
def comprovante():
    # Precisa estar logado.
    if usuario_logado() is None:
        return responder("Faca login primeiro.", 401)

    arquivo = request.args.get("arquivo", "")
    if not arquivo:
        return responder("Informe o arquivo (?arquivo=...).", 400)

    try:
        caminho = os.path.join(PASTA, arquivo)
        with open(caminho, encoding="utf-8", errors="replace") as f:
            return responder(f.read())
    except Exception as erro:
        return responder("Erro ao abrir comprovante: " + str(erro), 400)
