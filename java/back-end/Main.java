// Ponto de entrada: inicia o servidor HTTP, serve o front-end e registra as rotas.
import com.sun.net.httpserver.HttpServer;
import java.net.InetSocketAddress;

public class Main {

    public static void main(String[] args) throws Exception {
        // Cria o banco a partir do schema, se ainda nao existir.
        Banco.prepararBanco();

        int porta = 8090;
        HttpServer servidor = HttpServer.create(new InetSocketAddress(porta), 0);

        // Rotas do sistema.
        servidor.createContext("/cadastro", new CadastroHandler());
        servidor.createContext("/login", new LoginHandler());
        servidor.createContext("/logout", new LogoutHandler());
        servidor.createContext("/me", new MeHandler());
        servidor.createContext("/recuperar-senha", new RecuperarSenhaHandler());
        servidor.createContext("/saldo", new SaldoHandler());
        servidor.createContext("/deposito", new DepositoHandler());
        servidor.createContext("/saque", new SaqueHandler());
        servidor.createContext("/transferencia", new TransferenciaHandler());
        servidor.createContext("/perfil", new PerfilHandler());
        servidor.createContext("/comprovante", new ComprovanteHandler());
        servidor.createContext("/admin", new AdminHandler());

        // Front-end estatico (fallback): "/" e demais caminhos servem os arquivos.
        servidor.createContext("/", new StaticHandler());

        servidor.setExecutor(null); // executor padrao
        servidor.start();
        System.out.println("Servidor rodando em http://localhost:" + porta);
    }
}
