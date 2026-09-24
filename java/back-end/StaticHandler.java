// Serve os arquivos do front-end (pasta "front-end") como conteudo estatico.
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;
import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Path;

public class StaticHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        String caminho = troca.getRequestURI().getPath();
        if (caminho.equals("/") || caminho.isEmpty()) {
            caminho = "/index.html";
        }

        Path base = Path.of("front-end").toAbsolutePath().normalize();
        Path arquivo = Path.of("front-end" + caminho).toAbsolutePath().normalize();

        // So serve arquivos de dentro de "front-end" (evita traversal na parte estatica).
        if (!arquivo.startsWith(base) || !Files.exists(arquivo) || Files.isDirectory(arquivo)) {
            Util.responder(troca, 404, "Nao encontrado.");
            return;
        }

        byte[] bytes = Files.readAllBytes(arquivo);
        troca.getResponseHeaders().set("Content-Type", tipo(caminho));
        troca.sendResponseHeaders(200, bytes.length);
        try (OutputStream saida = troca.getResponseBody()) {
            saida.write(bytes);
        }
    }

    private String tipo(String p) {
        if (p.endsWith(".html")) return "text/html; charset=utf-8";
        if (p.endsWith(".css")) return "text/css; charset=utf-8";
        if (p.endsWith(".js")) return "application/javascript; charset=utf-8";
        if (p.endsWith(".svg")) return "image/svg+xml";
        if (p.endsWith(".jpg") || p.endsWith(".jpeg")) return "image/jpeg";
        if (p.endsWith(".png")) return "image/png";
        return "application/octet-stream";
    }
}
