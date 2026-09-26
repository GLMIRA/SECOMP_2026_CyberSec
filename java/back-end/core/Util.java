package core;

// Funcoes auxiliares para ler requisicoes e enviar respostas HTTP.
import com.sun.net.httpserver.HttpExchange;
import java.io.IOException;
import java.io.OutputStream;
import java.net.URLDecoder;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class Util {

    // Le o corpo da requisicao no formato de formulario (usuario=alice&senha=123).
    public static Map<String, String> lerFormulario(HttpExchange troca) throws IOException {
        return separarCampos(lerCorpo(troca));
    }

    // Le os parametros da URL (o que vem depois do "?", ex.: /saldo?conta=1).
    public static Map<String, String> lerConsulta(HttpExchange troca) {
        String query = troca.getRequestURI().getQuery();
        return separarCampos(query == null ? "" : query);
    }

    // Le o corpo bruto da requisicao (texto).
    public static String lerCorpo(HttpExchange troca) throws IOException {
        return new String(troca.getRequestBody().readAllBytes(), StandardCharsets.UTF_8);
    }

    // Quebra um texto "chave=valor&chave=valor" em um mapa.
    private static Map<String, String> separarCampos(String texto) {
        Map<String, String> campos = new HashMap<>();
        for (String par : texto.split("&")) {
            if (par.isEmpty()) continue;
            String[] partes = par.split("=", 2);
            String chave = URLDecoder.decode(partes[0], StandardCharsets.UTF_8);
            String valor = partes.length > 1 ? URLDecoder.decode(partes[1], StandardCharsets.UTF_8) : "";
            campos.put(chave, valor);
        }
        return campos;
    }

    // Parser simples de JSON plano: {"a":"b","c":1} -> mapa chave -> valor (como texto).
    public static Map<String, String> parseJsonPlano(String corpo) {
        Map<String, String> mapa = new HashMap<>();
        if (corpo == null) return mapa;
        String s = corpo.trim();
        int ini = s.indexOf('{');
        int fim = s.lastIndexOf('}');
        if (ini < 0 || fim <= ini) return mapa;
        s = s.substring(ini + 1, fim);

        List<String> pares = new ArrayList<>();
        StringBuilder atual = new StringBuilder();
        boolean aspas = false;
        for (int i = 0; i < s.length(); i++) {
            char c = s.charAt(i);
            if (c == '"') aspas = !aspas;
            if (c == ',' && !aspas) {
                pares.add(atual.toString());
                atual.setLength(0);
            } else {
                atual.append(c);
            }
        }
        if (atual.length() > 0) pares.add(atual.toString());

        for (String par : pares) {
            int dp = indiceDoisPontos(par);
            if (dp < 0) continue;
            String chave = limpar(par.substring(0, dp));
            String valor = limpar(par.substring(dp + 1));
            if (!chave.isEmpty()) mapa.put(chave, valor);
        }
        return mapa;
    }

    private static int indiceDoisPontos(String s) {
        boolean aspas = false;
        for (int i = 0; i < s.length(); i++) {
            char c = s.charAt(i);
            if (c == '"') aspas = !aspas;
            else if (c == ':' && !aspas) return i;
        }
        return -1;
    }

    private static String limpar(String s) {
        s = s.trim();
        if (s.length() >= 2 && s.startsWith("\"") && s.endsWith("\"")) {
            s = s.substring(1, s.length() - 1);
        }
        return s;
    }

    // Escapa um texto para colocar dentro de uma string JSON.
    public static String escaparJson(String s) {
        if (s == null) return "";
        return s.replace("\\", "\\\\").replace("\"", "\\\"")
                .replace("\n", "\\n").replace("\r", "\\r");
    }

    // Envia uma resposta de texto simples com o codigo HTTP informado.
    public static void responder(HttpExchange troca, int codigo, String texto) throws IOException {
        byte[] bytes = texto.getBytes(StandardCharsets.UTF_8);
        troca.getResponseHeaders().set("Content-Type", "text/plain; charset=utf-8");
        troca.getResponseHeaders().set("Access-Control-Allow-Origin", "*");
        troca.sendResponseHeaders(codigo, bytes.length);
        try (OutputStream saida = troca.getResponseBody()) {
            saida.write(bytes);
        }
    }

    // Envia uma resposta em JSON.
    public static void responderJson(HttpExchange troca, int codigo, String json) throws IOException {
        byte[] bytes = json.getBytes(StandardCharsets.UTF_8);
        troca.getResponseHeaders().set("Content-Type", "application/json; charset=utf-8");
        troca.getResponseHeaders().set("Access-Control-Allow-Origin", "*");
        troca.sendResponseHeaders(codigo, bytes.length);
        try (OutputStream saida = troca.getResponseBody()) {
            saida.write(bytes);
        }
    }
}
