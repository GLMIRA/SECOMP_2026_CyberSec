# Funcoes auxiliares para respostas e sessao.
import json

from flask import Response, session


# Monta uma resposta de texto simples com o codigo HTTP informado.
def responder(texto, codigo=200):
    resposta = Response(str(texto), status=codigo, mimetype="text/plain")
    # Libera o acesso a partir do front-end (localhost, material educacional).
    resposta.headers["Access-Control-Allow-Origin"] = "*"
    return resposta


# Resposta em JSON.
def responder_json(dados, codigo=200):
    resposta = Response(json.dumps(dados), status=codigo, mimetype="application/json")
    resposta.headers["Access-Control-Allow-Origin"] = "*"
    return resposta


# Devolve o id do usuario logado, ou None se ninguem estiver logado.
def usuario_logado():
    return session.get("usuario_id")
