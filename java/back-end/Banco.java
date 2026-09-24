// Conexao com o banco de dados SQLite.
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.sql.Statement;

public class Banco {

    // Caminho do arquivo do banco (relativo a pasta onde o servidor e iniciado).
    private static final String CAMINHO = "db/banco.db";
    private static final String URL = "jdbc:sqlite:" + CAMINHO;

    // Garante que o driver do SQLite esteja carregado.
    static {
        try {
            Class.forName("org.sqlite.JDBC");
        } catch (ClassNotFoundException e) {
            throw new RuntimeException("Driver do SQLite nao encontrado no classpath.", e);
        }
    }

    // Abre uma nova conexao com o banco.
    public static Connection conectar() throws SQLException {
        Connection conexao = DriverManager.getConnection(URL);
        // O SQLite so respeita as chaves estrangeiras se ligarmos isto em cada conexao.
        try (Statement st = conexao.createStatement()) {
            st.execute("PRAGMA foreign_keys = ON");
        }
        return conexao;
    }

    // Cria o banco a partir do schema, caso ainda nao exista.
    public static void prepararBanco() {
        if (Files.exists(Path.of(CAMINHO))) {
            return;
        }
        try {
            String schema = Files.readString(Path.of("db/schema.sql"));
            try (Connection conexao = conectar(); Statement st = conexao.createStatement()) {
                for (String comando : schema.split(";")) {
                    if (!comando.trim().isEmpty()) {
                        st.execute(comando);
                    }
                }
            }
        } catch (IOException | SQLException e) {
            throw new RuntimeException("Nao foi possivel preparar o banco.", e);
        }
    }

    // Verifica se uma conta pertence a um usuario.
    public static boolean contaPertence(String conta, int usuarioId) throws SQLException {
        try (Connection conexao = conectar();
             PreparedStatement ps = conexao.prepareStatement(
                     "SELECT 1 FROM contas WHERE id = ? AND usuario_id = ?")) {
            ps.setString(1, conta);
            ps.setInt(2, usuarioId);
            try (ResultSet rs = ps.executeQuery()) {
                return rs.next();
            }
        }
    }

    // Verifica se o usuario e administrador.
    public static boolean ehAdmin(int usuarioId) throws SQLException {
        try (Connection conexao = conectar();
             PreparedStatement ps = conexao.prepareStatement(
                     "SELECT admin FROM usuarios WHERE id = ?")) {
            ps.setInt(1, usuarioId);
            try (ResultSet rs = ps.executeQuery()) {
                return rs.next() && rs.getInt(1) == 1;
            }
        }
    }
}
