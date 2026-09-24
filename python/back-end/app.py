# Ponto de entrada: cria o servidor Flask, serve o front-end e registra as rotas.
import os

from flask import Flask

import banco
from usuario.admin import rota as rota_admin
from usuario.cadastro import rota as rota_cadastro
from sistema.comprovante import rota as rota_comprovante
from sistema.deposito import rota as rota_deposito
from sistema.extrato import rota as rota_extrato
from usuario.login import rota as rota_login
from usuario.me import rota as rota_me
from usuario.perfil import rota as rota_perfil
from usuario.recuperar_senha import rota as rota_recuperar_senha
from sistema.saldo import rota as rota_saldo
from sistema.saque import rota as rota_saque
from sistema.transferencia import rota as rota_transferencia

# O front-end fica na pasta "front-end", na raiz do projeto.
PASTA_FRONT = os.path.join(banco.RAIZ, "front-end")
app = Flask(__name__, static_folder=PASTA_FRONT, static_url_path="")

# Chave usada para assinar o cookie de sessao (material educacional).
app.secret_key = "banco-ctf-chave-secreta"


# Pagina inicial.
@app.route("/")
def inicio():
    return app.send_static_file("index.html")


# Registra as rotas do sistema.
app.register_blueprint(rota_cadastro)
app.register_blueprint(rota_login)
app.register_blueprint(rota_recuperar_senha)
app.register_blueprint(rota_saldo)
app.register_blueprint(rota_deposito)
app.register_blueprint(rota_saque)
app.register_blueprint(rota_transferencia)
app.register_blueprint(rota_me)
app.register_blueprint(rota_extrato)
app.register_blueprint(rota_perfil)
app.register_blueprint(rota_admin)
app.register_blueprint(rota_comprovante)


if __name__ == "__main__":
    banco.preparar_banco()
    print("Servidor rodando em http://localhost:8091")
    app.run(host="localhost", port=8091)
