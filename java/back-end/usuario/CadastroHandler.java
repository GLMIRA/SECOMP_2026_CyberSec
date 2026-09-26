package usuario;

import core.Banco;
import core.Util;
import core.Sessao;

// Rota /cadastro: cria um novo cliente (usuario, perfil e uma conta).
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.Statement;
import java.util.Map;

public class CadastroHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        // O cadastro so aceita POST.
        if (!troca.getRequestMethod().equals("POST")) {
            Util.responder(troca, 405, "Use POST.");
            return;
        }

        // Le os campos enviados pelo formulario.
        Map<String, String> dados = Util.lerFormulario(troca);
        String usuario = dados.get("usuario");
        String senha = dados.get("senha");
        String nome = dados.get("nome");
        String cpf = dados.get("cpf");

        // Usuario e senha sao obrigatorios.
        if (usuario == null || usuario.isEmpty() || senha == null || senha.isEmpty()) {
            Util.responder(troca, 400, "Informe usuario e senha.");
            return;
        }

        try (Connection conexao = Banco.conectar()) {

            // 1) Cria o usuario e recupera o id gerado.
            PreparedStatement inserirUsuario = conexao.prepareStatement(
                    "INSERT INTO usuarios (usuario, senha) VALUES (?, ?)",
                    Statement.RETURN_GENERATED_KEYS);
            inserirUsuario.setString(1, usuario);
            inserirUsuario.setString(2, senha);
            inserirUsuario.executeUpdate();

            ResultSet chaves = inserirUsuario.getGeneratedKeys();
            int usuarioId = chaves.next() ? chaves.getInt(1) : -1;

            // 2) Cria o perfil do usuario (relacao 1:1).
            PreparedStatement inserirPerfil = conexao.prepareStatement(
                    "INSERT INTO perfis (usuario_id, nome, cpf) VALUES (?, ?, ?)");
            inserirPerfil.setInt(1, usuarioId);
            inserirPerfil.setString(2, nome);
            inserirPerfil.setString(3, cpf);
            inserirPerfil.executeUpdate();

            // 3) Cria uma conta para o usuario, comecando com saldo 0.
            PreparedStatement inserirConta = conexao.prepareStatement(
                    "INSERT INTO contas (usuario_id, saldo) VALUES (?, 0)");
            inserirConta.setInt(1, usuarioId);
            inserirConta.executeUpdate();

            Util.responder(troca, 200, "Cadastro realizado! id do usuario: " + usuarioId);

        } catch (Exception e) {
            // Ex.: usuario ja existente (o campo usuario e UNIQUE no banco).
            Util.responder(troca, 400, "Nao foi possivel cadastrar: " + e.getMessage());
        }
    }
}
