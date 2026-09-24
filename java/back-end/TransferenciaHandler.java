// Rota /transferencia: transfere um valor de uma conta para outra.
// ESQUELETO - a implementacao deve ser feita aqui.
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.util.Map;

public class TransferenciaHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        // A transferencia usa POST.
        if (!troca.getRequestMethod().equals("POST")) {
            Util.responder(troca, 405, "Use POST.");
            return;
        }

        // Campos esperados do formulario.
        Map<String, String> dados = Util.lerFormulario(troca);
        String contaOrigem = dados.get("origem");
        String contaDestino = dados.get("destino");
        String valor = dados.get("valor");

        // TODO: validar os campos recebidos (origem, destino e valor).

        // TODO: buscar o saldo da conta de origem.

        // TODO: aplicar as regras de negocio, por exemplo:
        //   - o valor precisa ser positivo;
        //   - a origem precisa ter saldo suficiente;
        //   - o saldo de uma conta nao pode passar de 1.000.000,00.

        // TODO: debitar da origem e creditar no destino.

        // TODO: registrar a transferencia na tabela "transferencias".

        // TODO: responder com o resultado da operacao.
        Util.responder(troca, 501, "Transferencia ainda nao implementada.");
    }
}
