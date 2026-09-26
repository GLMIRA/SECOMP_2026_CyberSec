// Funcoes auxiliares compartilhadas: respostas HTTP, sessao e checagem de admin.
using System.Text.Json;

namespace BancoCtf;

public static class Web
{
    // Pasta onde ficam os comprovantes (definida no Program.cs).
    public static string PastaComprovantes = "";

    // Id do usuario logado (pela sessao), ou null.
    public static long? UsuarioLogado(HttpContext ctx) => ctx.Session.GetInt32("usuario_id");

    // Resposta de texto simples.
    public static async Task Responder(HttpContext ctx, string texto, int codigo = 200)
    {
        ctx.Response.StatusCode = codigo;
        ctx.Response.ContentType = "text/plain; charset=utf-8";
        await ctx.Response.WriteAsync(texto);
    }

    // Resposta em JSON.
    public static async Task ResponderJson(HttpContext ctx, object dados, int codigo = 200)
    {
        ctx.Response.StatusCode = codigo;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(dados));
    }

    // Verifica se o usuario e administrador.
    public static bool EhAdmin(long usuarioId)
    {
        using var conexao = Banco.Conectar();
        using var cmd = conexao.CreateCommand();
        cmd.CommandText = "SELECT admin FROM usuarios WHERE id = @id";
        cmd.Parameters.AddWithValue("@id", usuarioId);
        var r = cmd.ExecuteScalar();
        return r != null && Convert.ToInt64(r) == 1;
    }
}
