package sistema;

import core.Banco;
import core.Util;
import core.Sessao;

// Rota /saque: retira um valor do saldo de uma conta.
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.util.Map;

public class SaqueHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        if (!troca.getRequestMethod().equals("POST")) {
            Util.responder(troca, 405, "Use POST.");
            return;
        }
        Integer usuarioId = Sessao.usuarioLogado(troca);
        if (usuarioId == null) {
            Util.responder(troca, 401, "Faca login primeiro.");
            return;
        }

        Map<String, String> dados = Util.lerFormulario(troca);
        String conta = dados.get("conta");
        String valorTexto = dados.get("valor");
        if (conta == null || conta.isEmpty() || valorTexto == null || valorTexto.isEmpty()) {
            Util.responder(troca, 400, "Informe a conta e o valor.");
            return;
        }

        double valor;
        try {
            valor = Double.parseDouble(valorTexto);
        } catch (NumberFormatException e) {
            Util.responder(troca, 400, "Valor invalido.");
            return;
        }
        if (valor <= 0) {
            Util.responder(troca, 400, "O valor do saque deve ser positivo.");
            return;
        }

        try {
            // So pode sacar de uma conta sua.
            if (!Banco.contaPertence(conta, usuarioId)) {
                Util.responder(troca, 403, "Essa conta nao e sua.");
                return;
            }
            try (Connection conexao = Banco.conectar()) {
                PreparedStatement consulta = conexao.prepareStatement("SELECT saldo FROM contas WHERE id = ?");
                consulta.setString(1, conta);
                ResultSet resultado = consulta.executeQuery();
                if (!resultado.next()) {
                    Util.responder(troca, 404, "Conta nao encontrada.");
                    return;
                }

                double saldoAtual = resultado.getDouble("saldo");
                if (valor > saldoAtual) {
                    Util.responder(troca, 400, "Saldo insuficiente.");
                    return;
                }

                double novoSaldo = saldoAtual - valor;
                PreparedStatement atualizar = conexao.prepareStatement("UPDATE contas SET saldo = ? WHERE id = ?");
                atualizar.setDouble(1, novoSaldo);
                atualizar.setString(2, conta);
                atualizar.executeUpdate();

                PreparedStatement movimento = conexao.prepareStatement(
                        "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (?, 'saque', ?)");
                movimento.setString(1, conta);
                movimento.setDouble(2, valor);
                movimento.executeUpdate();

                Util.responder(troca, 200, "Saque realizado. Novo saldo: " + novoSaldo);
            }
        } catch (Exception e) {
            Util.responder(troca, 400, "Erro no saque: " + e.getMessage());
        }
    }
}
