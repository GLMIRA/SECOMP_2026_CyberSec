// Conexao com o banco de dados SQLite.
using Microsoft.Data.Sqlite;

namespace BancoCtf;

public static class Banco
{
    // Caminho do arquivo do banco e do schema (definidos no Program).
    public static string CaminhoBanco = "db/banco.db";
    public static string CaminhoSchema = "db/schema.sql";

    // Abre uma nova conexao com o banco.
    public static SqliteConnection Conectar()
    {
        var conexao = new SqliteConnection($"Data Source={CaminhoBanco}");
        conexao.Open();
        // O SQLite so respeita as chaves estrangeiras se ligarmos isto em cada conexao.
        using var pragma = conexao.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON";
        pragma.ExecuteNonQuery();
        return conexao;
    }

    // Cria o banco a partir do schema, caso ainda nao exista.
    public static void CriarSeNaoExiste()
    {
        if (File.Exists(CaminhoBanco))
            return;

        var script = File.ReadAllText(CaminhoSchema);
        using var conexao = Conectar();
        using var cmd = conexao.CreateCommand();
        cmd.CommandText = script;
        cmd.ExecuteNonQuery();
    }

    // Verifica se uma conta pertence a um usuario.
    public static bool ContaPertence(string conta, long usuarioId)
    {
        using var conexao = Conectar();
        using var cmd = conexao.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM contas WHERE id = @conta AND usuario_id = @uid";
        cmd.Parameters.AddWithValue("@conta", conta);
        cmd.Parameters.AddWithValue("@uid", usuarioId);
        return cmd.ExecuteScalar() != null;
    }
}
