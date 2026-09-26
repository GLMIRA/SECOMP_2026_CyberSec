package sistema;

import core.Banco;
import core.Util;
import core.Sessao;

// Rota /transferencia: transfere um valor de uma conta para outra.
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.util.Map;

public class TransferenciaHandler implements HttpHandler {

    // Valor maximo por operacao.
    private static final double LIMITE = 1_000_000.00;

    @Override
    public void handle(HttpExchange troca) throws IOException {
        // A transferencia usa POST.
        if (!troca.getRequestMethod().equals("POST")) {
            Util.responder(troca, 405, "Use POST.");
            return;
        }
        Integer usuarioId = Sessao.usuarioLogado(troca);
        if (usuarioId == null) {
            Util.responder(troca, 401, "Faca login primeiro.");
            return;
        }

        // Campos esperados do formulario.
        Map<String, String> dados = Util.lerFormulario(troca);
        String origem = dados.get("origem");
        String destino = dados.get("destino");
        String valorTexto = dados.get("valor");

        if (origem == null || origem.isEmpty() || destino == null || destino.isEmpty()
                || valorTexto == null || valorTexto.isEmpty()) {
            Util.responder(troca, 400, "Informe origem, destino e valor.");
            return;
        }
        if (origem.equals(destino)) {
            Util.responder(troca, 400, "Origem e destino nao podem ser a mesma conta.");
            return;
        }

        double valor;
        try {
            valor = Double.parseDouble(valorTexto);
        } catch (NumberFormatException e) {
            Util.responder(troca, 400, "Valor invalido.");
            return;
        }
        if (valor > LIMITE) {
            Util.responder(troca, 400, "Transferencia maxima por operacao e 1.000.000,00.");
            return;
        }

        try {
            // So pode transferir de uma conta sua.
            if (!Banco.contaPertence(origem, usuarioId)) {
                Util.responder(troca, 403, "Essa conta nao e sua.");
                return;
            }
            try (Connection conexao = Banco.conectar()) {
                double saldoOrigem;
                double saldoDestino;

                PreparedStatement lerOrigem = conexao.prepareStatement("SELECT saldo FROM contas WHERE id = ?");
                lerOrigem.setString(1, origem);
                ResultSet rsOrigem = lerOrigem.executeQuery();
                if (!rsOrigem.next()) {
                    Util.responder(troca, 404, "Conta de origem nao encontrada.");
                    return;
                }
                saldoOrigem = rsOrigem.getDouble("saldo");

                PreparedStatement lerDestino = conexao.prepareStatement("SELECT saldo FROM contas WHERE id = ?");
                lerDestino.setString(1, destino);
                ResultSet rsDestino = lerDestino.executeQuery();
                if (!rsDestino.next()) {
                    Util.responder(troca, 404, "Conta de destino nao encontrada.");
                    return;
                }
                saldoDestino = rsDestino.getDouble("saldo");

                if (valor > saldoOrigem) {
                    Util.responder(troca, 400, "Saldo insuficiente.");
                    return;
                }

                double novoOrigem = saldoOrigem - valor;
                double novoDestino = saldoDestino + valor;

                PreparedStatement atualizarOrigem = conexao.prepareStatement("UPDATE contas SET saldo = ? WHERE id = ?");
                atualizarOrigem.setDouble(1, novoOrigem);
                atualizarOrigem.setString(2, origem);
                atualizarOrigem.executeUpdate();

                PreparedStatement atualizarDestino = conexao.prepareStatement("UPDATE contas SET saldo = ? WHERE id = ?");
                atualizarDestino.setDouble(1, novoDestino);
                atualizarDestino.setString(2, destino);
                atualizarDestino.executeUpdate();

                // Registra a transferencia.
                PreparedStatement registrar = conexao.prepareStatement(
                        "INSERT INTO transferencias (conta_origem, conta_destino, valor) VALUES (?, ?, ?)");
                registrar.setString(1, origem);
                registrar.setString(2, destino);
                registrar.setDouble(3, valor);
                registrar.executeUpdate();

                // Registra nos extratos das duas contas.
                PreparedStatement movSaida = conexao.prepareStatement(
                        "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (?, 'transferencia_saida', ?)");
                movSaida.setString(1, origem);
                movSaida.setDouble(2, valor);
                movSaida.executeUpdate();

                PreparedStatement movEntrada = conexao.prepareStatement(
                        "INSERT INTO movimentacoes (conta_id, tipo, valor) VALUES (?, 'transferencia_entrada', ?)");
                movEntrada.setString(1, destino);
                movEntrada.setDouble(2, valor);
                movEntrada.executeUpdate();

                Util.responder(troca, 200, "Transferencia realizada. Novo saldo da origem: " + novoOrigem);
            }
        } catch (Exception e) {
            Util.responder(troca, 400, "Erro na transferencia: " + e.getMessage());
        }
    }
}
