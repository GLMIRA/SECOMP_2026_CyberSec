package usuario;

import core.Banco;
import core.Util;
import core.Sessao;

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
            } else if (caminho.endsWith("/transferencias")) {
                listarTransferencias(troca);
            } else if (caminho.endsWith("/estornar")) {
                estornarTransferencia(troca);
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

    private void listarTransferencias(HttpExchange troca) throws Exception {
        try (Connection conexao = Banco.conectar()) {
            PreparedStatement ps = conexao.prepareStatement(
                    "SELECT id, conta_origem, conta_destino, valor, data "
                    + "FROM transferencias ORDER BY id DESC");
            ResultSet rs = ps.executeQuery();

            StringBuilder sb = new StringBuilder("[");
            boolean primeiro = true;
            while (rs.next()) {
                if (!primeiro) {
                    sb.append(",");
                }
                primeiro = false;
                sb.append("{\"id\":").append(rs.getInt("id"))
                  .append(",\"origem\":").append(rs.getInt("conta_origem"))
                  .append(",\"destino\":").append(rs.getInt("conta_destino"))
                  .append(",\"valor\":").append(rs.getDouble("valor"))
                  .append(",\"data\":\"").append(Util.escaparJson(rs.getString("data"))).append("\"}");
            }
            sb.append("]");
            Util.responderJson(troca, 200, sb.toString());
        }
    }

    private void estornarTransferencia(HttpExchange troca) throws Exception {
        if (!troca.getRequestMethod().equals("POST")) {
            Util.responder(troca, 405, "Use POST.");
            return;
        }
        Map<String, String> dados = Util.parseJsonPlano(Util.lerCorpo(troca));
        String alvo = dados.get("id");
        if (alvo == null || alvo.isEmpty()) {
            Util.responder(troca, 400, "Informe o id da transferencia.");
            return;
        }

        try (Connection conexao = Banco.conectar()) {
            PreparedStatement buscar = conexao.prepareStatement(
                    "SELECT conta_origem, valor FROM transferencias WHERE id = ?");
            buscar.setString(1, alvo);
            ResultSet rs = buscar.executeQuery();
            if (!rs.next()) {
                Util.responder(troca, 404, "Transferencia nao encontrada.");
                return;
            }
            int origem = rs.getInt("conta_origem");
            double valor = rs.getDouble("valor");

            // Devolve o valor da transferencia para a conta de origem.
            double valorDevolvido = Math.abs(valor);
            PreparedStatement lerSaldo = conexao.prepareStatement("SELECT saldo FROM contas WHERE id = ?");
            lerSaldo.setInt(1, origem);
            ResultSet rsSaldo = lerSaldo.executeQuery();
            rsSaldo.next();
            double saldo = rsSaldo.getDouble("saldo");

            PreparedStatement atualizar = conexao.prepareStatement("UPDATE contas SET saldo = ? WHERE id = ?");
            atualizar.setDouble(1, saldo + valorDevolvido);
            atualizar.setInt(2, origem);
            atualizar.executeUpdate();

            PreparedStatement mov = conexao.prepareStatement(
                    "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (?, 'estorno', ?)");
            mov.setInt(1, origem);
            mov.setDouble(2, valorDevolvido);
            mov.executeUpdate();

            // Uma transferencia so pode ser estornada uma vez.
            PreparedStatement remover = conexao.prepareStatement("DELETE FROM transferencias WHERE id = ?");
            remover.setString(1, alvo);
            remover.executeUpdate();

            Util.responder(troca, 200, "Transferencia estornada. Valor devolvido a conta " + origem + ".");
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
