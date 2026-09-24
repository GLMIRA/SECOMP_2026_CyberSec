// Rota /saldo: informa o saldo de uma conta.
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.util.Map;

public class SaldoHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        if (!troca.getRequestMethod().equals("GET")) {
            Util.responder(troca, 405, "Use GET.");
            return;
        }
        if (Sessao.usuarioLogado(troca) == null) {
            Util.responder(troca, 401, "Faca login primeiro.");
            return;
        }

        Map<String, String> parametros = Util.lerConsulta(troca);
        String conta = parametros.get("conta");
        if (conta == null || conta.isEmpty()) {
            Util.responder(troca, 400, "Informe a conta (?conta=...).");
            return;
        }

        try (Connection conexao = Banco.conectar()) {
            PreparedStatement consulta = conexao.prepareStatement("SELECT saldo FROM contas WHERE id = ?");
            consulta.setString(1, conta);
            ResultSet resultado = consulta.executeQuery();

            if (resultado.next()) {
                double saldo = resultado.getDouble("saldo");
                Util.responder(troca, 200, "Saldo da conta " + conta + ": " + saldo);
            } else {
                Util.responder(troca, 404, "Conta nao encontrada.");
            }
        } catch (Exception e) {
            Util.responder(troca, 400, "Erro ao consultar saldo: " + e.getMessage());
        }
    }
}
