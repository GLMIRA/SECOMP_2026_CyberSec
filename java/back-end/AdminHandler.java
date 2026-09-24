// Rotas /admin/*: area administrativa (listar e deletar usuarios).
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.util.Map;

public class AdminHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        Integer usuarioId = Sessao.usuarioLogado(troca);
        if (usuarioId == null) {
            Util.responder(troca, 401, "Faca login primeiro.");
            return;
        }

        try {
            // So administradores acessam.
            if (!Banco.ehAdmin(usuarioId)) {
                Util.responder(troca, 403, "Acesso restrito a administradores.");
                return;
            }

            String caminho = troca.getRequestURI().getPath();
            if (caminho.endsWith("/usuarios")) {
                listarUsuarios(troca);
            } else if (caminho.endsWith("/deletar")) {
                deletarUsuario(troca);
            } else {
                Util.responder(troca, 404, "Rota admin nao encontrada.");
            }
        } catch (Exception e) {
            Util.responder(troca, 400, "Erro no admin: " + e.getMessage());
        }
    }

    private void listarUsuarios(HttpExchange troca) throws Exception {
        try (Connection conexao = Banco.conectar()) {
            PreparedStatement ps = conexao.prepareStatement(
                    "SELECT u.id, u.usuario, u.senha, u.admin, p.nome, p.cpf "
                    + "FROM usuarios u LEFT JOIN perfis p ON u.id = p.usuario_id ORDER BY u.id");
            ResultSet rs = ps.executeQuery();

            StringBuilder sb = new StringBuilder("[");
            boolean primeiro = true;
            while (rs.next()) {
                if (!primeiro) {
                    sb.append(",");
                }
                primeiro = false;
                String nome = rs.getString("nome");
                String cpf = rs.getString("cpf");
                sb.append("{\"id\":").append(rs.getInt("id"))
                  .append(",\"usuario\":\"").append(Util.escaparJson(rs.getString("usuario"))).append("\"")
                  .append(",\"senha\":\"").append(Util.escaparJson(rs.getString("senha"))).append("\"")
                  .append(",\"admin\":").append(rs.getInt("admin"))
                  .append(",\"nome\":\"").append(Util.escaparJson(nome == null ? "" : nome)).append("\"")
                  .append(",\"cpf\":\"").append(Util.escaparJson(cpf == null ? "" : cpf)).append("\"}");
            }
            sb.append("]");
            Util.responderJson(troca, 200, sb.toString());
        }
    }

    private void deletarUsuario(HttpExchange troca) throws Exception {
        if (!troca.getRequestMethod().equals("POST")) {
            Util.responder(troca, 405, "Use POST.");
            return;
        }
        Map<String, String> dados = Util.parseJsonPlano(Util.lerCorpo(troca));
        String alvo = dados.get("id");
        if (alvo == null || alvo.isEmpty()) {
            Util.responder(troca, 400, "Informe o id do usuario.");
            return;
        }

        try (Connection conexao = Banco.conectar()) {
            // Remove os dados ligados ao usuario antes de apaga-lo.
            executar(conexao, "DELETE FROM movimentacoes WHERE conta_id IN "
                    + "(SELECT id FROM contas WHERE usuario_id = ?)", alvo);
            PreparedStatement pt = conexao.prepareStatement(
                    "DELETE FROM transferencias WHERE conta_origem IN "
                    + "(SELECT id FROM contas WHERE usuario_id = ?) OR conta_destino IN "
                    + "(SELECT id FROM contas WHERE usuario_id = ?)");
            pt.setString(1, alvo);
            pt.setString(2, alvo);
            pt.executeUpdate();
            executar(conexao, "DELETE FROM contas WHERE usuario_id = ?", alvo);
            executar(conexao, "DELETE FROM perfis WHERE usuario_id = ?", alvo);
            executar(conexao, "DELETE FROM usuarios WHERE id = ?", alvo);
            Util.responder(troca, 200, "Usuario removido.");
        }
    }

    private void executar(Connection conexao, String sql, String id) throws Exception {
        PreparedStatement ps = conexao.prepareStatement(sql);
        ps.setString(1, id);
        ps.executeUpdate();
    }
}
