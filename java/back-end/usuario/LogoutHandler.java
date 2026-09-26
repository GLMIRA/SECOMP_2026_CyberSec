package usuario;

import core.Banco;
import core.Util;
import core.Sessao;

// Rota /logout: encerra a sessao do usuario.
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import java.io.IOException;

public class LogoutHandler implements HttpHandler {

    @Override
    public void handle(HttpExchange troca) throws IOException {
        Sessao.remover(troca);
        Util.responder(troca, 200, "Logout feito.");
    }
}
