package core;

// Sessao simples em memoria, identificada por um cookie "session".
import com.sun.net.httpserver.HttpExchange;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

public class Sessao {

    // Guarda: id da sessao (cookie) -> id do usuario.
    private static final Map<String, Integer> SESSOES = new ConcurrentHashMap<>();

    // Cria uma sessao para o usuario e devolve o id do cookie.
    public static String criar(int usuarioId) {
        String id = UUID.randomUUID().toString().replace("-", "");
        SESSOES.put(id, usuarioId);
        return id;
    }

    // Devolve o id do usuario logado (pelo cookie), ou null se ninguem estiver logado.
    public static Integer usuarioLogado(HttpExchange troca) {
        String id = idDoCookie(troca);
        return id == null ? null : SESSOES.get(id);
    }

    // Encerra a sessao do cookie atual.
    public static void remover(HttpExchange troca) {
        String id = idDoCookie(troca);
        if (id != null) SESSOES.remove(id);
    }

    // Extrai o valor de "session=" do header Cookie.
    private static String idDoCookie(HttpExchange troca) {
        String cookie = troca.getRequestHeaders().getFirst("Cookie");
        if (cookie == null) return null;
        for (String parte : cookie.split(";")) {
            String p = parte.trim();
            if (p.startsWith("session=")) {
                return p.substring("session=".length());
            }
        }
        return null;
    }
}
