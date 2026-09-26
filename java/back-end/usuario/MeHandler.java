package usuario;

import core.Banco;
import core.Util;
import core.Sessao;

// Rota /me: informa o usuario logado (pela sessao), se e admin e o numero da conta.
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;

public class MeHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        Integer usuarioId = Sessao.usuarioLogado(troca);
        if (usuarioId == null) {
            Util.responder(troca, 401, "Faca login primeiro.");
            return;
        }

        try (Connection conexao = Banco.conectar()) {
            PreparedStatement ps = conexao.prepareStatement("SELECT usuario, admin FROM usuarios WHERE id = ?");
            ps.setInt(1, usuarioId);
            ResultSet rs = ps.executeQuery();
            if (!rs.next()) {
                Util.responder(troca, 404, "Usuario nao encontrado.");
                return;
            }
            String usuario = rs.getString("usuario");
            int admin = rs.getInt("admin");

            PreparedStatement pc = conexao.prepareStatement(
                    "SELECT id FROM contas WHERE usuario_id = ? ORDER BY id LIMIT 1");
            pc.setInt(1, usuarioId);
            ResultSet rc = pc.executeQuery();
            String conta = rc.next() ? String.valueOf(rc.getInt("id")) : "null";

            String json = "{\"id\":" + usuarioId
                    + ",\"usuario\":\"" + Util.escaparJson(usuario) + "\""
                    + ",\"admin\":" + admin
                    + ",\"conta\":" + conta + "}";
            Util.responderJson(troca, 200, json);
        } catch (Exception e) {
            Util.responder(troca, 400, "Erro ao obter usuario: " + e.getMessage());
        }
    }
}
