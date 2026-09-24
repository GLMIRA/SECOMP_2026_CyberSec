// Rota /login: verifica usuario e senha e abre a sessao.
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.sql.Connection;
import java.sql.ResultSet;
import java.sql.Statement;
import java.util.Map;

public class LoginHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        if (!troca.getRequestMethod().equals("POST")) {
            Util.responder(troca, 405, "Use POST.");
            return;
        }

        Map<String, String> dados = Util.lerFormulario(troca);
        String usuario = dados.get("usuario");
        String senha = dados.get("senha");

        if (usuario == null || usuario.isEmpty() || senha == null || senha.isEmpty()) {
            Util.responder(troca, 400, "Informe usuario e senha.");
            return;
        }

        try (Connection conexao = Banco.conectar()) {
            // Monta a consulta com o usuario e a senha recebidos.
            String sql = "SELECT id FROM usuarios "
                    + "WHERE usuario = '" + usuario + "' AND senha = '" + senha + "'";

            Statement comando = conexao.createStatement();
            ResultSet resultado = comando.executeQuery(sql);

            if (resultado.next()) {
                int usuarioId = resultado.getInt("id");
                String sid = Sessao.criar(usuarioId);
                troca.getResponseHeaders().add("Set-Cookie", "session=" + sid + "; Path=/");
                Util.responder(troca, 200, "Login OK! id do usuario: " + usuarioId);
            } else {
                Util.responder(troca, 401, "Usuario ou senha invalidos.");
            }
        } catch (Exception e) {
            Util.responder(troca, 400, "Erro no login: " + e.getMessage());
        }
    }
}
