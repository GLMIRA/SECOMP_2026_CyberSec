package sistema;

// Rota /extrato: lista as movimentacoes das contas do usuario logado.
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;

import core.Banco;
import core.Util;
import core.Sessao;

public class ExtratoHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        if (!troca.getRequestMethod().equals("GET")) {
            Util.responder(troca, 405, "Use GET.");
            return;
        }
        Integer usuarioId = Sessao.usuarioLogado(troca);
        if (usuarioId == null) {
            Util.responder(troca, 401, "Faca login primeiro.");
            return;
        }

        try (Connection conexao = Banco.conectar()) {
            // Busca as movimentacoes (deposito, saque, ...) das contas do usuario.
            PreparedStatement consulta = conexao.prepareStatement(
                    "SELECT tipo, conta_id, valor, data FROM movimentacoes "
                    + "WHERE conta_id IN (SELECT id FROM contas WHERE usuario_id = ?) "
                    + "ORDER BY data DESC");
            consulta.setInt(1, usuarioId);
            ResultSet resultado = consulta.executeQuery();

            StringBuilder texto = new StringBuilder();
            boolean tem = false;
            while (resultado.next()) {
                tem = true;
                texto.append(resultado.getString("tipo").toUpperCase())
                     .append("  conta ").append(resultado.getInt("conta_id"))
                     .append("  R$ ").append(resultado.getDouble("valor"))
                     .append("  (").append(resultado.getString("data")).append(")\n");
            }

            if (!tem) {
                Util.responder(troca, 200, "Nenhuma movimentacao.");
            } else {
                Util.responder(troca, 200, texto.toString().trim());
            }
        } catch (Exception e) {
            Util.responder(troca, 400, "Erro ao obter extrato: " + e.getMessage());
        }
    }
}
