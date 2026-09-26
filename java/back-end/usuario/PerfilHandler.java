package usuario;

import core.Banco;
import core.Util;
import core.Sessao;

// Rota /perfil: mostra (GET) e atualiza (POST) os dados do usuario logado.
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;

public class PerfilHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        Integer usuarioId = Sessao.usuarioLogado(troca);
        if (usuarioId == null) {
            Util.responder(troca, 401, "Faca login primeiro.");
            return;
        }

        String metodo = troca.getRequestMethod();
        if (metodo.equals("GET")) {
            verPerfil(troca, usuarioId);
        } else if (metodo.equals("POST")) {
            atualizarPerfil(troca, usuarioId);
        } else {
            Util.responder(troca, 405, "Metodo nao suportado.");
        }
    }

    private void verPerfil(HttpExchange troca, int usuarioId) throws IOException {
        try (Connection conexao = Banco.conectar()) {
            PreparedStatement ps = conexao.prepareStatement(
                    "SELECT u.usuario, u.admin, p.nome, p.cpf "
                    + "FROM usuarios u JOIN perfis p ON u.id = p.usuario_id WHERE u.id = ?");
            ps.setInt(1, usuarioId);
            ResultSet rs = ps.executeQuery();
            if (!rs.next()) {
                Util.responder(troca, 404, "Usuario nao encontrado.");
                return;
            }
            String json = montarJson(usuarioId, rs.getString("usuario"), rs.getInt("admin"),
                    rs.getString("nome"), rs.getString("cpf"), null);
            Util.responderJson(troca, 200, json);
        } catch (Exception e) {
            Util.responder(troca, 400, "Erro ao obter perfil: " + e.getMessage());
        }
    }

    private void atualizarPerfil(HttpExchange troca, int usuarioId) throws IOException {
        try {
            Map<String, String> dados = Util.parseJsonPlano(Util.lerCorpo(troca));
            if (dados.isEmpty()) {
                Util.responder(troca, 400, "Envie os dados em JSON.");
                return;
            }
            try (Connection conexao = Banco.conectar()) {
                // nome e cpf ficam na tabela de perfil.
                List<String> colunas = new ArrayList<>();
                List<String> valores = new ArrayList<>();
                for (Map.Entry<String, String> campo : dados.entrySet()) {
                    String chave = campo.getKey();
                    if (chave.equals("nome") || chave.equals("cpf")) {
                        PreparedStatement pp = conexao.prepareStatement(
                                "UPDATE perfis SET " + chave + " = ? WHERE usuario_id = ?");
                        pp.setString(1, campo.getValue());
                        pp.setInt(2, usuarioId);
                        pp.executeUpdate();
                    } else {
                        colunas.add(chave + " = ?");
                        valores.add(campo.getValue());
                    }
                }
                // Os demais campos atualizam a tabela de usuarios.
                if (!colunas.isEmpty()) {
                    PreparedStatement pu = conexao.prepareStatement(
                            "UPDATE usuarios SET " + String.join(", ", colunas) + " WHERE id = ?");
                    int i = 1;
                    for (String v : valores) {
                        pu.setString(i++, v);
                    }
                    pu.setInt(i, usuarioId);
                    pu.executeUpdate();
                }

                // Devolve o perfil completo ja atualizado.
                PreparedStatement ps = conexao.prepareStatement(
                        "SELECT u.usuario, u.admin, p.nome, p.cpf "
                        + "FROM usuarios u JOIN perfis p ON u.id = p.usuario_id WHERE u.id = ?");
                ps.setInt(1, usuarioId);
                ResultSet rs = ps.executeQuery();
                rs.next();
                String json = montarJson(usuarioId, rs.getString("usuario"), rs.getInt("admin"),
                        rs.getString("nome"), rs.getString("cpf"), "Perfil atualizado.");
                Util.responderJson(troca, 200, json);
            }
        } catch (Exception e) {
            Util.responder(troca, 400, "Erro ao atualizar perfil: " + e.getMessage());
        }
    }

    private String montarJson(int id, String usuario, int admin, String nome, String cpf, String mensagem) {
        StringBuilder sb = new StringBuilder("{");
        if (mensagem != null) {
            sb.append("\"mensagem\":\"").append(Util.escaparJson(mensagem)).append("\",");
        }
        sb.append("\"id\":").append(id)
          .append(",\"usuario\":\"").append(Util.escaparJson(usuario)).append("\"")
          .append(",\"admin\":").append(admin)
          .append(",\"nome\":\"").append(Util.escaparJson(nome == null ? "" : nome)).append("\"")
          .append(",\"cpf\":\"").append(Util.escaparJson(cpf == null ? "" : cpf)).append("\"}");
        return sb.toString();
    }
}
