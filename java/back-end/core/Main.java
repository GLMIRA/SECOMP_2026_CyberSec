package core;

// Ponto de entrada: inicia o servidor HTTP, serve o front-end e registra as rotas.
import com.sun.net.httpserver.HttpServer;
import java.net.InetSocketAddress;

public class Main {

    public static void main(String[] args) throws Exception {
        // Cria o banco a partir do schema, se ainda nao existir.
        Banco.prepararBanco();

        int porta = 8090;
        HttpServer servidor = HttpServer.create(new InetSocketAddress(porta), 0);

        // Rotas de usuario/identidade.
        servidor.createContext("/cadastro", new usuario.CadastroHandler());
        servidor.createContext("/login", new usuario.LoginHandler());
        servidor.createContext("/logout", new usuario.LogoutHandler());
        servidor.createContext("/me", new usuario.MeHandler());
        servidor.createContext("/recuperar-senha", new usuario.RecuperarSenhaHandler());
        servidor.createContext("/perfil", new usuario.PerfilHandler());
        servidor.createContext("/admin", new usuario.AdminHandler());

        // Rotas do sistema (operacoes da conta).
        servidor.createContext("/saldo", new sistema.SaldoHandler());
        servidor.createContext("/deposito", new sistema.DepositoHandler());
        servidor.createContext("/saque", new sistema.SaqueHandler());
        servidor.createContext("/extrato", new sistema.ExtratoHandler());
        servidor.createContext("/transferencia", new sistema.TransferenciaHandler());
        servidor.createContext("/comprovante", new sistema.ComprovanteHandler());

        // Front-end estatico (fallback): "/" e demais caminhos servem os arquivos.
        servidor.createContext("/", new StaticHandler());

        servidor.setExecutor(null); // executor padrao
        servidor.start();
        System.out.println("Servidor rodando em http://localhost:" + porta);
    }
}
