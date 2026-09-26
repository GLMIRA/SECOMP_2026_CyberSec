package sistema;

import core.Banco;
import core.Util;
import core.Sessao;

// Rota /comprovante: baixa o comprovante (arquivo) de uma operacao.
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Map;

public class ComprovanteHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        if (Sessao.usuarioLogado(troca) == null) {
            Util.responder(troca, 401, "Faca login primeiro.");
            return;
        }

        Map<String, String> parametros = Util.lerConsulta(troca);
        String arquivo = parametros.get("arquivo");
        if (arquivo == null || arquivo.isEmpty()) {
            Util.responder(troca, 400, "Informe o arquivo (?arquivo=...).");
            return;
        }

        try {
            Path caminho = Path.of("comprovantes", arquivo);
            String conteudo = Files.readString(caminho);
            Util.responder(troca, 200, conteudo);
        } catch (Exception e) {
            Util.responder(troca, 400, "Erro ao abrir comprovante: " + e.getMessage());
        }
    }
}
