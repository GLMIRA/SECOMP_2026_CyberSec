package usuario;

import core.Banco;
import core.Util;
import core.Sessao;

// Rota /recuperar-senha: troca a senha depois de confirmar a identidade (usuario + cpf).
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.Statement;
import java.util.Map;

public class RecuperarSenhaHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        if (!troca.getRequestMethod().equals("POST")) {
            Util.responder(troca, 405, "Use POST.");
            return;
        }

        Map<String, String> dados = Util.lerFormulario(troca);
        String usuario = dados.get("usuario");
        String cpf = dados.get("cpf");
        String novaSenha = dados.get("nova_senha");

        if (usuario == null || usuario.isEmpty() || cpf == null || cpf.isEmpty()
                || novaSenha == null || novaSenha.isEmpty()) {
            Util.responder(troca, 400, "Informe usuario, cpf e a nova senha.");
            return;
        }

        try (Connection conexao = Banco.conectar()) {
            // Confere a identidade: o usuario e o cpf precisam bater.
            String sql = "SELECT u.id FROM usuarios u JOIN perfis p ON u.id = p.usuario_id "
                    + "WHERE u.usuario = '" + usuario + "' AND p.cpf = '" + cpf + "'";
            Statement comando = conexao.createStatement();
            ResultSet resultado = comando.executeQuery(sql);

            if (!resultado.next()) {
                Util.responder(troca, 401, "Dados nao conferem.");
                return;
            }

            int usuarioId = resultado.getInt("id");
            PreparedStatement atualizar = conexao.prepareStatement("UPDATE usuarios SET senha = ? WHERE id = ?");
            atualizar.setString(1, novaSenha);
            atualizar.setInt(2, usuarioId);
            atualizar.executeUpdate();
            Util.responder(troca, 200, "Senha atualizada.");
        } catch (Exception e) {
            Util.responder(troca, 400, "Erro ao atualizar senha: " + e.getMessage());
        }
    }
}
